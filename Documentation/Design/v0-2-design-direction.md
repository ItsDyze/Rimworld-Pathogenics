# v0.2 Design Direction

## Status

Current direction

## Summary

Version 0.2 should move away from the original generic residue model and toward a standalone respiratory disease simulation.

The original implementation treated multiple diseases as if they all produced the same kind of floor residue. That was useful as a prototype, but it is too generic for a realism-oriented disease simulation.

## New design principle

Each disease should eventually have its own transmission path.

For v0.2, the mod should not try to model every infection. It should model one disease well.

## v0.2 target

Create one custom respiratory disease with:

- outsider importation
- hidden incubation
- pre-symptomatic infectiousness
- proximity-based respiratory exposure
- visible symptom onset after incubation
- basic player feedback
- debug tools for balancing

## Explicit pivot

The residue system is no longer the foundation of v0.2.

Deprecated direction:

```text
Any supported disease → generic residue on floor → generic exposure
```

New direction:

```text
Standalone respiratory disease → shared-air exposure → incubation → symptoms → spread
```

## Why this pivot matters

The new model is more realistic, easier to reason about, and easier to expand later.

A strong standalone disease gives the mod a stable simulation foundation. After that, additional diseases can be added with their own specific transmission rules.
