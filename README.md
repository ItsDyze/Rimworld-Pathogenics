# Dyze's Pathogenics

A RimWorld mod that replaces random illness events with a realistic disease simulation loop.

## What It Is

Pathogenics implements a standalone respiratory disease that enters your colony through outsiders, spreads via proximity exposure, and progresses through distinct stages before becoming visible to players.

Unlike vanilla's random one-off illness events, this mod models disease as a propagating system: infected colonists can spread illness before showing symptoms, giving quarantine and distance meaningful gameplay value.

## Current Status

**v1.0-rc** — Release candidate. The mod is functionally ready for release; the only missing public-release piece is the final illustration from the artist before it goes live.

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
| Global disease registry for cross-map/caravan state continuity | v1.0-rc |
| Persistent outsider import cache across save/load | v1.0-rc |
| Pause-safe master toggle and reset tooling | v1.0-rc |
| Coronavirus as the default custom Pathogenics disease | v1.0-rc |
| Deprecated `DP_PathogenicFlu` compatibility def retained for old saves | v1.0-rc |
| Vanilla flu integration with optional vanilla disease incident suppression | v1.0-rc |

### Integrated Vanilla Diseases

Pathogenics now has a small disease integration registry. The default custom Pathogenics disease is **coronavirus** (`DP_Coronavirus`). The old `DP_PathogenicFlu` def is deprecated, non-scenario-addable, and retained only so existing saves can load safely.

The first integrated vanilla disease is **flu** (`Flu`), because it fits the current shared-air respiratory model.

Integrated flu can enter through the existing outsider importation flow, spread through the same hidden exposure/incubation system, and appear as the vanilla flu hediff when symptoms start.

The settings window includes disease event controls:

- **Disable all vanilla disease incidents**: blocks vanilla random disease incidents entirely.
- **Disable integrated vanilla disease incidents**: blocks only vanilla incidents for Pathogenics-integrated diseases, currently flu. Non-integrated diseases such as malaria remain untouched.

Malaria and other vector/environment-bound illnesses are intentionally not integrated until Pathogenics has matching transmission routes.

### Not Yet Implemented

- Quarantine management tools
- Additional disease types
- Richer disease progression variants
- World map spread

## Disease Loop

```
Outsider arrives infected or incubating
        ↓
Global registry tracks hidden disease state across maps/caravans
        ↓
Pawn may become infectious before symptoms appear
        ↓
Nearby pawns accumulate exposure through shared air
        ↓
Exposure threshold triggers incubation
        ↓
Symptoms appear as visible disease
        ↓
Hidden state owns symptom end / recovery cleanup
        ↓
Isolation and distance reduce spread
```

## Release Reliability Notes

This release candidate hardens the simulation for public release:

- **Cross-map continuity:** disease state is now owned by a `GameComponent` registry instead of only a map component, so lookups continue to work when pawns move between maps or travel off-map.
- **Save/load stability:** outsider import checks are persisted, removing reload-dependent re-rolls for already-seen outsiders.
- **No hidden/visible drift:** the hidden disease state now owns visible disease application and removal, so symptom timing and hediff lifetime stay aligned.
- **Pause-safe master toggle:** disabling the mod now pauses both hidden progression and visible Pathogenics severity progression. Re-enabling resumes from the preserved state.
- **Recovery/reset path:** debug actions now include registry health logging, cross-map state logging, and a worldwide Pathogenics reset to recover seamlessly from broken prototype-era states if needed.

## Save Compatibility

Existing saves are intended to upgrade forward.

> That's mostly vibe coded.

- Older map-owned `pawnDiseaseStates` are imported into the new global registry on load.
- If a prototype save already contains inconsistent visible/hidden Pathogenics state, the new resync logic repairs many common cases automatically.
- If a save is still in a broken prototype state, use the debug action **"Reset all Pathogenics state worldwide"** to clear hidden states, visible Pathogenics hediffs, and outsider import cache cleanly across the whole save.

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