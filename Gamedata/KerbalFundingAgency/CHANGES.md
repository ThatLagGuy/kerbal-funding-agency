# Changelog

All notable changes to Kerbal Funding Agency will be documented here.
Format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

---

## [0.3.2] – 2026-05-24

### Changed
- Settings are now saved to the career save file instead of KerbalFundingAgency.cfg.
  The cfg file is now a read-only defaults template — it is never overwritten by the mod.
  This means different career saves can have different KFA settings independently.
- "Save to cfg" button in the settings panel renamed to "Save to career".

### Fixed
- Settings now correctly persist across save reloads. Previously the cfg write
  approach was unreliable; settings now live in the save file alongside budget
  state and are loaded correctly every time the save is opened.

---

## [0.3.1] – 2026-05-24

### Fixed
- Save to cfg now correctly resolves the config file path on all systems. Previously
  the GameDatabase URL lookup produced a doubled folder path, causing the save to
  fail with a file-not-found error.
- Settings saved in-game now correctly persist across save reloads. Previously
  LoadConfig read from GameDatabase which is only populated at KSP startup, so
  any in-game changes were lost on reload. LoadConfig now reads directly from disk.

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
