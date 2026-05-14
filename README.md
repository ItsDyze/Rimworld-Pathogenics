# Dyze's Pathogenics

A RimWorld mod that replaces random illness events with a realistic disease simulation loop.

## What It Is

Pathogenics implements a standalone respiratory disease that enters your colony through outsiders, spreads via proximity exposure, and progresses through distinct stages before becoming visible to players.

Unlike vanilla's random one-off illness events, this mod models disease as a propagating system: infected colonists can spread illness before showing symptoms, giving quarantine and distance meaningful gameplay value.

## Current Status

**v0.3.1** — Active development. The core disease loop is functional. Focus is on stabilizing the simulation, adding player feedback tools, and expanding configuration options.

### Implemented Features

| Feature | Version |
|---------|---------|
| Hidden disease state (incubation before symptoms) | v0.2 |
| Exposure accumulation and decay | v0.2.1 |
| Respiratory proximity transmission | v0.2.2 |
| Debug tools (transmission multiplier, pulse action) | v0.2.2 |
| Outsider importation (visitors, traders, raiders, refugees, prisoners, quest pawns) | v0.3 |
| Symptom onset notifications | v0.3 |
| Debug readout overlay | v0.3 |
| Configurable balancing (import chance, exposure multiplier) | v0.3 |
| Mask protection (face-covering apparel blocks transmission) | v0.3.1 |

### Not Yet Implemented

- Quarantine management tools
- Additional disease types
- Richer disease progression variants
- World map spread

## Disease Loop

```
Outsider arrives infected or incubating
        ↓
Pawn may become infectious before symptoms appear
        ↓
Nearby pawns accumulate exposure through shared air
        ↓
Exposure threshold triggers incubation
        ↓
Symptoms appear as visible disease
        ↓
Isolation and distance reduce spread
```

## Requirements

- RimWorld 1.6
- Harmony (brrainz.harmony)

## Building from Source

The project requires RimWorld managed assemblies at build time. Set one of these MSBuild properties:

- `RimWorldInstallDir` — Root RimWorld install directory
- `RimWorldManagedDir` — Direct path to `Managed` folder

**Windows:**
```powershell
dotnet build /p:RimWorldInstallDir="D:\SteamLibrary\steamapps\common\RimWorld"
```

**Linux:**
```bash
dotnet build /p:RimWorldInstallDir="$HOME/.steam/steam/steamapps/common/RimWorld"
```

If neither property is set, the build attempts common install paths before failing with a clear error.

## Design Philosophy

- **One complete loop first.** The mod focuses on making a single respiratory disease work cleanly before adding variety.
- **Transparency where useful.** Debug tools and overlays help players understand what's happening.
- **Configurable.** Settings let players tune import frequency, transmission rates, and toggle features.
- **Honest scope.** "Not included yet" features are documented. This is not a promise.

## Documentation

Design decisions and technical details are in `Documentation/`:

- `Design/` — Rationale for major features
- `Technical/` — Architecture, save data, performance notes
- `Roadmap/` — Implemented and planned features