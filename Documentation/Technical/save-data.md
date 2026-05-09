# Save Data

## Status

Planned

## Goal

Persist hidden disease simulation state safely across saves.

## Data to save

For each tracked pawn:

```text
pawn reference
stage
exposure amount
infection tick
infectious start tick
symptom onset tick
infectious end tick
recovery tick
source information, optional
```

## First version simplification

Because v0.2 focuses on one disease, the save data can track only the standalone respiratory disease.

Initial structure:

```text
Pawn -> PathogenicFluState
```

Future structure:

```text
Pawn -> DiseaseDef -> DiseaseState
```

## Cleanup rules

During load or tick processing:

- remove entries for destroyed pawns
- remove entries for dead pawns if not needed
- remove recovered states after immunity expires
- remove invalid references
- avoid keeping references to world pawns unnecessarily

## Safe removal

The old residue cleanup button is less important for v0.2 if no custom residue things are spawned.

However, a debug/admin cleanup action is still useful:

```text
Clear all simulated disease state
```

This should remove hidden exposure/incubation data from loaded maps.
