# Feature: Standalone Respiratory Disease

## Status

Done

## Goal

Create one custom respiratory disease that serves as the prototype for the v0.2 disease simulation.

## Player-facing behavior

A new illness can appear in the colony after hidden incubation. It behaves like a normal visible disease once symptoms begin, but the infection process is controlled by the custom simulation rather than vanilla random disease incidents.

## Technical approach

Add a new `HediffDef`.

Implemented def name:

```text
DP_PathogenicFlu
```

The disease does not replace vanilla Flu.

## Scope

Included:

- custom disease `HediffDef`
- English label and description
- French label and description
- debug action to apply the visible disease
- debug action to remove the visible disease
- custom non-widget severity progression
- early and late symptomatic stages
- non-lethal disease lifecycle for testing

Excluded:

- vanilla Flu modification
- multiple diseases
- disease importation
- proximity spread
- surface contamination

## Implementation notes

A standalone disease avoids conflicts with vanilla disease logic and makes testing easier.

The disease can later become a template for generalized disease profiles.

This implementation intentionally delivers the visible disease layer plus debug tools as a clean vertical slice. The current prototype uses custom code-driven severity progression rather than the vanilla disease widget, so it can move through symptom stages without relying on immunizable UI behavior. Hidden incubation and transmission systems remain for later roadmap items.

Current disease behavior:

- starts as a mild visible illness
- progresses over time from early symptomatic to late symptomatic
- applies modest late-stage movement and manipulation penalties
- is non-lethal for testing
- removes itself when its configured maximum severity is reached

## Acceptance criteria

- [x] `DP_PathogenicFlu` exists as a custom `HediffDef`
- [x] disease appears correctly in the pawn health tab when applied
- [x] disease has English localization
- [x] disease has French localization
- [x] debug action can apply it to a selected pawn
- [x] debug action can remove it from a selected pawn
- [x] disease progresses from early symptomatic to late symptomatic over time
- [x] disease remains non-lethal for prototype testing
