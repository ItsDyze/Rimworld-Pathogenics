# Feature: Incubation and Symptom Onset

## Status

Planned

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

At `symptomOnsetTick`, apply the visible `PR_PathogenicFlu` hediff.

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

- [ ] exposure threshold creates incubation
- [ ] incubation persists across save/load
- [ ] symptoms appear after configured time
- [ ] visible hediff is applied at symptom onset
- [ ] colonist symptom onset can trigger a letter/message
