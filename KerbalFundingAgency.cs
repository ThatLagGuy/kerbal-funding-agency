using System;
using UnityEngine;
using KSP.UI.Screens;

namespace KFA
{
    /// <summary>
    /// Kerbal Funding Agency – Career Mode Annual Budget Allocation
    /// Compatible with stock KSP and RSS (reads year length from home body orbit period).
    ///
    /// Formula (no freeze):
    ///   annual_budget = baseAllocation × reputationMultiplier × facilityMultiplier × penaltyMultiplier
    ///
    /// Penalty system:
    ///   - Reputation below repPenaltyThreshold triggers a budget cut each year.
    ///   - consecutivePenaltyYears tracks how many years in a row rep has been low.
    ///   - After penaltyFreezeAfterYears consecutive penalty years, funding is frozen (0) for that year.
    ///   - Recovering rep above the threshold resets the consecutive counter.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.AllGameScenes, false)]
    public class KFAController : MonoBehaviour
    {
        // ── Singleton so BudgetUI can read live state ────────────────────────────
        public static KFAController Instance { get; private set; }

        // ── Persistence key ──────────────────────────────────────────────────────

        // ── Internal state ───────────────────────────────────────────────────────
        public double LastBudgetUT          { get; set; } = -1.0;
        public int    ConsecutivePenaltyYears { get; set; } = 0;
        public bool   FrozenLastYear        { get; set; } = false;
        private bool  initialized           = false;

        // ── Config (loaded from KerbalFundingAgency.cfg) ──────────────────────────────────
        public double BaseAllocation          { get; set; } = 150000.0;
        public double MinRepMultiplier        { get; set; } = 0.5;
        public double MaxRepMultiplier        { get; set; } = 2.0;
        public double FacilityMultiplierBase  { get; set; } = 1.0;
        public double FacilityMultiplierMax   { get; set; } = 2.5;

        // Penalty config
        public double RepPenaltyThreshold     { get; set; } = 200.0;   // rep below this → penalty
        public double PenaltyPerYear          { get; set; } = 0.15;    // 15% cut per consecutive year
        public double MaxPenaltyMultiplier    { get; set; } = 0.6;     // floor: never below 40% cut
        public int    PenaltyFreezeAfterYears { get; set; } = 3;       // freeze after N years in a row

        public bool ShowBreakdown             { get; set; } = true;

        // ── Lifecycle ────────────────────────────────────────────────────────────

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            // Load cfg defaults first, then overlay any saved career data on top.
            LoadConfig();
            ApplyPendingScenarioData();
        }

        /// <summary>
        /// Applies data cached by KFAScenario.OnLoad, which fires before
        /// KFAController.Awake() sets Instance. Called from Start() once
        /// Instance is ready and defaults are loaded.
        /// </summary>
        private void ApplyPendingScenarioData()
        {
            ConfigNode node = KFAScenario.PendingLoad;
            if (node == null)
            {
                Debug.Log("[KFA] No pending scenario data — using cfg defaults.");
                return;
            }

            string v;
            v = node.GetValue("lastBudgetUT");
            if (!string.IsNullOrEmpty(v) && double.TryParse(v, out double d)) LastBudgetUT = d;

            v = node.GetValue("consecutivePenaltyYears");
            if (!string.IsNullOrEmpty(v) && int.TryParse(v, out int ci)) ConsecutivePenaltyYears = ci;

            v = node.GetValue("frozenLastYear");
            if (!string.IsNullOrEmpty(v) && bool.TryParse(v, out bool fl)) FrozenLastYear = fl;

            double tmp;
            v = node.GetValue("baseAllocation");
            if (!string.IsNullOrEmpty(v) && double.TryParse(v, out tmp)) BaseAllocation = tmp;

            v = node.GetValue("minRepMultiplier");
            if (!string.IsNullOrEmpty(v) && double.TryParse(v, out tmp)) MinRepMultiplier = tmp;

            v = node.GetValue("maxRepMultiplier");
            if (!string.IsNullOrEmpty(v) && double.TryParse(v, out tmp)) MaxRepMultiplier = tmp;

            v = node.GetValue("facilityMultiplierBase");
            if (!string.IsNullOrEmpty(v) && double.TryParse(v, out tmp)) FacilityMultiplierBase = tmp;

            v = node.GetValue("facilityMultiplierMax");
            if (!string.IsNullOrEmpty(v) && double.TryParse(v, out tmp)) FacilityMultiplierMax = tmp;

            v = node.GetValue("repPenaltyThreshold");
            if (!string.IsNullOrEmpty(v) && double.TryParse(v, out tmp)) RepPenaltyThreshold = tmp;

            v = node.GetValue("penaltyPerYear");
            if (!string.IsNullOrEmpty(v) && double.TryParse(v, out tmp)) PenaltyPerYear = tmp;

            v = node.GetValue("maxPenaltyMultiplier");
            if (!string.IsNullOrEmpty(v) && double.TryParse(v, out tmp)) MaxPenaltyMultiplier = tmp;

            v = node.GetValue("penaltyFreezeAfterYears");
            if (!string.IsNullOrEmpty(v) && int.TryParse(v, out int fi)) PenaltyFreezeAfterYears = fi;

            v = node.GetValue("showBreakdown");
            if (!string.IsNullOrEmpty(v) && bool.TryParse(v, out bool sb)) ShowBreakdown = sb;

            Debug.Log($"[KFA] Pending scenario data applied. Base={BaseAllocation:N0}, " +
                      $"UT={LastBudgetUT:F0}, PenaltyYears={ConsecutivePenaltyYears}");
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (HighLogic.CurrentGame == null) return;
            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER) return;

