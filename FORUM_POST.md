# Kerbal Funding Agency – Government Budget Allocation for Career Mode

**Version:** 0.3.0
**KSP Version:** 1.12.x
**License:** MIT
**Downloads:** [SpaceDock](#) | [GitHub](#)

---

## What is this?

BudgetMod adds a realistic government funding system to KSP career mode. Every
in-game year your space agency receives an annual budget allocation from "the
government", scaled by your reputation and the upgrade level of your KSC
facilities. Keep your reputation high and build up your space center to unlock
larger budgets. Let your reputation slip and face escalating budget cuts — or
even a full funding freeze.

---

## Features

**Annual budget allocation**
Every year (automatically adjusted for RSS, JNSQ, GPP, or any planet pack) a
lump sum is deposited into your agency funds. The amount is determined by:

> `budget = base allocation × reputation multiplier × facility multiplier × penalty multiplier`

**Reputation scaling**
Your reputation directly affects your funding. A brand-new agency with no rep
gets half the base allocation. A legendary agency with maxed reputation gets
double. Everything in between scales linearly.

**Facility multiplier**
The government rewards investment in your space center. Upgrading your VAB,
R&D facility, Administration building, and Launch Pad all contribute to a
higher facility multiplier, up to 2.5× with a fully upgraded KSC.

**Reputation penalty system**
Sustained poor reputation has consequences. If your rep drops below the
configurable threshold:
- Year 1 low rep → 15% budget cut
- Year 2 low rep → 30% budget cut
- Year 3+ low rep → **funding frozen entirely for that year**

Recover your reputation above the threshold at any point to reset the streak.

**In-game settings panel**
A toolbar button (AppLauncher, Space Center and Flight scenes) opens a window
with two tabs:

- **Overview** — countdown to next deposit, full multiplier breakdown, projected
  total, reputation status, facility level bars
- **Settings** — sliders for every tunable value, applies changes immediately
  in-game, with a "Save to cfg" button to persist across restarts

**RSS / planet pack compatible**
Year length is read directly from the home body's orbital period, so BudgetMod
automatically uses the correct year length for any planet pack — no
configuration needed.

---

## Installation

1. Download the zip from SpaceDock or GitHub Releases
2. Extract into your `GameData` folder — the result should look like:

```
GameData/
  BudgetMod/
    KerbalFundingAgency.cfg
    Textures/
      icon.png
    Plugins/
      KerbalFundingAgency.dll
```

3. Launch KSP and start or load a career save

---

## Configuration

All values are tunable in `GameData/KerbalFundingAgency/KerbalFundingAgency.cfg` or via the
in-game settings panel:

| Setting | Default | Description |
|---|---|---|
| `baseAllocation` | 150,000 | Funds per year before multipliers |
| `minRepMultiplier` | 0.5 | Multiplier at rep = 0 |
| `maxRepMultiplier` | 2.0 | Multiplier at rep = 1000 |
| `facilityMultiplierBase` | 1.0 | Multiplier with all buildings at level 0 |
| `facilityMultiplierMax` | 2.5 | Multiplier with all buildings fully upgraded |
| `repPenaltyThreshold` | 200 | Rep below this triggers penalties |
| `penaltyPerYear` | 0.15 | Budget cut per consecutive penalty year |
| `penaltyFreezeAfterYears` | 3 | Years of low rep before full freeze |

**Suggested presets:**

| Mode | Base allocation |
|---|---|
| Easy stock | 300,000 |
| Normal stock | 150,000 |
| Hard stock | 75,000 |
| RP-1 / RSS | 1,500,000 |

---

## Compatibility

- **Career mode only** — does nothing in Sandbox or Science mode
- **KSP 1.12.x** — tested on 1.12.5
- **RSS** — fully compatible, year length auto-detected
- **JNSQ, GPP, OPM** — compatible, year length auto-detected
- **Contract Configurator** — compatible
- **RP-1** — compatible, increase `baseAllocation` to ~1,500,000 to match RP-1's economy
- **Strategia** — compatible

---

## Planned features

- Budget allocation across departments (science, construction, astronaut corps)
- Emergency grants tied to high-prestige mission completions
- Random public opinion events that affect reputation
- ModuleManager patch with RP-1 preset values
- Low-reputation warning when approaching the penalty threshold

---

## Source & bug reports

Source code is on GitHub. Bug reports and feature requests welcome via GitHub
Issues. Pull requests accepted — see the README for build instructions.

**[GitHub Repository](#)**

---

## Changelog

See [CHANGES.md](#) on GitHub for the full version history.

---

## Credits

Built with the KSP modding API and Unity. Thanks to the KSP modding community
for documentation, especially the KSP wiki and the unofficial modding Discord.

---

*Licensed under the MIT License. See LICENSE for details.*
