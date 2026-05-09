# Feature: Exposure Accumulation

## Status

Planned

## Goal

Model infection risk through accumulated exposure instead of single random infection rolls.

## Player-facing behavior

Brief contact is less dangerous than repeated or prolonged contact.

A pawn becomes infected only after enough exposure has accumulated.

## Technical approach

Each pawn has an exposure value for the standalone respiratory disease.

Example:

```text
exposure: 0.00 to 1.00
threshold: 1.00
```

Exposure increases from transmission events and decays over time.

## Scope

Included:

- exposure amount
- exposure threshold
- exposure decay
- transition from exposed to incubating
- debug action to add exposure
- debug action to clear exposure

Excluded:

- surface exposure
- object contamination
- disease-specific exposure profiles

## Implementation notes

Exposure should decay slowly so that repeated contact matters.

Example values for testing:

```text
threshold: 1.0
decay per day: 0.15
```

## Acceptance criteria

- [ ] pawn can receive exposure
- [ ] exposure decays over time
- [ ] exposure crossing threshold starts incubation
- [ ] debug action can add exposure
- [ ] debug action can clear exposure
- [ ] debug readout shows current exposure
