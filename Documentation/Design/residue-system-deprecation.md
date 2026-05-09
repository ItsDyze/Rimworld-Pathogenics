# Residue System Deprecation

## Status

Deprecated for v0.2

## Summary

The original pathogenic residue system should not be expanded as the main disease simulation mechanic.

It can be kept temporarily for reference, testing, or legacy support, but v0.2 should not build more features on top of generic floor residue.

## Deprecated behavior

The old model:

```text
Pawn has supported disease
        ↓
Pawn walks
        ↓
Pathogenic residue may spawn on the floor
        ↓
Other pawns may interact with the residue
```

## Reason for deprecation

This model treats very different diseases as if they share the same environmental transmission path.

That is not realistic enough for the new direction.

Examples:

- flu-like diseases should primarily spread through shared air and close contact
- malaria should require a vector model, not floor residue
- wound infection should be linked to wounds, treatment, hygiene, and medical environments
- plague-like diseases may require animals, corpses, fleas, or respiratory variants depending on the form

## What to keep

The following parts may still be useful:

- mod settings structure
- debug logging infrastructure
- map component pattern
- safe removal button concept
- localization structure
- XML/DefModExtension experience
- Harmony patching experience

## What to remove or stop extending

The following should not be part of the v0.2 core:

- generic residue for all diseases
- movement-based floor contamination as the primary spread mechanic
- fallback scanning for residue placement
- one shared residue style for unrelated diseases
- disease behavior driven only by `HediffDef` residue metadata

## Future use

Environmental contamination can return later as a disease-specific route.

Examples:

- contaminated surfaces for flu-like diseases as a secondary route
- contaminated beds or tools for hospital-acquired infections
- corpse or animal contamination for plague-like diseases
- bodily-fluid filth for specific infections

But it should not be the generic foundation.
