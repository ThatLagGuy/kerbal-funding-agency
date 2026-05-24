using System;
using System.IO;
using UnityEngine;
using KSP.UI.Screens;

namespace KFA
{
    /// <summary>
    /// BudgetUI – Toolbar button, overview window, and settings panel.
    ///
    /// Tab 0 – Overview: countdown, projected breakdown, rep status, facility bars.
    /// Tab 1 – Settings: sliders + text field for all config values, save to cfg.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.AllGameScenes, false)]
    public class BudgetUI : MonoBehaviour
    {
        // ── Window ───────────────────────────────────────────────────────────────
        private const int   WindowId    = 0x42_55_44_47;
        private const float WinW        = 360f;
        private Rect        windowRect  = new Rect(Screen.width - 400f, 80f, WinW, 100f);

        // ── State ────────────────────────────────────────────────────────────────
        private ApplicationLauncherButton toolbarButton;
        private bool windowVisible = false;
        private int  activeTab     = 0;   // 0 = Overview, 1 = Settings

        // Overview cache
        private double cachedRepMult     = 1.0;
        private double cachedFacMult     = 1.0;
        private double cachedPenMult     = 1.0;
        private double cachedProjected   = 0.0;
        private bool   cachedWouldFreeze = false;
        private string cachedTimeStr     = "—";
        private float  lastRefresh       = -999f;

        // Settings editing state (mirror of KFAController values, edited live)
        private double  sBaseAllocation;
        private string  sBaseAllocationStr;   // text field buffer
        private bool    sBaseAllocationValid = true;
        private double  sMinRepMult;
        private double  sMaxRepMult;
        private double  sFacBase;
        private double  sFacMax;
        private double  sPenThreshold;
        private double  sPenPerYear;
        private int     sPenFreezeYears;
        private bool    sShowBreakdown;
        private string  sSaveStatus = "";     // "Saved!" or error text
        private float   sSaveStatusTime = 0f;

        // GUIStyles (built once)
        private GUIStyle styleTabActive;
        private GUIStyle styleTabInactive;
        private GUIStyle styleHeader;
        private GUIStyle styleLabel;
        private GUIStyle styleMuted;
        private GUIStyle styleValue;
        private GUIStyle styleWarning;
        private GUIStyle styleGood;
        private GUIStyle styleDivider;
        private GUIStyle styleSmall;
        private bool     stylesBuilt = false;

        // ── Lifecycle ────────────────────────────────────────────────────────────

        private void Start()
        {
            GameEvents.onGUIApplicationLauncherReady.Add(AddButton);
            GameEvents.onGUIApplicationLauncherDestroyed.Add(RemoveButton);
        }

        private void OnDestroy()
        {
            GameEvents.onGUIApplicationLauncherReady.Remove(AddButton);
            GameEvents.onGUIApplicationLauncherDestroyed.Remove(RemoveButton);
            RemoveButton();
        }

        private void AddButton()
        {
            if (toolbarButton != null) return;
            if (ApplicationLauncher.Instance == null) return;

            toolbarButton = ApplicationLauncher.Instance.AddModApplication(
                onTrue:     OnToolbarOn,
                onFalse:    OnToolbarOff,
                onHover:    null, onHoverOut: null, onEnable: null, onDisable: null,
                visibleInScenes: ApplicationLauncher.AppScenes.SPACECENTER |
                                 ApplicationLauncher.AppScenes.FLIGHT,
                texture: GameDatabase.Instance.GetTexture("KerbalFundingAgency/Textures/icon", false)
                         ?? Texture2D.whiteTexture
            );
        }

        private void RemoveButton()
        {
            if (toolbarButton == null) return;
            ApplicationLauncher.Instance?.RemoveModApplication(toolbarButton);
            toolbarButton = null;
        }

        private void OnToolbarOn()
        {
            windowVisible = true;
            SyncSettingsFromController();
            RefreshOverview();
        }

        private void OnToolbarOff() => windowVisible = false;

        // ── Update ───────────────────────────────────────────────────────────────

        private void Update()
        {
            if (!windowVisible) return;
            if (activeTab == 0 && Time.realtimeSinceStartup - lastRefresh > 2f)
                RefreshOverview();
        }

        // ── Overview data refresh ────────────────────────────────────────────────

