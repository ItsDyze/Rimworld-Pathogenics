# Dyze's Pathogenics

A RimWorld mod focused on disease simulation instead of random one-off illness events.

## Current slice

The v0.3.x series adds complete disease simulation with outsider importation, player feedback, and configurable balancing.

### Included features:
- Hidden disease state before symptoms appear
- Visible standalone disease for testing
- **v0.2.1**: Exposure accumulation and decay system
- **v0.2.2**: Respiratory proximity transmission with room-based factors
- **v0.2.2**: Debug tools for fast transmission testing (10x multiplier, pulse action)
- **v0.3.0**: Outsider importation (disease enters via visitors, traders, raiders, etc.)
- **v0.3.0**: Player feedback - symptom onset notifications with transmission warnings
- **v0.3.0**: Debug readout overlay showing all pawns with disease state
- **v0.3.0**: Expanded settings for balancing (import chance, exposure multiplier)

### Not included yet:
- Quarantine tools
- Additional disease types
- Richer disease progression variants
- World map spread

## Requirements

- RimWorld 1.6
- Harmony

## Development setup

The project expects RimWorld managed assemblies at build time.

You can build by setting one of these MSBuild properties:
- `RimWorldInstallDir` → root RimWorld install directory
- `RimWorldManagedDir` → direct path to the game's `Managed` folder

Examples:
- Windows: `dotnet build /p:RimWorldInstallDir="D:\\SteamLibrary\\steamapps\\common\\RimWorld"`
- Linux: `dotnet build /p:RimWorldInstallDir="$HOME/.steam/steam/steamapps/common/RimWorld"`

If neither property is set, the project tries a few common install paths first and then fails with a clear error.

## Notes

This mod is still in active development.
The current goal is to make one disease simulation loop work cleanly before expanding into balance, feedback, and additional features.

## v0.3.0 Changes

### Outsider Importation
- Disease can now enter the colony through outsiders (visitors, traders, raiders, refugees, prisoners, quest pawns)
- Configurable import chance (default 15%)
- Imported outsider states can start as incubating, pre-symptomatic infectious, or symptomatic
- Can be enabled/disabled in settings

### Player Feedback
- Colonist symptom onset creates a letter notification
- Optional transmission warning in the notification
- On-screen debug readout (toggleable in settings)

### Settings & Balancing
- New settings UI with clear sections
- Outsider importation can be toggled on/off
- Respiratory spread can be toggled on/off
- Import chance slider (1% - 50%)
- Exposure gain multiplier slider (0.1x - 5.0x, default 3.0x)
- Debug readout visibility toggle

### Persistence / Travel Notes
- Hidden disease state is preserved across save/load
- Colonist disease state is preserved across off-map travel and caravan transitions
- Debug readout currently focuses on the active map view; off-map continuity is enforced at the simulation/state level
