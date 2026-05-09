# Pathogenic Residue

A RimWorld mod that adds a new kind of filth left behind by selected diseases.

Sick pawns can contaminate the floors they walk across, creating an additional cleanliness concern for colonies, hospitals, prisons, and quarantine areas.

## Features

- Adds a new filth type: pathogenic residue
- Custom texture variants
- Disease-based residue behavior
- Configurable spawn chance and cooldown
- Movement-based contamination using Harmony
- Fallback periodic scan mode
- English and French localization
- Debug tools for testing and balancing

## Currently supported conditions

- Flu
- Plague
- Malaria
- Wound infection
- Scaria infection

Supported conditions are defined through `DefModExtension`, so additional diseases can be patched in without changing the C# logic.

## Requirements

- RimWorld 1.6
- Harmony

## Compatibility

This mod does not overwrite vanilla Defs.

It uses:

- XML patches to attach metadata to selected `HediffDef`s
- A custom `ThingDef` for pathogenic residue
- A small Harmony postfix for movement-based residue tracking
- A MapComponent fallback for periodic scanning

The movement hook can be disabled in the mod settings if needed.

## Settings

The mod includes settings for:

- enabling or disabling pathogenic residue
- using the movement hook or fallback scan mode
- limiting residue to colonists only
- requiring pawn movement
- spawn chance
- per-pawn cooldown
- debug logging

## Development notes

The mod is structured around keeping behavior extensible:

- XML defines content
- patches attach disease metadata
- settings control balance
- utility classes handle shared logic
- MapComponent stores runtime state
- Harmony only detects movement and delegates the actual logic

## Folder structure

```text
About/
Assemblies/
Defs/
Languages/
Patches/
Textures/
Source/
```

## Safe removal

Before removing the mod from an existing save:

1. Load the save with the mod still enabled.
2. Open the mod settings.
3. Click **Clear all pathogenic residue**.
4. Save the game.
5. Exit RimWorld.
6. Remove the mod.

This removes spawned pathogenic residue from loaded maps before the custom `ThingDef` is removed from the mod list.