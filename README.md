<p align="center">
  <img src="[https://github.com/ThatLagGuy/kerbal-funding-agency/tree/main/Textures/banner.png" alt="Kerbal Funding Agency](https://github.com/ThatLagGuy/kerbal-funding-agency/blob/main/Textures/banner.png)" width="800"/>
</p>

<p align="center">
  <a href="https://github.com/ThatLagGuy/kerbal-funding-agency/releases/latest">
    <img src="https://img.shields.io/github/v/release/ThatLagGuy/kerbal-funding-agency?style=for-the-badge&logo=github&label=Download&color=4c91e6" alt="Latest Release"/>
  </a>
  <a href="https://spacedock.info">
    <img src="https://img.shields.io/badge/SpaceDock-Download-orange?style=for-the-badge" alt="SpaceDock"/>
  </a>
  <img src="https://img.shields.io/badge/KSP-1.12.x-94c1ff?style=for-the-badge" alt="KSP Version"/>
  <img src="https://img.shields.io/badge/License-MIT-green?style=for-the-badge" alt="MIT License"/>
  <img src="https://img.shields.io/badge/RSS-Compatible-blue?style=for-the-badge" alt="RSS Compatible"/>
</p>

<p align="center">
  <i>Government funding for your space program — with all the bureaucracy that implies.</i>
</p>

---

## What is KFA?

**Kerbal Funding Agency** adds a realistic government budget system to KSP career mode. Every in-game year your agency receives an annual funding allocation — scaled by your reputation and the quality of your KSC facilities. Keep your reputation high, invest in your space center, and the funding follows. Let things slip and face escalating budget cuts, or worse, a full government funding freeze.

---

## Features

### 💰 Annual Budget Allocation
Every year the government deposits a budget into your agency funds, calculated as:

```
budget = base allocation × reputation multiplier × facility multiplier × penalty multiplier
```

A detailed breakdown is posted to your KSP message log each year.

### ⭐ Reputation Scaling
Your reputation directly influences your funding. A brand new agency scraping by at zero rep gets half the base allocation. A legendary agency with maxed reputation gets double. Everything scales linearly between the two.

### 🏗️ Facility Multiplier
The government rewards investment in your space center. Upgrading your **VAB**, **R&D facility**, **Administration building**, and **Launch Pad** all contribute to a higher budget — up to **2.5×** with a fully upgraded KSC.

### ⚠️ Reputation Penalty System
Sustained poor reputation has real consequences:

| Consecutive years below threshold | Effect |
|---|---|
| 1 year | 15% budget cut |
| 2 years | 30% budget cut |
| 3+ years | ❌ Funding frozen entirely |

Recover your reputation above the threshold at any point to reset the streak.

### 🖥️ In-Game Settings Panel
Click the KFA button in the AppLauncher toolbar to open the budget window:

- **Overview tab** — countdown to next deposit, projected budget with full multiplier breakdown, reputation status, penalty streak, and facility level indicators
- **Settings tab** — sliders for every tunable value, changes apply immediately in-game, with a Save button that writes back to the config file

### 🌍 RSS & Planet Pack Compatible
Year length is read directly from the home body's orbital period — no configuration needed.

| Installation | Year length used |
|---|---|
| Stock KSP | ~426 Kerbin days |
| RSS | 365.25 Earth days |
| JNSQ / GPP / OPM | Home body orbital period |

---

## Installation

> **Requires:** KSP 1.12.x

1. Download the latest release from [GitHub Releases](https://github.com/ThatLagGuy/kerbal-funding-agency/releases/latest) or [SpaceDock](#)
2. Extract the zip into your `GameData` folder:

```
GameData/
  KerbalFundingAgency/
    KerbalFundingAgency.cfg
    Textures/
      icon.png
    Plugins/
      KerbalFundingAgency.dll
```

3. Launch KSP and load a career save — that's it!

---

## Configuration

All values can be tuned in `KerbalFundingAgency.cfg` or directly via the **in-game Settings tab** without editing any files.

| Setting | Default | Description |
|---|---|---|
| `baseAllocation` | 150,000 | Funds deposited per year before multipliers |
| `minRepMultiplier` | 0.5 | Budget multiplier at reputation = 0 |
| `maxRepMultiplier` | 2.0 | Budget multiplier at reputation = 1000 |
| `facilityMultiplierBase` | 1.0 | Multiplier with all KSC buildings at level 0 |
| `facilityMultiplierMax` | 2.5 | Multiplier with all KSC buildings fully upgraded |
| `repPenaltyThreshold` | 200 | Reputation below this triggers budget penalties |
| `penaltyPerYear` | 0.15 | Budget cut per consecutive penalty year |
| `penaltyFreezeAfterYears` | 3 | Years of low rep before full funding freeze |
| `showBreakdown` | true | Show annual budget breakdown in the message log |

### Presets

| Difficulty | Suggested `baseAllocation` |
|---|---|
| Easy | 300,000 |
| Normal (default) | 150,000 |
| Hard | 75,000 |
| RP-1 / RSS | 1,500,000 |

---

## Compatibility

| Mod | Status |
|---|---|
| Stock KSP 1.12.x | ✅ Fully supported |
| RSS | ✅ Fully compatible |
| JNSQ / GPP / OPM | ✅ Fully compatible |
| Contract Configurator | ✅ Compatible |
| Strategia | ✅ Compatible |
| RP-1 | ✅ Compatible (increase base allocation) |
| Sandbox / Science mode | ➖ Inactive — career only |

---

## Changelog

See [CHANGES.md](CHANGES.md) for the full version history.

---

## License

Distributed under the **MIT License** — see [LICENSE](LICENSE) for details.

---

<p align="center">
  Made with 💚 for the KSP community &nbsp;•&nbsp;
  <a href="https://github.com/ThatLagGuy/kerbal-funding-agency/issues">Report a Bug</a> &nbsp;•&nbsp;
  <a href="https://github.com/ThatLagGuy/kerbal-funding-agency/issues">Request a Feature</a>
</p>
