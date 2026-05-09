# Technical Architecture

## Status

Planned for v0.2

## Goal

Create a clean simulation layer for one standalone respiratory disease without expanding the old residue system.

## Proposed folders

```text
Source/
├─ Defs/
├─ Settings/
├─ Simulation/
├─ Utility/
├─ Components/
├─ UI/
└─ Debug/
```

## Main components

### Map component

Responsible for ticking the simulation and storing map-level state.

Suggested file:

```text
Source/Components/PathogenicDiseaseMapComponent.cs
```

Responsibilities:

- track pawn disease states
- process exposure decay
- process incubation progression
- process respiratory transmission
- detect outsider importation
- expose debug data
- save/load simulation state

### Disease state tracker

Suggested file:

```text
Source/Simulation/DiseaseStateTracker.cs
```

Responsibilities:

- store per-pawn disease state
- add exposure
- transition to incubation
- transition to symptoms
- clear or recover states

### Respiratory transmission worker

Suggested file:

```text
Source/Simulation/RespiratoryTransmissionWorker.cs
```

Responsibilities:

- find infectious pawns
- find nearby exposed pawns
- calculate distance and room factors
- apply exposure

### Disease importation worker

Suggested file:

```text
Source/Simulation/DiseaseImportationWorker.cs
```

Responsibilities:

- detect newly spawned outsider pawns
- roll import chance
- seed hidden disease state

### Debug actions

Suggested file:

```text
Source/Debug/PathogenicDiseaseDebugActions.cs
```

Responsibilities:

- apply disease state
- add exposure
- clear disease state
- print selected pawn state

## Design constraint

Do not hardwire the old floor residue mechanic into the new disease simulation.

The simulation should be able to run without spawning any residue `Thing`.
