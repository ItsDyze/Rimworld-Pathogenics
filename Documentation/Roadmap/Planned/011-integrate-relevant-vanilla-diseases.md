# Feature: Integrate Relevant Vanilla Diseases with Pathogenics

## Status

Planned

## Goal

Extend Pathogenics so selected vanilla diseases can use the mod's hidden infection and transmission system instead of relying only on random spawn incidents.

The first target should be high-impact diseases that fit the current outbreak model, with plague as the initial candidate.

## Player-facing behavior

Relevant diseases no longer feel like isolated random events. When enabled, they can enter the colony through Pathogenics-supported infection sources and follow the same broader outbreak logic as the custom disease simulation.

Players can also control how vanilla disease incidents behave:

- disable all vanilla spawn-disease incidents entirely
- disable only incidents for diseases that are integrated with Pathogenics
- leave non-integrated diseases untouched

This keeps the disease experience more consistent while still letting players choose how much vanilla randomness remains in the game.

## Technical approach

Add a disease-integration layer that maps selected vanilla `HediffDef`s to Pathogenics-compatible profiles or handling rules.

For each integrated disease:

- define whether it is supported by the hidden simulation loop
- define whether it can be imported through the existing outsider importation logic
- define whether it should use the current exposure and transmission rules or disease-specific tuning
- define whether the vanilla random disease incident for that disease should be suppressed when the corresponding setting is enabled

Add a new settings section for disease event handling with two distinct controls:

- disable all vanilla spawn-disease incidents
- disable only incidents tied to diseases integrated with Pathogenics

The implementation should prefer a data-driven registration path so additional diseases can be integrated later without reworking the core system.

## Scope

Included:

- integration framework for relevant vanilla diseases
- first-pass integration of at least one suitable disease, starting with plague
- settings to disable all vanilla spawn-disease incidents
- settings to disable only integrated-disease incidents
- clear player-facing descriptions of how integrated diseases behave
- documentation updates across roadmap and player docs

Excluded:

- full integration of every vanilla disease
- custom tuning UI for every individual disease
- support for diseases that do not fit the current transmission model
- major rework of the core Pathogenics outbreak loop
- cross-mod disease compatibility pass

## Implementation notes

The main design risk is consistency. If a disease is marked as integrated, players should not still see that same disease arriving from vanilla random incidents unless settings explicitly allow it.

Disease selection should stay conservative. Only diseases that make sense for the current Pathogenics model should be integrated. It is better to support a small number of diseases cleanly than to force every vanilla illness into the same system.

Plague is a strong first candidate from a player-value perspective, but the implementation should confirm that it fits the hidden-state and transmission model well enough. If it needs disease-specific handling, that should be documented explicitly.

Documentation should be updated in at least these places when the feature is implemented:

- `README.md`
- `Documentation/STEAM_DESCRIPTION.md`
- roadmap feature files
- any relevant design document describing the disease model and vanilla event replacement behavior

## Acceptance criteria

- [ ] at least one vanilla disease is integrated into the Pathogenics system
- [ ] integrated diseases can use Pathogenics-controlled infection flow instead of only vanilla random incidents
- [ ] players can disable all vanilla spawn-disease incidents in settings
- [ ] players can disable only incidents for integrated diseases in settings
- [ ] non-integrated diseases remain unaffected when using the integrated-only option
- [ ] documentation clearly explains which diseases are integrated and how incident suppression works
- [ ] README and Steam description are updated to reflect the new behavior
