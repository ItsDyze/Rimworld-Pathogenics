# Feature: Settings and Balancing

## Status

Done (v0.3.0)

## Goal

Expose core simulation values through settings without overwhelming the player.

## Player-facing behavior

Players can enable or disable the disease simulation and tune broad difficulty.

## Technical approach

Extend existing settings rather than creating a large new configuration system.

Suggested settings:

```text
Enable disease simulation
Enable outsider importation
Enable respiratory spread
Import chance
Exposure gain multiplier
Exposure radius
Exposure threshold
Exposure decay rate
Show debug disease state
```

## Scope

Included:

- simulation on/off
- importation on/off
- respiratory spread on/off
- broad spread multiplier
- debug display toggle

Excluded:

- per-stage tuning UI
- per-faction import rates
- detailed disease profile editor
- disease mutation settings

## Implementation notes

Keep the UI small.

Most constants can remain internal until the system stabilizes.

## Acceptance criteria

- [x] simulation can be disabled
- [x] outsider importation can be disabled
- [x] respiratory spread can be disabled
- [x] import chance can be adjusted
- [x] exposure multiplier can be adjusted
- [x] debug display can be toggled
