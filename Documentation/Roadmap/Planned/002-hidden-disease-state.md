# Feature: Hidden Disease State

## Status

Planned

## Goal

Track infection state before the visible disease appears.

## Player-facing behavior

A pawn may be infected or incubating without the player immediately seeing a health condition.

Symptoms appear later.

## Technical approach

Create a hidden simulation state stored by the map component.

Suggested enum:

```csharp
public enum SimulatedDiseaseStage
{
    None,
    Exposed,
    Incubating,
    PreSymptomaticInfectious,
    Symptomatic,
    Recovering,
    Recovered
}
```

Suggested files:

```text
Source/Simulation/SimulatedDiseaseStage.cs
Source/Simulation/PawnDiseaseState.cs
Source/Simulation/DiseaseStateTracker.cs
```

## Scope

Included:

- hidden state per pawn
- current disease stage
- key transition ticks
- debug readout

Excluded:

- multiple disease support
- immunity history across maps
- world pawn tracking

## Implementation notes

For v0.2, keep the system specific to the standalone respiratory disease.

Do not over-generalize too early.

## Acceptance criteria

- [ ] selected pawn can receive a hidden disease state
- [ ] hidden state persists through save/load
- [ ] hidden state can be cleared
- [ ] debug readout shows current stage
- [ ] hidden state is separate from visible `HediffDef`
