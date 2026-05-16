# Feature: Carrier Mechanic

## Status

Planned

## Goal

Add a carrier state to Pathogenics so some pawns can harbor and spread disease without presenting obvious symptoms.

This should deepen outbreak uncertainty, create more believable hidden spread, and support colony stories where disease is tolerated, cultivated, or even ideologically embraced.

## Player-facing behavior

Some infected pawns do not progress into a visibly sick state right away, or at all. Instead, they can remain outwardly functional while still acting as a source of infection.

This creates a different kind of threat from clearly symptomatic pawns:

- a colony may appear healthy while transmission is still ongoing
- quarantine decisions become less obvious
- disease-positive colonies can intentionally keep carriers inside the population
- disease-worshiping or disease-tolerant colony themes gain a stronger gameplay identity

Carriers should still be understandable within the mod's design. The system should create uncertainty, not pure randomness.

## Technical approach

Extend the hidden disease-state model to support a carrier outcome or carrier-capable stage.

Possible implementation directions:

- add a dedicated `Carrier` hidden stage
- allow some disease profiles to branch from incubation into a long-term asymptomatic infectious state
- allow carriers to remain infectious with different exposure output than symptomatic pawns
- optionally allow carriers to resolve, relapse into symptoms, or remain chronic depending on disease rules

Carrier behavior should be controlled by disease-profile data rather than hardcoded for a single illness.

At minimum, the system needs to define:

- when a pawn becomes a carrier
- whether carrier chance is disease-specific
- whether carriers are always infectious or only under certain conditions
- whether carriers ever become visibly sick later
- what notifications, if any, the player receives

The implementation should reuse the current hidden-state architecture as much as possible rather than creating a parallel disease system.

## Scope

Included:

- hidden carrier state or equivalent carrier outcome in the disease lifecycle
- transmission from carrier pawns
- disease-profile support for carrier-capable diseases
- balancing hooks for carrier frequency and infectiousness
- documentation updates describing the mechanic and its gameplay purpose

Excluded:

- ideology-specific content or rituals
- special UI for disease-worshiping colonies
- perfectly realistic medical modeling for every disease
- guaranteed carrier support for every disease in the game
- deep diagnostic gameplay beyond what current player feedback tools support

## Implementation notes

The main risk is fairness. A carrier mechanic can make outbreaks more interesting, but if it is too opaque it will feel like the game is cheating.

The best version of this feature adds uncertainty while still leaving room for player reasoning. Existing debug tools can help during development, and player-facing feedback should eventually give enough clues that carriers feel dangerous rather than arbitrary.

Carrier behavior should also be tuned carefully against existing pre-symptomatic spread. If both systems are too strong at once, the distinction between incubation and carrier states becomes muddy.

For thematic value, this feature is especially useful for colonies that deliberately accept disease risk. That supports the user request directly without requiring the mod to become an Ideology-specific content pack.

Documentation should be updated in at least these places when the feature is implemented:

- `README.md`
- `Documentation/STEAM_DESCRIPTION.md`
- relevant roadmap feature files
- any design document describing hidden disease stages and transmission behavior

## Acceptance criteria

- [ ] the hidden disease model supports a carrier-capable outcome or stage
- [ ] carrier pawns can spread disease without requiring visible symptoms
- [ ] carrier behavior is configurable or data-driven at the disease level
- [ ] carriers are documented clearly enough that the mechanic feels intentional rather than bug-like
- [ ] documentation explains how carrier states differ from normal incubation and symptomatic illness
