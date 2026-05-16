# Feature: Hidden Disease State

## Status

Done, hardened for release in v0.3.2-dev

## Goal

Track infection state before the visible disease appears.

## Player-facing behavior

A pawn may be infected or incubating without the player immediately seeing a health condition.

Symptoms appear later.

## Technical approach

Hidden simulation state is now owned by the game-wide registry (`PathogenicsGameComponent`) instead of only by a map component.

Implemented enum:

```csharp
public enum SimulatedDiseaseStage
{
    None = 0,
    Exposed = 1,
    Incubating = 2,
    PreSymptomaticInfectious = 3,
    Symptomatic = 4,
    Recovering = 5,
    Recovered = 6
}
```

Implemented files:

```text
Sources/DyzePathogenics/Runtime/PawnDiseaseState.cs
Sources/DyzePathogenics/Runtime/PathogenicsGameComponent.cs
Sources/DyzePathogenics/Runtime/PathogenicsMapComponent.cs
```

The `PawnDiseaseState` class tracks:

- Current stage
- Exposed tick
- Infectious start tick
- Symptom onset tick
- Recovering tick
- Recovered tick
- MapId
- PreserveAcrossMaps
- VisibleHediffApplied
- Exposure

## Scope

Included:

- hidden state per pawn
- current disease stage
- key transition ticks
- debug readout
- save/load persistence via Scribe
- cross-map/caravan-safe state ownership
- legacy migration from map-owned saves

Excluded:

- multiple disease support
- faction/world epidemic history
- automatic generalized disease framework

## Release-hardening notes

The original v0.2 approach stored state directly in `PathogenicsMapComponent`. That was fine for single-map prototyping, but fragile once pawns moved between maps or lived off-map in caravans.

The release-hardening pass changes ownership:

- `PathogenicsGameComponent` is now the source of truth
- `PathogenicsMapComponent` is now the map-local runner / compatibility bridge
- older map-owned `pawnDiseaseStates` are imported into the new registry on load

## Implementation detail: Visible and hidden state are separate, but no longer independent

The visible disease (`DP_PathogenicFlu` HediffDef) and the hidden disease state (`PawnDiseaseState`) are still separate systems conceptually:

- **Visible:** standard RimWorld `HediffDef` shown in the health tab
- **Hidden:** tracks progression before and after visible symptoms

But release hardening changed ownership of transitions:

- hidden state now owns symptom onset, visible hediff application, recovery transition, and visible hediff removal
- the visible hediff no longer removes itself independently from the hidden schedule
- periodic resync logic repairs common mismatches on load/tick

That keeps the design goal of pre-symptomatic infection while removing the earlier drift risk between the two clocks.

## Enabled-toggle behavior

`Settings.Enabled` now pauses both:

- hidden disease progression
- visible Pathogenics severity progression

Re-enabling resumes the same state. Disabling does not silently clear infection.

If a prototype-era save is already inconsistent, use the debug action **Reset Pathogenics state on current map** for a clean reset.

## Acceptance criteria

- [x] selected pawn can receive a hidden disease state
- [x] hidden state persists through save/load
- [x] hidden state survives map/caravan transitions through global ownership
- [x] hidden state can be cleared
- [x] debug readout shows current stage
- [x] hidden state is separate from visible `HediffDef`
- [x] visible and hidden disease timing are synchronized by one owner
