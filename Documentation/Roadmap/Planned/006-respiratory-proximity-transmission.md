# Feature: Respiratory Proximity Transmission

## Status

In Progress (Feature 5 branch)

## Goal

Allow infectious pawns to increase exposure in nearby pawns through shared air.

## Player-facing behavior

Pawns who spend time near infected pawns, especially indoors, are more likely to become infected.

Isolation and distance reduce risk.

## Technical approach

Run a periodic worker from the map component.

Suggested interval:

```text
250 ticks
```

For each infectious pawn:

1. find nearby humanlike pawns
2. skip invalid targets
3. check room/outdoor conditions
4. calculate exposure
5. add exposure to target

Suggested files:

```text
Source/Simulation/RespiratoryTransmissionWorker.cs
Source/Utility/RoomExposureUtility.cs
Source/Utility/PawnProximityUtility.cs
```

## Exposure formula

Initial formula:

```text
exposure =
    baseExposure
    × sourceInfectiousness
    × distanceFactor
    × roomFactor
```

Suggested first values:

```text
same room indoors: 1.0
outdoors: 0.25
different rooms: 0.0
```

## Scope

Included:

- radius-based proximity exposure
- distance falloff
- same-room requirement
- reduced outdoor exposure
- exposure added to target pawn

Excluded:

- room airborne load
- ventilation
- masks
- surface contamination
- object residue
- exact social interaction hooks

## Implementation notes

This should be the core v0.2 transmission mechanic.

Do not create visible zone objects. The zone can be calculated invisibly.

## Acceptance criteria

- [x] infectious pawn exposes nearby valid pawns
- [x] different rooms block exposure
- [x] outdoor exposure is reduced
- [x] distance reduces exposure
- [x] exposure can lead to incubation
- [x] debug logging can show exposure events