        private void RefreshOverview()
        {
            lastRefresh = Time.realtimeSinceStartup;
            KFAController bc = KFAController.Instance;
            if (bc == null) return;

            cachedRepMult      = bc.CalculateReputationMultiplier();
            cachedFacMult      = bc.CalculateFacilityMultiplier();
            cachedPenMult      = bc.PreviewPenaltyMultiplier(out cachedWouldFreeze);
            cachedProjected    = cachedWouldFreeze ? 0.0
                                 : bc.BaseAllocation * cachedRepMult * cachedFacMult * cachedPenMult;
            cachedTimeStr      = KFAController.FormatTime(bc.SecondsUntilNextBudget());
        }

        // ── Settings sync ────────────────────────────────────────────────────────

        /// <summary>Pull current live values from KFAController into editing state.</summary>
        private void SyncSettingsFromController()
        {
            KFAController bc = KFAController.Instance;
            if (bc == null) return;

            sBaseAllocation    = bc.BaseAllocation;
            sBaseAllocationStr = ((long)bc.BaseAllocation).ToString();
            sMinRepMult        = bc.MinRepMultiplier;
            sMaxRepMult        = bc.MaxRepMultiplier;
            sFacBase           = bc.FacilityMultiplierBase;
            sFacMax            = bc.FacilityMultiplierMax;
            sPenThreshold      = bc.RepPenaltyThreshold;
            sPenPerYear        = bc.PenaltyPerYear;
            sPenFreezeYears    = bc.PenaltyFreezeAfterYears;
            sShowBreakdown     = bc.ShowBreakdown;
            sBaseAllocationValid = true;
        }

        /// <summary>Push editing state back into KFAController so changes apply immediately.</summary>
        private void ApplySettingsToController()
        {
            KFAController bc = KFAController.Instance;
            if (bc == null) return;
            bc.ApplySettings(
                sBaseAllocation, sMinRepMult, sMaxRepMult,
                sFacBase, sFacMax,
                sPenThreshold, sPenPerYear, bc.MaxPenaltyMultiplier, sPenFreezeYears,
                sShowBreakdown
            );
            RefreshOverview();
        }

        // ── GUI ──────────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            if (!windowVisible) return;
            BuildStyles();

            windowRect = GUILayout.Window(WindowId, windowRect, DrawWindow,
                "Government Budget", HighLogic.Skin.window, GUILayout.Width(WinW));

            windowRect.x = Mathf.Clamp(windowRect.x, 0, Screen.width  - windowRect.width);
            windowRect.y = Mathf.Clamp(windowRect.y, 0, Screen.height - windowRect.height);
        }

