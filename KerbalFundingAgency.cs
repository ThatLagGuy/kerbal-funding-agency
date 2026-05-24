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
        private const string ScenarioName = "KFAScenario";

        // ── Internal state ───────────────────────────────────────────────────────
        public double LastBudgetUT          { get; private set; } = -1.0;
        public int    ConsecutivePenaltyYears { get; private set; } = 0;
        public bool   FrozenLastYear        { get; private set; } = false;
        private bool  initialized           = false;

        // ── Config (loaded from KerbalFundingAgency.cfg) ──────────────────────────────────
        public double BaseAllocation          { get; private set; } = 150000.0;
        public double MinRepMultiplier        { get; private set; } = 0.5;
        public double MaxRepMultiplier        { get; private set; } = 2.0;
        public double FacilityMultiplierBase  { get; private set; } = 1.0;
        public double FacilityMultiplierMax   { get; private set; } = 2.5;

        // Penalty config
        public double RepPenaltyThreshold     { get; private set; } = 200.0;   // rep below this → penalty
        public double PenaltyPerYear          { get; private set; } = 0.15;    // 15% cut per consecutive year
        public double MaxPenaltyMultiplier    { get; private set; } = 0.6;     // floor: never below 40% cut
        public int    PenaltyFreezeAfterYears { get; private set; } = 3;       // freeze after N years in a row

        public bool ShowBreakdown             { get; private set; } = true;

        // ── Lifecycle ────────────────────────────────────────────────────────────

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            LoadConfig();
            GameEvents.onGameStateSaved.Add(OnSave);
            GameEvents.onGameStateLoad.Add(OnLoad);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            GameEvents.onGameStateSaved.Remove(OnSave);
            GameEvents.onGameStateLoad.Remove(OnLoad);
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
        /// Changes take effect immediately; call SaveConfig() to persist to disk.
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

        /// <summary>
        /// Writes current in-memory settings back to KerbalFundingAgency.cfg on disk.
        /// Returns true on success.
        /// </summary>
        public bool SaveConfig()
        {
            try
            {
                // Find the cfg file path via GameDatabase so we use the correct GameData location
                UrlDir.UrlConfig[] configs = GameDatabase.Instance.GetConfigs("KFA_SETTINGS");
                if (configs == null || configs.Length == 0)
                {
                    Debug.LogError("[KFA] SaveConfig: Could not find KFA_SETTINGS in GameDatabase.");
                    return false;
                }

                string cfgPath = configs[0].url;
                // configs[0].url is a GameData-relative path without extension,
                // e.g. "KerbalFundingAgency/KerbalFundingAgency". Resolve to absolute path.
                string fullPath = System.IO.Path.Combine(
                    KSPUtil.ApplicationRootPath, "GameData", cfgPath + ".cfg");

                string contents =
                    "// Kerbal Funding Agency – Government Budget Allocation Settings\n" +
                    "// Auto-saved from in-game settings panel.\n\n" +
                    "KFA_SETTINGS\n{\n" +
                    $"    baseAllocation         = {BaseAllocation:F0}\n" +
                    $"    minRepMultiplier       = {MinRepMultiplier:F2}\n" +
                    $"    maxRepMultiplier       = {MaxRepMultiplier:F2}\n" +
                    $"    facilityMultiplierBase = {FacilityMultiplierBase:F2}\n" +
                    $"    facilityMultiplierMax  = {FacilityMultiplierMax:F2}\n" +
                    $"    repPenaltyThreshold    = {RepPenaltyThreshold:F0}\n" +
                    $"    penaltyPerYear         = {PenaltyPerYear:F2}\n" +
                    $"    maxPenaltyMultiplier   = {MaxPenaltyMultiplier:F2}\n" +
                    $"    penaltyFreezeAfterYears = {PenaltyFreezeAfterYears}\n" +
                    $"    showBreakdown          = {ShowBreakdown.ToString().ToLower()}\n" +
                    "}\n";

                System.IO.File.WriteAllText(fullPath, contents);
                Debug.Log($"[KFA] Config saved to {fullPath}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[KFA] SaveConfig failed: {e.Message}");
                return false;
            }
        }

        // ── Config loading ───────────────────────────────────────────────────────

        private void LoadConfig()
        {
            UrlDir.UrlConfig[] configs = GameDatabase.Instance.GetConfigs("KFA_SETTINGS");
            if (configs == null || configs.Length == 0)
            {
                Debug.LogWarning("[KFA] No config found – using defaults.");
                return;
            }

            ConfigNode node = configs[0].config;

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

            Debug.Log($"[KFA] Config loaded. Base={BaseAllocation:N0}, " +
                      $"PenaltyThreshold={RepPenaltyThreshold}, FreezeAfter={PenaltyFreezeAfterYears}yr");
        }

        private static bool TryParseDouble(ConfigNode node, string key, out double result)
        {
            result = 0;
            string val = node.GetValue(key);
            return !string.IsNullOrEmpty(val) && double.TryParse(val, out result);
        }

        // ── Persistence ──────────────────────────────────────────────────────────

        private void OnSave(Game game)
        {
            ConfigNode s = game.config.AddNode(ScenarioName);
            s.AddValue("lastBudgetUT",            LastBudgetUT.ToString("R"));
            s.AddValue("consecutivePenaltyYears", ConsecutivePenaltyYears.ToString());
            s.AddValue("frozenLastYear",          FrozenLastYear.ToString());
        }

        private void OnLoad(ConfigNode gameNode)
        {
            ConfigNode s = gameNode.GetNode(ScenarioName);
            if (s == null) return;

            string v = s.GetValue("lastBudgetUT");
            if (!string.IsNullOrEmpty(v) && double.TryParse(v, out double d)) LastBudgetUT = d;

            v = s.GetValue("consecutivePenaltyYears");
            if (!string.IsNullOrEmpty(v) && int.TryParse(v, out int i)) ConsecutivePenaltyYears = i;

            v = s.GetValue("frozenLastYear");
            if (!string.IsNullOrEmpty(v) && bool.TryParse(v, out bool b)) FrozenLastYear = b;
        }
    }
}
