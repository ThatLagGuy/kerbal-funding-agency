# Kerbal Funding Agency (KFA)

A career mode mod for Kerbal Space Program that adds a realistic government budget system to your space agency. Every in-game year, your agency receives an annual funding allocation from the government — scaled by your reputation and the quality of your KSC facilities. Keep your reputation high, invest in your space center, and the money follows. Let things slip and face the consequences.

---

## Features

### Annual Budget Allocation
Every year your agency receives a government budget deposit calculated as:

> `budget = base allocation × reputation multiplier × facility multiplier × penalty multiplier`

A detailed breakdown is posted to your message log each year showing exactly how the figure was reached.

### Reputation Scaling
Your reputation directly influences your funding. An agency with no reputation receives half the base allocation. A legendary agency with maxed reputation receives double. Everything scales linearly in between.

### Facility Multiplier
The government rewards investment in your space center. Upgrading your VAB, R&D facility, Administration building, and Launch Pad all contribute to a higher facility multiplier — up to 2.5× with a fully upgraded KSC.

### Reputation Penalty System
Sustained poor reputation has real consequences. If your reputation drops below the configurable threshold:

- **Year 1** below threshold → 15% budget cut
- **Year 2** below threshold → 30% budget cut
- **Year 3+** below threshold → funding frozen entirely for that year

Recover your reputation above the threshold at any point to reset the streak.

### In-Game Settings Panel
A toolbar button in the AppLauncher (Space Center and Flight scenes) opens a window with two tabs:

- **Overview** — countdown to next deposit, full multiplier breakdown with projected total, reputation status and penalty streak, facility level indicators
- **Settings** — sliders for every tunable value with live preview, applies changes immediately, with a Save button that writes back to the config file

### RSS / Planet Pack Compatible
Year length is read directly from the home body's orbital period, so KFA automatically uses the correct year length for any planet pack — no configuration needed.

| Install | Year length |
|---|---|
| Stock KSP | ~426 Kerbin days |
| RSS | 365.25 Earth days |
| JNSQ, GPP, OPM | Home body orbital period |

---

## Installation

1. Download the latest release zip from [SpaceDock](#) or [GitHub Releases](#)
2. Extract into your `GameData` folder:

```
GameData/
  KerbalFundingAgency/
    KerbalFundingAgency.cfg
    Textures/
      icon.png
    Plugins/
      KerbalFundingAgency.dll
```

3. Launch KSP and load a career save

---

## Configuration

All values can be adjusted in `GameData/KerbalFundingAgency/KerbalFundingAgency.cfg` or via the in-game Settings tab.

| Setting | Default | Description |
|---|---|---|
| `baseAllocation` | 150,000 | Funds deposited per year before multipliers |
| `minRepMultiplier` | 0.5 | Budget multiplier at reputation = 0 |
| `maxRepMultiplier` | 2.0 | Budget multiplier at reputation = 1000 |
| `facilityMultiplierBase` | 1.0 | Multiplier with all KSC buildings at level 0 |
| `facilityMultiplierMax` | 2.5 | Multiplier with all KSC buildings fully upgraded |
| `repPenaltyThreshold` | 200 | Reputation below this triggers budget penalties |
| `penaltyPerYear` | 0.15 | Fractional budget cut per consecutive penalty year |
| `penaltyFreezeAfterYears` | 3 | Consecutive penalty years before full funding freeze |
| `showBreakdown` | true | Show annual budget breakdown in the message log |

### Suggested base allocation presets

| Career difficulty | Base allocation |
|---|---|
| Easy | 300,000 |
| Normal | 150,000 |
| Hard | 75,000 |
| RP-1 / RSS | 1,500,000 |

---

## Compatibility

- Career mode only — does nothing in Sandbox or Science mode
- KSP 1.12.x
- Compatible with RSS, JNSQ, GPP, OPM and any other planet pack
- Compatible with Contract Configurator, Strategia, and RP-1

---

## License

MIT — see [LICENSE](LICENSE) for details.
