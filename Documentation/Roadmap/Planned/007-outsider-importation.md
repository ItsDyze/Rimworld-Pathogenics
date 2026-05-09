# Feature: Outsider Importation

## Status

Planned

## Goal

Make the disease enter the colony through outsiders instead of random colony infection.

## Player-facing behavior

Visitors, traders, raiders, refugees, prisoners, or quest pawns may bring the disease into the map.

They may not show symptoms immediately.

## Technical approach

Use the map component to detect newly spawned outsider pawns.

Do not hook every arrival event at first.

Suggested logic:

```text
for each spawned humanlike pawn:
    if pawn has not been checked:
        mark as checked
        if pawn is outsider:
            roll import chance
            if successful:
                assign hidden disease state
```

Suggested file:

```text
Source/Simulation/DiseaseImportationWorker.cs
```

## Imported states

Suggested first distribution:

```text
70% incubating
25% pre-symptomatic infectious
5% symptomatic
```

## Scope

Included:

- detect newly spawned humanlike pawns
- identify non-colony outsiders
- roll import chance
- seed disease state
- debug logging

Excluded:

- faction-level epidemic simulation
- world map spread
- storyteller incident replacement
- vanilla disease incident suppression

## Implementation notes

This feature should come after hidden state, incubation, and respiratory transmission.

## Acceptance criteria

- [ ] new outsider pawns can be detected
- [ ] same pawn is not checked repeatedly
- [ ] import chance can be configured
- [ ] imported disease state is assigned correctly
- [ ] imported pawn can spread disease if infectious