            CheckYearRollover();
        }

        // ── Year rollover check ──────────────────────────────────────────────────

        private void CheckYearRollover()
        {
            double now        = Planetarium.GetUniversalTime();
            double yearLength = GetYearLength();

            // First run: anchor LastBudgetUT to the start of the current year
            // so the first deposit fires at the next year boundary, not immediately.
            if (!initialized)
            {
                LastBudgetUT = Math.Floor(now / yearLength) * yearLength;
                initialized  = true;
                Debug.Log($"[KFA] Initialized. UT={now:F0}, yearLength={yearLength:F0}, " +
                          $"nextBudgetUT={LastBudgetUT + yearLength:F0}");
                return;
            }

            double nextBudgetUT = LastBudgetUT + yearLength;

            if (now >= nextBudgetUT)
            {
                Debug.Log($"[KFA] Year boundary crossed. now={now:F0}, nextBudgetUT={nextBudgetUT:F0}");
                DepositBudget();

                // Advance LastBudgetUT by however many full years have elapsed
                // (handles the case where multiple years passed during time warp)
                long yearsPassed = (long)Math.Floor((now - LastBudgetUT) / yearLength);
                LastBudgetUT += yearsPassed * yearLength;
                Debug.Log($"[KFA] Advanced LastBudgetUT by {yearsPassed} year(s). " +
                          $"Next deposit at UT={LastBudgetUT + yearLength:F0}");
            }
        }

        // ── Budget calculation & deposit ─────────────────────────────────────────

        private void DepositBudget()
        {
            double repMult      = CalculateReputationMultiplier();
            double facMult      = CalculateFacilityMultiplier();
            double penMult      = CalculatePenaltyMultiplier(out bool frozen, out int penaltyYearsUsed);
            double budget       = frozen ? 0.0 : BaseAllocation * repMult * facMult * penMult;

            FrozenLastYear = frozen;

            if (!frozen)
                Funding.Instance.AddFunds(budget, TransactionReasons.None);

            if (ShowBreakdown)
                PostBudgetMessage(budget, repMult, facMult, penMult, frozen);

            Debug.Log($"[KFA] Year deposit: {budget:N0} funds " +
                      $"(rep×{repMult:F2}, fac×{facMult:F2}, pen×{penMult:F2}, frozen={frozen})");
        }

        // ── Multiplier helpers ───────────────────────────────────────────────────

        public double CalculateReputationMultiplier()
        {
            if (Reputation.Instance == null) return 1.0;
            float repNorm = Mathf.Clamp01(Reputation.Instance.reputation / 1000f);
            return MinRepMultiplier + repNorm * (MaxRepMultiplier - MinRepMultiplier);
        }

        public double CalculateFacilityMultiplier()
        {
            SpaceCenterFacility[] facilities =
            {
                SpaceCenterFacility.VehicleAssemblyBuilding,
                SpaceCenterFacility.ResearchAndDevelopment,
                SpaceCenterFacility.Administration,
                SpaceCenterFacility.LaunchPad
            };

            int totalLevel  = 0;
            int maxPossible = facilities.Length * 2;

            foreach (var fac in facilities)
            {
                float levelNorm = ScenarioUpgradeableFacilities.GetFacilityLevel(fac);
                totalLevel += Mathf.RoundToInt(levelNorm * 2f);
            }

            double normalized = (double)totalLevel / maxPossible;
            return FacilityMultiplierBase + normalized * (FacilityMultiplierMax - FacilityMultiplierBase);
        }

        /// <summary>
        /// Calculates the penalty multiplier for this deposit cycle.
        /// Side-effect: advances ConsecutivePenaltyYears.
        /// </summary>
        public double CalculatePenaltyMultiplier(out bool frozen, out int yearsUsed)
        {
            frozen    = false;
            yearsUsed = ConsecutivePenaltyYears;

            if (Reputation.Instance == null)
                return 1.0;

            float rep = Reputation.Instance.reputation;

            if (rep >= (float)RepPenaltyThreshold)
            {
                // Reputation is healthy – reset streak
                ConsecutivePenaltyYears = 0;
                return 1.0;
            }

            // Rep is below threshold: advance streak then evaluate
            ConsecutivePenaltyYears++;
            yearsUsed = ConsecutivePenaltyYears;

            if (ConsecutivePenaltyYears >= PenaltyFreezeAfterYears)
            {
                frozen = true;
                return 0.0;
            }

            // Escalating cut: 15% per consecutive year, floored at MaxPenaltyMultiplier
            double cut = 1.0 - (PenaltyPerYear * ConsecutivePenaltyYears);
            return Math.Max(cut, MaxPenaltyMultiplier);
        }

        /// <summary>
        /// Preview version of the penalty multiplier – does NOT advance the counter.
        /// Used by the UI to show projected next deposit.
        /// </summary>
        public double PreviewPenaltyMultiplier(out bool wouldFreeze)
        {
            wouldFreeze = false;
            if (Reputation.Instance == null) return 1.0;

            float rep = Reputation.Instance.reputation;
            if (rep >= (float)RepPenaltyThreshold) return 1.0;

            int nextYear = ConsecutivePenaltyYears + 1;
            if (nextYear >= PenaltyFreezeAfterYears)
            {
                wouldFreeze = true;
                return 0.0;
            }

            double cut = 1.0 - (PenaltyPerYear * nextYear);
            return Math.Max(cut, MaxPenaltyMultiplier);
        }

        // ── Year / time helpers (public so UI can use them) ──────────────────────

        public static double GetYearLength()
        {
            try
            {
                if (FlightGlobals.Bodies == null || FlightGlobals.Bodies.Count == 0)
                    return 9203545.0;
                CelestialBody homeWorld = FlightGlobals.GetHomeBody();
                if (homeWorld?.orbitDriver?.orbit != null)
                    return homeWorld.orbitDriver.orbit.period;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[KFA] Could not get home body orbit: {e.Message}");
            }
            return 9203545.0; // fallback: stock Kerbin year
        }

        public static int GetCurrentYear()
        {
            double now = Planetarium.GetUniversalTime();
            return (int)Math.Floor(now / GetYearLength()) + 1;
        }

        /// <summary>Seconds until the next budget deposit.</summary>
        public double SecondsUntilNextBudget()
        {
            if (LastBudgetUT < 0) return GetYearLength();
            double nextUT = LastBudgetUT + GetYearLength();
            return Math.Max(0.0, nextUT - Planetarium.GetUniversalTime());
        }

        /// <summary>Formats seconds as "Xd Xh Xm" using the home body day length.</summary>
        public static string FormatTime(double seconds)
        {
            double dayLength = 0;
            try
            {
                CelestialBody home = FlightGlobals.GetHomeBody();
                dayLength = home?.solarDayLength ?? 21600.0;
            }
            catch { dayLength = 21600.0; }

            int days    = (int)(seconds / dayLength);
            int hours   = (int)((seconds % dayLength) / 3600);
            int minutes = (int)((seconds % 3600) / 60);
            return $"{days}d {hours:D2}h {minutes:D2}m";
        }

        // ── Message log ──────────────────────────────────────────────────────────

        private void PostBudgetMessage(double total, double repMult, double facMult,
                                       double penMult, bool frozen)
        {
            int year = GetCurrentYear();
            string title = frozen
                ? $"⚠ Budget FROZEN – Year {year}"
                : $"Annual Budget Allocated – Year {year}";

            string body;
            if (frozen)
            {
                body = "<color=#FF4444><b>FUNDING FROZEN</b></color>\n" +
                       $"Your agency has had poor reputation for {ConsecutivePenaltyYears} consecutive year(s).\n" +
                       "The government has suspended your budget allocation this year.\n" +
                       "Improve your reputation above " +
                       $"{RepPenaltyThreshold:F0} to restore funding.";
            }
            else
            {
                string penLine = penMult < 1.0
                    ? $"\n<color=#FF8844>Penalty modifier:   ×{penMult:F2}  ({ConsecutivePenaltyYears} yr low rep)</color>"
                    : "";

                body = $"<b>{title}</b>\n" +
                       $"Base allocation:    {BaseAllocation:N0} funds\n" +
                       $"Reputation mod:     ×{repMult:F2}  (Rep: {Reputation.Instance?.reputation:F0})" +
                       penLine + "\n" +
                       $"Facility mod:       ×{facMult:F2}\n" +
                       $"<color=#7CFC00><b>Total deposited: {total:N0} funds</b></color>";
            }

            var m = new MessageSystem.Message(
                title, body,
                frozen ? MessageSystemButton.MessageButtonColor.RED
                       : MessageSystemButton.MessageButtonColor.GREEN,
                MessageSystemButton.ButtonIcons.ALERT
            );
            MessageSystem.Instance.AddMessage(m);
        }

        // ── Runtime settings application ─────────────────────────────────────────

        /// <summary>
        /// Called by BudgetUI to push edited values into the live controller.
        /// Changes take effect immediately. Settings are saved with the career save file.
        /// </summary>
        public void ApplySettings(
            double baseAlloc, double minRep, double maxRep,
            double facBase,   double facMax,
            double penThreshold, double penPerYear, double maxPenMult, int penFreezeYears,
            bool showBreakdown)
        {
            BaseAllocation         = baseAlloc;
            MinRepMultiplier       = minRep;
            MaxRepMultiplier       = maxRep;
            FacilityMultiplierBase = facBase;
            FacilityMultiplierMax  = facMax;
            RepPenaltyThreshold    = penThreshold;
            PenaltyPerYear         = penPerYear;
            MaxPenaltyMultiplier   = maxPenMult;
            PenaltyFreezeAfterYears = penFreezeYears;
            ShowBreakdown          = showBreakdown;

            Debug.Log($"[KFA] Settings applied in-game. Base={BaseAllocation:N0}");
        }


        // ── Config loading ───────────────────────────────────────────────────────

        private void LoadConfig()
        {
            // Read directly from disk so saved settings are picked up after
            // every game reload, not just on first KSP launch.
            // GameDatabase caches cfg files at startup and never rescans,
            // so any settings saved in-game would be lost on reload if we used it.
            string fullPath = System.IO.Path.Combine(
                KSPUtil.ApplicationRootPath,
                "GameData",
                "KerbalFundingAgency",
                "KerbalFundingAgency.cfg");

            if (!System.IO.File.Exists(fullPath))
            {
                Debug.LogWarning($"[KFA] Config file not found at {fullPath} – using defaults.");
                return;
            }

            ConfigNode root = ConfigNode.Load(fullPath);
            if (root == null)
            {
                Debug.LogWarning("[KFA] Failed to parse config file – using defaults.");
                return;
            }

            ConfigNode node = root.GetNode("KFA_SETTINGS");
            if (node == null)
            {
                Debug.LogWarning("[KFA] KFA_SETTINGS node not found in config – using defaults.");
                return;
            }

            double tmp;
            if (TryParseDouble(node, "baseAllocation",          out tmp)) BaseAllocation         = tmp;
            if (TryParseDouble(node, "minRepMultiplier",         out tmp)) MinRepMultiplier       = tmp;
            if (TryParseDouble(node, "maxRepMultiplier",         out tmp)) MaxRepMultiplier       = tmp;
            if (TryParseDouble(node, "facilityMultiplierBase",   out tmp)) FacilityMultiplierBase = tmp;
            if (TryParseDouble(node, "facilityMultiplierMax",    out tmp)) FacilityMultiplierMax  = tmp;
            if (TryParseDouble(node, "repPenaltyThreshold",      out tmp)) RepPenaltyThreshold    = tmp;
            if (TryParseDouble(node, "penaltyPerYear",           out tmp)) PenaltyPerYear         = tmp;
            if (TryParseDouble(node, "maxPenaltyMultiplier",     out tmp)) MaxPenaltyMultiplier   = tmp;

            int itmp;
            string v = node.GetValue("penaltyFreezeAfterYears");
            if (!string.IsNullOrEmpty(v) && int.TryParse(v, out itmp)) PenaltyFreezeAfterYears = itmp;

            v = node.GetValue("showBreakdown");
            if (!string.IsNullOrEmpty(v) && bool.TryParse(v, out bool btmp)) ShowBreakdown = btmp;

            Debug.Log($"[KFA] Config loaded from disk. Base={BaseAllocation:N0}, " +
                      $"PenaltyThreshold={RepPenaltyThreshold}, FreezeAfter={PenaltyFreezeAfterYears}yr");
        }

        private static bool TryParseDouble(ConfigNode node, string key, out double result)
        {
            result = 0;
            string val = node.GetValue(key);
            return !string.IsNullOrEmpty(val) && double.TryParse(val, out result);
        }

        // ── Persistence ──────────────────────────────────────────────────────────

    }

    // ── ScenarioModule ────────────────────────────────────────────────────────
    // KSP's ScenarioModule system guarantees OnSave/OnLoad fire at the correct
    // point in the save/load cycle. This is the standard KSP pattern for
    // persisting mod data inside career saves.

    [KSPScenario(
        ScenarioCreationOptions.AddToExistingCareerGames |
        ScenarioCreationOptions.AddToNewCareerGames,
        GameScenes.FLIGHT, GameScenes.SPACECENTER)]
    public class KFAScenario : ScenarioModule
    {
        // Cache the loaded node so KFAController can pull from it once ready.
        // OnLoad fires before KFAController.Awake() sets Instance, so we store
        // the data here and apply it when the controller calls ApplyLoadedData().
        public static ConfigNode PendingLoad { get; private set; } = null;

        public override void OnSave(ConfigNode node)
        {
            KFAController bc = KFAController.Instance;
            if (bc == null) { Debug.LogWarning("[KFA] OnSave: KFAController not found."); return; }

            // Budget state
            node.AddValue("lastBudgetUT",            bc.LastBudgetUT.ToString("R"));
            node.AddValue("consecutivePenaltyYears", bc.ConsecutivePenaltyYears.ToString());
            node.AddValue("frozenLastYear",          bc.FrozenLastYear.ToString());

            // Settings
            node.AddValue("baseAllocation",          bc.BaseAllocation.ToString("R"));
            node.AddValue("minRepMultiplier",        bc.MinRepMultiplier.ToString("R"));
            node.AddValue("maxRepMultiplier",        bc.MaxRepMultiplier.ToString("R"));
            node.AddValue("facilityMultiplierBase",  bc.FacilityMultiplierBase.ToString("R"));
            node.AddValue("facilityMultiplierMax",   bc.FacilityMultiplierMax.ToString("R"));
            node.AddValue("repPenaltyThreshold",     bc.RepPenaltyThreshold.ToString("R"));
            node.AddValue("penaltyPerYear",          bc.PenaltyPerYear.ToString("R"));
            node.AddValue("maxPenaltyMultiplier",    bc.MaxPenaltyMultiplier.ToString("R"));
            node.AddValue("penaltyFreezeAfterYears", bc.PenaltyFreezeAfterYears.ToString());
            node.AddValue("showBreakdown",           bc.ShowBreakdown.ToString());

            Debug.Log($"[KFA] Scenario saved. Base={bc.BaseAllocation:N0}, UT={bc.LastBudgetUT:F0}");
        }

        public override void OnLoad(ConfigNode node)
        {
            // Store the node — KFAController will apply it in Start() once
            // its Instance is set and LoadConfig() has run.
            PendingLoad = node;
            Debug.Log("[KFA] Scenario node cached for deferred load.");
        }
    }

} // end namespace KFA
