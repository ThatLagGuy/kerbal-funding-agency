# Changelog

All notable changes to BudgetMod will be documented here.
Format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

---

## [0.3.0] – 2026-05-24

### Added
- In-game settings panel accessible via the toolbar window
  - Sliders for all multipliers and penalty values
  - Text field for base allocation
  - Toggle for yearly breakdown message
  - "Save to cfg" button writes changes to KerbalFundingAgency.cfg on disk
  - "Reset defaults" button
  - Changes apply immediately in-game without requiring a restart
- Two-tab layout in toolbar window: Overview and Settings

### Changed
- Toolbar window is now wider (360px) to accommodate settings controls

---

## [0.2.0] – 2026-05-24

### Added
- Reputation penalty system
  - Budget cut escalates each consecutive year reputation stays below threshold
  - Configurable cut per year, floor multiplier, and freeze threshold
  - Full funding freeze after a configurable number of consecutive low-rep years
  - Freeze warning posted to KSP message log with red formatting
  - Penalty streak saved and loaded with the career save
- Overview toolbar window
  - Countdown to next deposit (days/hours/minutes in home body units)
  - Full multiplier breakdown with projected total
  - Reputation status and penalty streak display
  - Facility level bars for tracked KSC buildings
  - AppLauncher toolbar button (supports custom 38×38 icon)

### Changed
- Annual message log entry now includes penalty modifier line when applicable
- Message colour changes to red when funding is frozen

---

## [0.1.0] – 2026-05-24

### Added
- Annual government budget allocation for KSP career mode
- Base allocation configurable via KerbalFundingAgency.cfg
- Reputation multiplier: scales linearly from 0.5× (rep 0) to 2.0× (rep 1000)
- Facility multiplier: based on average upgrade level of VAB, R&D, Administration, and Launch Pad
- Automatic year length detection from home body orbital period
  - Compatible with stock KSP, RSS, JNSQ, GPP, and any planet pack
- Budget deposit persists correctly across saves and time warp
- Yearly breakdown message posted to KSP message log
- All values configurable via KFA_SETTINGS node in KerbalFundingAgency.cfg
