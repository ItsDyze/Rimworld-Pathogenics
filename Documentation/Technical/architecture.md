# Technical Architecture

## Status

Implemented for v0.2

## Goal

Create a clean simulation layer for one standalone respiratory disease without expanding the old residue system.

## Actual implementation

### Folder structure

```text
Sources/DyzePathogenics/
├─ Runtime/
│   ├─ PathogenicsMapComponent.cs
│   └─ PawnDiseaseState.cs
├─ Simulation/
│   ├─ InfectiousnessUtility.cs
│   └─ RespiratoryTransmissionWorker.cs
├─ Settings/
│   └─ DyzePathogenicsSettings.cs
├─ Debug/
│   └─ DyzeDebugAction.cs
└─ (other supporting files)
```

### Implemented components

#### Map component

File: `Sources/DyzePathogenics/Runtime/PathogenicsMapComponent.cs`

Responsibilities:

- store pawn disease states in `pawnDiseaseStates` dictionary
- provide get/create methods for disease state
- handle save/load via Scribe
- run respiratory transmission worker periodically

#### Disease state

File: `Sources/DyzePathogenics/Runtime/PawnDiseaseState.cs`

Contains:

- `SimulatedDiseaseStage` enum with stages: None, Exposed, Incubating, PreSymptomaticInfectious, Symptomatic, Recovering, Recovered
- `PawnDiseaseState` class tracking: Stage, ExposedTick, InfectiousStartTick, SymptomOnsetTick, RecoveringTick, RecoveredTick
- Implements `IExposable` for save/load

#### Debug actions

File: `Sources/DyzePathogenics/Debug/DyzeDebugAction.cs`

Actions:

- "Expose selected pawn (hidden infection)" - apply hidden disease state
- "Apply pathogenic flu to selected pawn" - apply visible HediffDef and sync hidden state
- "Remove pathogenic flu from selected pawn" - remove HediffDef and clear hidden state
- "Add exposure (0.25) to selected pawn" - manually add exposure for testing
- "Clear exposure for selected pawn" - clear exposure for one pawn
- "Clear hidden disease state for selected pawn" - clear only hidden state
- "Log disease state for selected pawn" - show current stage and timing
- "Toggle Fast Transmission (10x)" - debug multiplier for accelerated testing
- "Force transmission pulse" - force exposure to all nearby pawns
- "Log mod status" - show settings

### Key design: Separate visible and hidden state

The visible disease (`DP_PathogenicFlu` HediffDef) and the hidden disease state (`PawnDiseaseState`) are **separate but synchronized**.

- The HediffDef represents what shows in the health tab
- The PawnDiseaseState tracks pre-symptomatic progression
- Debug actions ensure both stay in sync
- This allows the disease to be "incubating" before it becomes visible

### Design constraint

The v0.2 simulation runs without any floor residue mechanic.

The old residue system is deprecated and retained only for legacy save cleanup.
