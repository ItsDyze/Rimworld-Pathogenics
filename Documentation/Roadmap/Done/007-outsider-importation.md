# Feature: Outsider Importation

## Status

Done (v0.3.0), hardened for release in v0.3.2-dev

## Goal

Make the disease enter the colony through outsiders instead of random colony infection.

## Player-facing behavior

Visitors, traders, raiders, refugees, prisoners, or quest pawns may bring the disease into the map.

They may not show symptoms immediately.

## Technical approach

Outsider detection still runs from the map component tick, but the "already checked" memory is now owned by the global `PathogenicsGameComponent` so it survives save/load.

Current logic:

```text
for each spawned humanlike pawn:
    if pawn has not been checked in the global import cache:
        mark as checked
        if pawn is outsider:
            roll import chance
            if successful:
                assign hidden disease state in the global registry
```

Implemented file:

```text
Sources/DyzePathogenics/Simulation/DiseaseImportationWorker.cs
```

Registry file:

```text
Sources/DyzePathogenics/Runtime/PathogenicsGameComponent.cs
```

## Imported states

Current distribution:

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
- persist outsider check state through save/load
- debug logging

Excluded:

- faction-level epidemic simulation
- world map spread
- storyteller incident replacement
- vanilla disease incident suppression

## Release-hardening notes

The original implementation used a static runtime cache, which made outsider importation slightly reload-dependent. That is no longer true.

The checked-pawn cache is now serialized in the save, so reloading does not re-roll already-seen outsiders just because the process restarted.

## Acceptance criteria

- [x] new outsider pawns can be detected
- [x] same pawn is not checked repeatedly during a play session
- [x] same pawn is not re-checked just because the game was reloaded
- [x] import chance can be configured
- [x] imported disease state is assigned correctly
- [x] imported pawn can spread disease if infectious
