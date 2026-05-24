# Save Data

## Status

Implemented for v1

## Goal

Persist hidden disease simulation state safely across saves, maps, and caravans.

## Data saved

Pathogenics v1 stores authoritative disease state in `PathogenicsGameComponent`, with legacy map-owned state imported on load. For each tracked pawn:

```text
pawn reference
stage
exposure amount
exposed tick
infectious start tick
symptom onset tick
recovering tick
recovered tick
map id
preserve-across-maps flag
visible hediff applied flag
```

The game component also persists the outsider import cache so already-seen outsiders are not rerolled differently after save/load.

## v1 disease scope

Because v1 focuses on a conservative respiratory model, saved hidden state tracks diseases registered with the Pathogenics disease registry.

Current custom/default disease:

```text
DP_Coronavirus
```

Integrated vanilla disease:

```text
Flu
```

Deprecated compatibility disease:

```text
DP_PathogenicFlu
```

`DP_PathogenicFlu` remains loadable for old saves, but it is not scenario-addable, importable, transmissible, or used by new gameplay/debug paths.

Malaria, vector-borne diseases, environment-bound diseases, and broad cross-mod disease support are intentionally excluded until matching transmission routes exist.

## Cleanup and migration rules

During load or tick processing:

- import legacy `PathogenicsMapComponent` entries into the global registry
- remove entries for destroyed pawns
- remove entries for dead pawns if not needed
- remove recovered states after cleanup windows expire
- remove invalid references
- resynchronize visible hediff state with hidden state where possible

## Safe removal

The old residue cleanup path is not part of the v1 respiratory loop. A debug/admin cleanup action remains useful for old prototype saves:

```text
Reset all Pathogenics state worldwide
```

This removes hidden exposure/incubation data, visible Pathogenics hediffs, and outsider import cache state across the whole save.
