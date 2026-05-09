# Feature: Infectiousness Stages

## Status

Planned

## Goal

Make disease spread depend on infection stage.

## Player-facing behavior

A pawn can spread disease before symptoms, spread more while visibly sick, and become less dangerous while recovering.

## Technical approach

Add a utility method that returns an infectiousness multiplier for a pawn.

Example:

```text
Exposed: 0.00
Incubating: 0.00
PreSymptomaticInfectious: 0.50
Symptomatic: 1.00
Recovering: 0.25
Recovered: 0.00
```

Suggested file:

```text
Source/Simulation/InfectiousnessUtility.cs
```

## Scope

Included:

- stage-based infectiousness
- pre-symptomatic infectious period
- reduced recovering infectiousness

Excluded:

- symptom-specific infectiousness
- severity-based coughing
- immunity modifiers
- treatment modifiers

## Implementation notes

This is required before respiratory transmission can work properly.

The simulation should never treat all infected pawns as equally contagious.

## Acceptance criteria

- [ ] infectiousness is zero before infectious start
- [ ] pre-symptomatic pawns can be infectious
- [ ] symptomatic pawns are highly infectious
- [ ] recovering pawns have reduced infectiousness
- [ ] recovered pawns are not infectious
