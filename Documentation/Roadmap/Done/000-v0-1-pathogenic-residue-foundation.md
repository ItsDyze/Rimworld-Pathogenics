# Feature: v0.1 Residue Foundation

## Status

Done / Deprecated as v0.2 foundation

## Goal

The original goal was to create a generic residue mechanic for selected diseases.

## Player-facing behavior

Sick pawns could leave residue on floors. Colonies had an additional cleanliness concern around hospitals, prisons, quarantine areas, and infected pawns.

## Technical approach

The v0.1 implementation used:

- custom filth `ThingDef`
- custom texture variants
- `DefModExtension` metadata on selected `HediffDef`s
- Harmony movement hook
- fallback periodic map scan
- configurable spawn chance and cooldown
- debug tools
- English and French localization
- safe removal button

## Outcome

The feature worked as a first prototype, but it is no longer the desired foundation for v0.2.

## Reason for deprecation

The mechanic treats different diseases as if they all spread through the same kind of floor residue.

The new direction is to model disease-specific transmission paths, starting with one standalone respiratory disease.

## Parts worth reusing

- settings UI
- debug infrastructure
- localization pattern
- map component pattern
- safe cleanup concept
- general mod structure

## Parts not worth extending

- generic residue for all supported diseases
- movement-based floor residue as primary disease spread
- shared residue style for unrelated diseases