        private void DrawWindow(int id)
        {
            // ── Tab bar ──────────────────────────────────────────────────────────
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Overview",  activeTab == 0 ? styleTabActive : styleTabInactive))
                activeTab = 0;
            if (GUILayout.Button("Settings",  activeTab == 1 ? styleTabActive : styleTabInactive))
            {
                if (activeTab != 1) SyncSettingsFromController();
                activeTab = 1;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(4f);

            if (activeTab == 0) DrawOverviewTab();
            else                DrawSettingsTab();

            GUI.DragWindow(new Rect(0, 0, WinW, 24f));
        }

        // ── Overview tab ─────────────────────────────────────────────────────────

        private void DrawOverviewTab()
        {
            KFAController bc = KFAController.Instance;
            if (bc == null) { GUILayout.Label("KFAController not loaded.", styleMuted); return; }

            DrawSection("NEXT DEPOSIT");
            DrawRow("Time remaining:", cachedTimeStr,
                    cachedWouldFreeze ? styleWarning : styleGood);
            DrawRow("Year:", KFAController.GetCurrentYear().ToString(), styleValue);

            GUILayout.Space(4f);
            DrawSection("PROJECTED BREAKDOWN");
            DrawRow("Base allocation:", $"{bc.BaseAllocation:N0} ₭", styleValue);
            DrawRow("Reputation mod:", $"×{cachedRepMult:F2}",
                    cachedRepMult >= 1.0 ? styleGood : styleWarning);
            DrawRow("Facility mod:",   $"×{cachedFacMult:F2}", styleValue);

            if (bc.ConsecutivePenaltyYears > 0
                || (Reputation.Instance != null && Reputation.Instance.reputation < (float)bc.RepPenaltyThreshold))
            {
                string penLabel = cachedWouldFreeze
                    ? "Penalty: FREEZE"
                    : $"Penalty ({bc.ConsecutivePenaltyYears + 1}yr):";
                DrawRow(penLabel,
                        cachedWouldFreeze ? "×0 – FROZEN" : $"×{cachedPenMult:F2}",
                        styleWarning);
            }

            GUILayout.Box("", styleDivider);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Projected total:", styleHeader);
            GUILayout.FlexibleSpace();
            GUILayout.Label(cachedWouldFreeze ? "FROZEN" : $"{cachedProjected:N0} ₭",
                            cachedWouldFreeze ? styleWarning : styleGood);
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);
            DrawSection("REPUTATION STATUS");
            float rep = Reputation.Instance?.reputation ?? 0f;
            DrawRow("Current rep:", $"{rep:F0} / 1000",
                    rep >= (float)bc.RepPenaltyThreshold ? styleGood : styleWarning);
            DrawRow("Penalty threshold:", $"{bc.RepPenaltyThreshold:F0}", styleMuted);

            if (bc.ConsecutivePenaltyYears > 0)
            {
                int yearsLeft = bc.PenaltyFreezeAfterYears - bc.ConsecutivePenaltyYears;
                string msg = yearsLeft > 0 ? $"Freeze in {yearsLeft} more yr" : "Frozen this year!";
                DrawRow("Low-rep streak:", $"{bc.ConsecutivePenaltyYears} yr – {msg}", styleWarning);
            }
            else
            {
                DrawRow("Low-rep streak:", "None", styleGood);
            }

            GUILayout.Space(6f);
            DrawSection("FACILITY LEVELS");
            DrawFacilityRow("VAB",   SpaceCenterFacility.VehicleAssemblyBuilding);
            DrawFacilityRow("R&D",   SpaceCenterFacility.ResearchAndDevelopment);
            DrawFacilityRow("Admin", SpaceCenterFacility.Administration);
            DrawFacilityRow("Pad",   SpaceCenterFacility.LaunchPad);

            GUILayout.Space(6f);
            if (GUILayout.Button("Close", HighLogic.Skin.button))
            {
                windowVisible = false;
                toolbarButton?.SetFalse(makeCall: false);
            }
        }

        // ── Settings tab ─────────────────────────────────────────────────────────

        private void DrawSettingsTab()
        {
            // ── Economy ──────────────────────────────────────────────────────────
            DrawSection("ECONOMY");

            // Base allocation – text field (large number, slider impractical)
            GUILayout.BeginHorizontal();
            GUILayout.Label("Base allocation (₭/yr)", styleLabel, GUILayout.Width(190f));
            GUILayout.FlexibleSpace();
            GUI.backgroundColor = sBaseAllocationValid ? Color.white : new Color(1f, 0.4f, 0.4f);
            string newAllocStr = GUILayout.TextField(sBaseAllocationStr,
                                     HighLogic.Skin.textField, GUILayout.Width(90f));
            GUI.backgroundColor = Color.white;
            if (newAllocStr != sBaseAllocationStr)
            {
                sBaseAllocationStr = newAllocStr;
                string clean = newAllocStr.Replace(",", "").Replace(" ", "");
                if (double.TryParse(clean, out double parsed) && parsed > 0)
                {
                    sBaseAllocation      = parsed;
                    sBaseAllocationValid = true;
                    ApplySettingsToController();
                }
                else
                {
                    sBaseAllocationValid = false;
                }
            }
            GUILayout.EndHorizontal();
            if (!sBaseAllocationValid)
                GUILayout.Label("  Enter a positive number", styleWarning);

            GUILayout.Space(4f);

            sMinRepMult = DrawSlider("Min rep multiplier", sMinRepMult, 0.1, 1.0, "×{0:F2}");
            sMaxRepMult = DrawSlider("Max rep multiplier", sMaxRepMult,
                          Math.Max(sMinRepMult + 0.1, 1.0), 5.0, "×{0:F2}");
            sFacBase    = DrawSlider("Facility min mult.",  sFacBase,  0.5, 2.0, "×{0:F2}");
            sFacMax     = DrawSlider("Facility max mult.",  sFacMax,
                          Math.Max(sFacBase + 0.1, 1.0), 6.0, "×{0:F2}");

            GUILayout.Space(2f);
            GUILayout.Box("", styleDivider);
            DrawSection("PENALTY SYSTEM");

            sPenThreshold  = DrawSlider("Penalty threshold (rep)", sPenThreshold, 0, 600, "{0:F0}");
            double penPct  = DrawSlider("Cut per year (%)", sPenPerYear * 100.0, 5.0, 50.0, "{0:F0}%");
            sPenPerYear    = penPct / 100.0;
            sPenFreezeYears = DrawSliderInt("Freeze after (years)", sPenFreezeYears, 1, 10, "{0} yr");

            GUILayout.Space(2f);
            GUILayout.Box("", styleDivider);
            DrawSection("DISPLAY");

            GUILayout.BeginHorizontal();
            GUILayout.Label("Show yearly breakdown msg", styleLabel, GUILayout.Width(220f));
            GUILayout.FlexibleSpace();
            bool newShow = GUILayout.Toggle(sShowBreakdown, "", HighLogic.Skin.toggle);
            if (newShow != sShowBreakdown) { sShowBreakdown = newShow; ApplySettingsToController(); }
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);

