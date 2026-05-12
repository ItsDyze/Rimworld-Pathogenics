# Feature: Incubation and Symptom Onset

## Status

Done

## Goal

Delay visible disease symptoms after infection.

## Player-facing behavior

A pawn can become infected and only develop visible symptoms later.

This creates uncertainty and allows pre-symptomatic spread.

## Technical approach

When exposure crosses the threshold, create an incubation state with scheduled transition ticks.

Suggested fields:

```text
infectionTick
infectiousStartTick
symptomOnsetTick
infectiousEndTick
recoveryTick
```

At `symptomOnsetTick`, apply the visible `DP_PathogenicFlu` hediff.

## Scope

Included:

- incubation timer
- symptom onset timer
- visible disease application
- notification when a colonist develops symptoms

Excluded:

- advanced disease severity progression
- treatment effects on hidden incubation
- relapse
- long-term immunity

## Implementation notes

For a respiratory disease, infectiousness may begin before visible symptoms.

Example testing timeline:

```text
infection: now
infectious start: +0.5 days
symptom onset: +1.5 days
infectious end: +5 days
```

## Acceptance criteria

- [x] exposure threshold creates incubation
- [x] incubation persists across save/load
- [x] symptoms appear after configured time
- [x] visible hediff is applied at symptom onset
- [x] colonist symptom onset can trigger a letter/message
