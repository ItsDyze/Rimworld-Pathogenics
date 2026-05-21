# Respiratory Disease Model

## Status

Planned model for v0.2

## Goal

Model one standalone respiratory disease with realistic spread behavior.

## Core loop

```text
Imported infected pawn
        ↓
Hidden incubation
        ↓
Pre-symptomatic infectious period
        ↓
Respiratory exposure to nearby pawns
        ↓
Exposure accumulation
        ↓
New incubation
        ↓
Visible disease
```

## Disease stages

The disease should use hidden simulation stages before the visible `HediffDef` appears.

Recommended stages:

```text
Healthy
Exposed
Incubating
PreSymptomaticInfectious
Symptomatic
Recovering
Recovered
```

## Incubation versus infectiousness

Incubation is the time between infection and visible symptoms.

A pawn may become infectious during incubation if the disease profile allows it.

For the standalone respiratory disease, pre-symptomatic transmission should be enabled.

## Transmission routes for v0.2

Primary route:

```text
shared air / proximity exposure
```

Secondary routes are excluded from the initial implementation:

```text
surface contamination
floor residue
vector transmission
bodily fluids
```

## Disease profile integration

The model is now backed by a small disease profile registry. Each integrated disease declares whether it:

- uses the hidden simulation loop
- can be imported by outsiders
- uses the current respiratory transmission rules
- should have its vanilla random disease incident suppressed when integrated-incident suppression is enabled

Current integrated diseases:

| Disease | Def | Reason |
|---------|-----|--------|
| Pathogenic flu | `DP_PathogenicFlu` | Original custom Pathogenics disease |
| Vanilla flu | `Flu` | Fits the existing shared-air respiratory model |

Explicitly excluded for now:

- malaria and other insect/vector-borne diseases
- environment-bound diseases that need unsupported transmission routes
- cross-mod diseases pending a compatibility pass

## Player counterplay

The player should be able to reduce spread by using existing RimWorld behavior:

- isolate sick pawns
- keep infected pawns in separate rooms
- avoid crowding
- reduce shared dining or recreation exposure
- separate visitors or prisoners from colonists

## Future expansion

Once the standalone disease works, the model can be generalized into disease profiles with different routes.