            // ── Action buttons ───────────────────────────────────────────────────
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Reset defaults", HighLogic.Skin.button))
            {
                ResetToDefaults();
                ApplySettingsToController();
            }

            if (GUILayout.Button("Save to career", HighLogic.Skin.button))
            {
                if (sBaseAllocationValid)
                {
                    ApplySettingsToController();
                    // Trigger KSP's save system to write the current game state,
                    // which includes our settings via OnSave()
                    if (HighLogic.CurrentGame != null)
                    {
                        GamePersistence.SaveGame(
                            HighLogic.CurrentGame,
                            HighLogic.CurrentGame.Title,
                            HighLogic.SaveFolder,
                            SaveMode.OVERWRITE);
                        sSaveStatus     = "Saved to career!";
                        sSaveStatusTime = Time.realtimeSinceStartup;
                        Debug.Log("[KFA] Career save triggered from settings panel.");
                    }
                    else
                    {
                        sSaveStatus     = "No active career save found.";
                        sSaveStatusTime = Time.realtimeSinceStartup;
                    }
                }
            }

            GUILayout.EndHorizontal();

            // Save status feedback (fades after 3 s)
            if (!string.IsNullOrEmpty(sSaveStatus)
                && Time.realtimeSinceStartup - sSaveStatusTime < 3f)
            {
                bool isErr = sSaveStatus.StartsWith("Save failed");
                GUILayout.Label(sSaveStatus, isErr ? styleWarning : styleGood);
            }

            GUILayout.Space(4f);
            GUILayout.Label("Changes apply immediately in-game.\n" +
                            "\"Save to career\" writes settings to your save file.",
                            styleSmall);

