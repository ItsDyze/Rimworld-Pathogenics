# Technical Architecture

## Status

Implemented for v0.3.2-dev release hardening

## Goal

Create a clean simulation layer for one standalone respiratory disease without expanding the old residue system, while making state ownership reliable across save/load, caravans, temporary maps, and multi-map colonies.

## Actual implementation

### Folder structure

```text
Sources/DyzePathogenics/
├─ Runtime/
│   ├─ PathogenicsGameComponent.cs
│   ├─ PathogenicsMapComponent.cs
│   ├─ PathogenicsPawnLookup.cs
│   └─ PawnDiseaseState.cs
├─ Simulation/
│   ├─ DiseaseImportationWorker.cs
│   ├─ InfectiousnessUtility.cs
│   └─ RespiratoryTransmissionWorker.cs
├─ HediffComps/
│   └─ HediffComp_SeverityPerDay.cs
├─ Debug/
│   └─ DyzeDebugAction.cs
└─ (other supporting files)
```

## Implemented components

### Global registry

File: `Sources/DyzePathogenics/Runtime/PathogenicsGameComponent.cs`

Responsibilities:

- own the authoritative `PawnDiseaseState` registry for the whole save
- persist outsider import checks across save/load
- provide global lookup and creation for disease state
- import legacy map-owned state from older saves

This is the main release-hardening change. Hidden disease state no longer depends on whichever map a pawn happens to be on when queried.

### Map component

File: `Sources/DyzePathogenics/Runtime/PathogenicsMapComponent.cs`

Responsibilities:

- tick map-local simulation work
- bridge map-local pawn iteration to the global registry
- resynchronize visible `DP_PathogenicFlu` with hidden state
- import legacy map-owned state during load
- provide local debug/reset helpers

The map component is now a runner and compatibility layer, not the source of truth.

### Disease state

File: `Sources/DyzePathogenics/Runtime/PawnDiseaseState.cs`

Tracks:

- `Stage`
- `ExposedTick`
- `InfectiousStartTick`
- `SymptomOnsetTick`
- `RecoveringTick`
- `RecoveredTick`
- `MapId`
- `PreserveAcrossMaps`
- `VisibleHediffApplied`
- `Exposure`

It still implements `IExposable` for save/load.

### Pawn lookup helper

File: `Sources/DyzePathogenics/Runtime/PathogenicsPawnLookup.cs`

Responsibilities:

- find tracked pawns across spawned maps, caravans/travel lists, and available world-pawn surfaces
- provide location descriptions for debug output without binding the simulation to a fragile single API call

### Transmission / importation workers

Files:

- `Sources/DyzePathogenics/Simulation/RespiratoryTransmissionWorker.cs`
- `Sources/DyzePathogenics/Simulation/DiseaseImportationWorker.cs`
- `Sources/DyzePathogenics/Simulation/InfectiousnessUtility.cs`

Release-hardening changes:

- infectiousness now reads from the global registry instead of `pawn.Map`
- outsider importation uses a persisted checked-pawn cache instead of a static runtime-only cache
- target-state checks use the global registry so stage decisions remain stable after map transitions

### Visible disease lifecycle

Files:

- `Sources/DyzePathogenics/Runtime/PathogenicsMapComponent.cs`
- `Sources/DyzePathogenics/HediffComps/HediffComp_SeverityPerDay.cs`

Key change:

- the hidden disease state owns when the visible hediff appears and disappears
- the hediff comp no longer removes the hediff at max severity on its own
- while the master toggle is disabled, severity progression pauses instead of drifting ahead of hidden state

That removes the previous risk where the visible disease could self-remove while hidden state still considered the pawn symptomatic or recovering.

## Key design: Separate visible and hidden state, with one owner

The visible disease (`DP_PathogenicFlu` HediffDef) and the hidden disease state (`PawnDiseaseState`) are still separate concepts, but they no longer progress independently.

- **Visible:** what the player sees in the health tab
- **Hidden:** progression, infectiousness, and stage scheduling
- **Owner:** hidden state owns visible apply/remove transitions
- **Repair path:** resync logic repairs common mismatches on tick/load

This preserves the design goal of incubation before symptoms while tightening reliability for release.

## Enabled toggle semantics

`Settings.Enabled` now means:

- hidden simulation progression is paused
- visible Pathogenics severity progression is paused
- tracked state is preserved
- re-enabling resumes from the same state

It does **not** silently clear active disease. For admin recovery or prototype-save cleanup, use the debug reset action.

## Save and migration behavior

Forward migration behavior:

- older saves with `pawnDiseaseStates` stored under `PathogenicsMapComponent` are imported into the global registry during load
- outsider import checks are now serialized in the game component
- stale mismatches between visible hediffs and hidden state are repaired where possible during resync

Fallback path:

- if an older prototype save still contains an irrecoverably odd disease state, use **Reset all Pathogenics state worldwide** to clear hidden state, visible Pathogenics hediffs, and outsider import cache across the whole save

## Debug actions relevant to validation

File: `Sources/DyzePathogenics/Debug/DyzeDebugAction.cs`

Useful release-validation actions:

- `Expose selected pawn (hidden infection)`
- `Apply pathogenic flu to selected pawn`
- `Remove pathogenic flu from selected pawn`
- `Force transmission pulse`
- `Print simulation state`
- `Log registry health`
- `Log selected pawn cross-map state`
- `Reset all Pathogenics state worldwide`

These are the minimum practical tools for validating save/load, symptom onset, recovery, cross-map lookup, and reset behavior in-game.

## Design constraint

The active simulation still runs without any floor residue mechanic.

The old residue system remains deprecated and is retained only where needed for legacy cleanup/compatibility.
