# Feature: Remove Residue Core Dependencies

## Status

Planned

## Goal

Prevent the old floor-residue implementation from being required by the new disease simulation.

## Player-facing behavior

The standalone respiratory disease should work without spawning residue on the floor.

## Technical approach

Separate old residue code from the new disease simulation.

The new system should not depend on:

- residue `ThingDef`
- movement residue hook
- residue fallback scanner
- residue spawn chance
- residue cooldown

## Scope

Included:

- isolate old residue classes
- disable old residue behavior by default
- remove old residue from v0.2 core loop
- keep old code only if useful for reference or optional legacy behavior

Excluded:

- deleting all old files immediately
- surface contamination
- migration to multiple disease profiles

## Implementation notes

Do not break the mod while refactoring.

Prefer disabling and isolating before deleting.

Suggested approach:

```text
1. Add new disease simulation classes.
2. Ensure they run without residue.
3. Disable old residue spawning by default.
4. Remove old residue references from settings text where appropriate.
5. Delete old code only after v0.2 works.
```

## Acceptance criteria

- [ ] respiratory disease simulation runs without residue spawning
- [ ] old movement residue hook is not required
- [ ] old fallback residue scan is not required
- [ ] old residue settings do not control respiratory spread
- [ ] safe cleanup still works if legacy residue exists in a save