            GUILayout.Space(4f);
            if (GUILayout.Button("Close", HighLogic.Skin.button))
            {
                windowVisible = false;
                toolbarButton?.SetFalse(makeCall: false);
            }
        }

        // ── Slider helpers ───────────────────────────────────────────────────────

        /// <summary>Draws a labelled slider, returns the new value, and calls ApplySettings if changed.</summary>
        private double DrawSlider(string label, double current, double min, double max, string fmt)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, styleLabel, GUILayout.Width(190f));
            float newVal = GUILayout.HorizontalSlider(
                (float)current, (float)min, (float)max,
                HighLogic.Skin.horizontalSlider, HighLogic.Skin.horizontalSliderThumb,
                GUILayout.ExpandWidth(true));
            // Snap to two decimal places to avoid float drift
            double rounded = Math.Round(newVal, 2);
            GUILayout.Label(string.Format(fmt, rounded), styleValue, GUILayout.Width(50f));
            GUILayout.EndHorizontal();

            if (Math.Abs(rounded - current) > 0.001)
            {
                ApplySettingsToController();
                return rounded;
            }
            return current;
        }

        private int DrawSliderInt(string label, int current, int min, int max, string fmt)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, styleLabel, GUILayout.Width(190f));
            float newVal = GUILayout.HorizontalSlider(
                (float)current, (float)min, (float)max,
                HighLogic.Skin.horizontalSlider, HighLogic.Skin.horizontalSliderThumb,
                GUILayout.ExpandWidth(true));
            int rounded = Mathf.RoundToInt(newVal);
            GUILayout.Label(string.Format(fmt, rounded), styleValue, GUILayout.Width(50f));
            GUILayout.EndHorizontal();

            if (rounded != current)
            {
                ApplySettingsToController();
                return rounded;
            }
            return current;
        }

        // ── Shared drawing helpers ───────────────────────────────────────────────

        private void DrawSection(string text)
        {
            GUILayout.Label(text, styleHeader);
            GUILayout.Box("", styleDivider);
        }

        private void DrawRow(string label, string value, GUIStyle valueStyle)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, styleLabel, GUILayout.Width(175f));
            GUILayout.FlexibleSpace();
            GUILayout.Label(value, valueStyle);
            GUILayout.EndHorizontal();
        }

        private void DrawFacilityRow(string name, SpaceCenterFacility fac)
        {
            float levelNorm = ScenarioUpgradeableFacilities.GetFacilityLevel(fac);
            int   level     = Mathf.RoundToInt(levelNorm * 2f);
            string bar      = level == 0 ? "[ ][ ][ ]"
                            : level == 1 ? "[█][ ][ ]"
                                         : "[█][█][█]";
            DrawRow(name, bar, level == 2 ? styleGood : level == 1 ? styleValue : styleMuted);
        }

        // ── Defaults ─────────────────────────────────────────────────────────────

        private void ResetToDefaults()
        {
            sBaseAllocation     = 150000.0;
            sBaseAllocationStr  = "150000";
            sBaseAllocationValid = true;
            sMinRepMult         = 0.5;
            sMaxRepMult         = 2.0;
            sFacBase            = 1.0;
            sFacMax             = 2.5;
            sPenThreshold       = 200.0;
            sPenPerYear         = 0.15;
            sPenFreezeYears     = 3;
            sShowBreakdown      = true;
        }

        // ── Style builder ────────────────────────────────────────────────────────

        private void BuildStyles()
        {
            if (stylesBuilt) return;
            stylesBuilt = true;

            styleTabActive = new GUIStyle(HighLogic.Skin.button)
            {
                fontSize = 12, fontStyle = FontStyle.Bold,
                normal   = { textColor = new Color(0.4f, 0.8f, 1f) }
            };
            styleTabInactive = new GUIStyle(HighLogic.Skin.button)
            {
                fontSize = 12,
                normal   = { textColor = new Color(0.7f, 0.7f, 0.7f) }
            };
            styleHeader = new GUIStyle(HighLogic.Skin.label)
            {
                fontSize = 11, fontStyle = FontStyle.Bold,
                normal   = { textColor = new Color(0.7f, 0.85f, 1f) }
            };
            styleLabel = new GUIStyle(HighLogic.Skin.label)
            {
                fontSize = 12,
                normal   = { textColor = new Color(0.8f, 0.8f, 0.8f) }
            };
            styleMuted = new GUIStyle(HighLogic.Skin.label)
            {
                fontSize = 12,
                normal   = { textColor = new Color(0.55f, 0.55f, 0.55f) }
            };
            styleValue = new GUIStyle(HighLogic.Skin.label)
            {
                fontSize = 12, fontStyle = FontStyle.Bold,
                normal   = { textColor = Color.white }
            };
            styleWarning = new GUIStyle(HighLogic.Skin.label)
            {
                fontSize = 12, fontStyle = FontStyle.Bold,
                normal   = { textColor = new Color(1f, 0.55f, 0.2f) }
            };
            styleGood = new GUIStyle(HighLogic.Skin.label)
            {
                fontSize = 12, fontStyle = FontStyle.Bold,
                normal   = { textColor = new Color(0.4f, 1f, 0.4f) }
            };
            styleDivider = new GUIStyle(GUI.skin.box)
            {
                fixedHeight = 1f,
                margin      = new RectOffset(0, 0, 2, 4),
                padding     = new RectOffset(0, 0, 0, 0),
                normal      = { background = MakeTex(1, 1, new Color(0.4f, 0.4f, 0.4f, 0.5f)) }
            };
            styleSmall = new GUIStyle(HighLogic.Skin.label)
            {
                fontSize  = 11,
                wordWrap  = true,
                normal    = { textColor = new Color(0.55f, 0.55f, 0.55f) }
            };
        }

        private static Texture2D MakeTex(int w, int h, Color c)
        {
            var t = new Texture2D(w, h);
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }
    }
}
