# Dyze's Pathogenics

A RimWorld mod focused on disease simulation instead of random one-off illness events.

## Current slice

The v0.2.x series adds the foundation for a standalone respiratory disease with proximity transmission.

### Included features:
- Hidden disease state before symptoms appear
- Visible standalone disease for testing
- Debug actions to expose pawns, inspect/clear state, force symptom onset
- **v0.2.2**: Respiratory proximity transmission with room-based factors
- **v0.2.2**: Exposure accumulation and decay system
- **v0.2.2**: Debug tools for fast transmission testing (10x multiplier, pulse action)

### Not included yet:
- Quarantine tools
- Additional disease types
- Player feedback / UI notifications
- Richer disease progression variants

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
