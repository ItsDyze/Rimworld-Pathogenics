# Feature: Standalone Respiratory Disease

## Status

Planned

## Goal

Create one custom respiratory disease that serves as the prototype for the v0.2 disease simulation.

## Player-facing behavior

A new illness can appear in the colony after hidden incubation. It behaves like a normal visible disease once symptoms begin, but the infection process is controlled by the custom simulation rather than vanilla random disease incidents.

## Technical approach

Add a new `HediffDef`.

Suggested def name:

```text
PR_PathogenicFlu
```

The disease should not replace vanilla Flu at first.

## Scope

Included:

- custom disease `HediffDef`
- English label and description
- French label and description
- debug action to apply the visible disease
- debug action to remove the visible disease

Excluded:

- vanilla Flu modification
- multiple diseases
- disease importation
- proximity spread
- surface contamination

## Implementation notes

A standalone disease avoids conflicts with vanilla disease logic and makes testing easier.

The disease can later become a template for generalized disease profiles.

## Acceptance criteria

- [ ] `PR_PathogenicFlu` exists as a custom `HediffDef`
- [ ] disease appears correctly in the pawn health tab
- [ ] disease has English localization
- [ ] disease has French localization
- [ ] debug action can apply it to a selected pawn
- [ ] debug action can remove it from a selected pawn
