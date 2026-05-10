# Feature: Remove Residue Core Dependencies

## Status

**Done** (Merged into Feature 002)

## Goal

Prevent the old floor-residue implementation from being required by the new disease simulation.

## Resolution

This feature was **merged into Feature 002** as part of the v0.2 expansion. The residue system removal is now complete.

### Summary of work:

1. Disabled residue spawning permanently (no longer toggleable)
2. Removed `UseLegacyResidue` flag from user-facing settings
3. Made Harmony movement patch permanently disabled
4. Marked residue utility methods as legacy
5. Removed residue-specific debug actions from main set
6. Updated README to reflect v0.2 direction
7. Retained cleanup tools for legacy saves only

### What is kept for legacy support:

- Residue ThingDef and filth definitions
- Cleanup debug action
- Clear all residue button in settings
- Save compatibility for residue data (Scribe)

### What was removed from active core:

- Movement hook residue spawning
- Fallback tick-based residue spawning
- Residue-specific settings in UI
- User-toggleable residue enable
- Hediff patches against vanilla diseases
- Disabled XML patch artifact

## Player-facing behavior

The standalone respiratory disease works without spawning residue on the floor.
Residue is no longer a toggleable feature - only cleanup is supported.

## Acceptance criteria

- [x] respiratory disease simulation runs without residue spawning
- [x] old movement residue hook is not required
- [x] old fallback residue scan is not required
- [x] residue is not a toggleable user option
- [x] safe cleanup still works if legacy residue exists in a save

## See also

- Feature 002: Hidden Disease State (expanded to include this work)