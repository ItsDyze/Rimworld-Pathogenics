# Feature: Player Feedback and Debugging

## Status

Planned

## Goal

Give enough feedback for the player and enough visibility for development.

## Player-facing behavior

The player should receive a message when a colonist develops visible symptoms.

The player should not automatically know every hidden infection state unless debug mode is enabled.

## Technical approach

Add normal feedback and debug feedback separately.

Normal mode:

- symptom onset letter/message
- optional disease description explaining transmission risk

Debug mode:

- selected pawn state
- exposure amount
- infection timeline
- infectiousness multiplier
- recent exposure events

Suggested files:

```text
Source/UI/DiseaseDebugReadout.cs
Source/Letters/DiseaseLetterUtility.cs
Source/Debug/PathogenicDiseaseDebugActions.cs
```

## Scope

Included:

- symptom onset notification
- debug readout
- debug add exposure
- debug clear state
- debug infect selected pawn

Excluded:

- full outbreak dashboard
- overlay visualization
- hospital risk UI
- room contamination UI

## Implementation notes

Debug tools are essential for balancing.

Normal gameplay should remain somewhat uncertain.

## Acceptance criteria

- [ ] colonist symptom onset creates feedback
- [ ] debug mode shows hidden disease state
- [ ] debug mode shows exposure
- [ ] debug action can infect selected pawn
- [ ] debug action can clear selected pawn state
- [ ] debug action can print current simulation state
