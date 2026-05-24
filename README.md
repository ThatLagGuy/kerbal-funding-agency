# Kerbal Funding Agency – KSP Career Annual Budget

Adds a government-style annual budget to KSP career mode. Every in-game year your
space agency receives a fund allocation based on reputation and KSC facility levels.
Persistent low reputation triggers escalating budget cuts, and eventually a full
funding freeze. A toolbar window shows your projected next deposit at any time.

---

## Formula

```
annual_budget = baseAllocation × reputationMultiplier × facilityMultiplier × penaltyMultiplier
```

| Factor | Range | Description |
|---|---|---|
| `baseAllocation` | configurable | Starting funds before any bonuses |
| `reputationMultiplier` | 0.5 – 2.0 | Scales linearly with reputation (0–1000) |
| `facilityMultiplier` | 1.0 – 2.5 | Average upgrade level of VAB, R&D, Admin, Launch Pad |
| `penaltyMultiplier` | 0.0 – 1.0 | Applied when rep stays below threshold |

---

## Reputation Penalty System

If your reputation drops below `repPenaltyThreshold` (default: 200), the government
begins cutting your budget. The penalty escalates each consecutive year rep stays low:

| Consecutive low-rep years | Default effect |
|---|---|
| 1 | ×0.85 (–15%) |
| 2 | ×0.70 (–30%) |
| 3+ | **FROZEN** – ×0.00, no funds deposited |

- The streak resets the moment your rep climbs back above the threshold.
- `maxPenaltyMultiplier` sets the floor before freeze (default 0.60 = max –40% cut before freeze).
- `penaltyFreezeAfterYears` controls how many years of low rep trigger a full freeze (default 3).
- A red warning message is posted to the KSP message log when a freeze occurs.

---

## Toolbar Window

Click the BudgetMod button in the AppLauncher toolbar (top right, Space Center or Flight)
to open the projection window. It shows:

- **Time until next deposit** (days/hours/minutes in planet-appropriate units)
- **Full multiplier breakdown** with projected total
- **Penalty streak** and how many years until freeze
- **Facility level bars** for the four tracked KSC buildings

The window refreshes every 2 seconds and is draggable.

---

## RSS / Planet Pack Compatibility

Year length is derived from `homeWorld.orbitDriver.orbit.period` automatically:

| Install | Year length used |
|---|---|
| Stock KSP | ~9,203,545 s (~426 Kerbin days) |
| RSS | ~31,557,600 s (365.25 Earth days) |
| JNSQ / GPP / OPM | Home body actual orbital period |

Day length for the countdown display is also read from the home body, so "14d 06h" is
correct whether you're on Kerbin or Earth.

---

## Installation

1. Build (see below) to produce `KerbalFundingAgency.dll`.
2. Copy into your KSP `GameData` folder:

```
GameData/
  BudgetMod/
    KerbalFundingAgency.cfg
    Textures/
      icon.png          ← optional 38×38 toolbar icon (white on transparent)
    Plugins/
      KerbalFundingAgency.dll
```

If `Textures/icon.png` is missing, the button falls back to a white square.

---

## Building

### Requirements
- Visual Studio 2019+ or `dotnet` CLI with .NET Framework 4.5 targeting pack
- KSP installed (used for assembly references)

### Steps

1. Set `<KSPRoot>` in `KerbalFundingAgency.csproj` to your KSP folder, or set the
   `KSP_ROOT` environment variable.

2. Build:
   ```
   dotnet build KerbalFundingAgency.csproj -c Release
   ```

3. The `CopyToGameData` MSBuild target copies the DLL into
   `GameData/KerbalFundingAgency/Plugins/` automatically after a successful build.

---

## Configuration Reference

```
KFA_SETTINGS
{
    // Economy
    baseAllocation         = 150000   // Funds/year before multipliers
    minRepMultiplier       = 0.5      // Multiplier at rep = 0
    maxRepMultiplier       = 2.0      // Multiplier at rep = 1000
    facilityMultiplierBase = 1.0      // Multiplier with all buildings level 0
    facilityMultiplierMax  = 2.5      // Multiplier with all buildings fully upgraded

    // Penalty system
    repPenaltyThreshold    = 200      // Rep below this triggers penalties
    penaltyPerYear         = 0.15     // Cut per consecutive penalty year
    maxPenaltyMultiplier   = 0.60     // Floor before freeze kicks in
    penaltyFreezeAfterYears = 3       // Consecutive years until full freeze

    // UI
    showBreakdown          = true     // Yearly message log entry
}
```

### Suggested presets

| Mode | baseAllocation | Notes |
|---|---|---|
| Easy stock | 300,000 | Generous; contracts are pure bonus |
| Normal stock | 150,000 | Balanced with stock contract payouts |
| Hard stock | 75,000 | Forces careful prioritisation |
| RP-1 / RSS | 1,500,000 | RP-1 hardware costs millions |

---

## Extending

To track more KSC buildings in the facility multiplier, edit the `facilities` array
in `CalculateFacilityMultiplier()` in `BudgetMod.cs`:

```csharp
SpaceCenterFacility[] facilities =
{
    SpaceCenterFacility.VehicleAssemblyBuilding,
    SpaceCenterFacility.ResearchAndDevelopment,
    SpaceCenterFacility.Administration,
    SpaceCenterFacility.LaunchPad,
    SpaceCenterFacility.TrackingStation,    // add here
    SpaceCenterFacility.AstronautComplex,
};
```
