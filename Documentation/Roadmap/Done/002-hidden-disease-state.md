# Feature: Hidden Disease State

## Status

Done

## Goal

Track infection state before the visible disease appears.

## Player-facing behavior

A pawn may be infected or incubating without the player immediately seeing a health condition.

Symptoms appear later.

## Technical approach

Hidden simulation state stored by the map component (`PathogenicsMapComponent`).

Implemented enum:

```csharp
public enum SimulatedDiseaseStage
{
    None = 0,
    Exposed = 1,
    Incubating = 2,
    PreSymptomaticInfectious = 3,
    Symptomatic = 4,
    Recovering = 5,
    Recovered = 6
}
```

Implemented files:

```text
Sources/DyzePathogenics/Runtime/PawnDiseaseState.cs
```

The `PawnDiseaseState` class tracks:
- Current stage
- Exposed tick
- Infectious start tick
- Symptom onset tick
- Recovering tick
- Recovered tick

State is stored in `PathogenicsMapComponent.pawnDiseaseStates`.

## Scope

Included:

- hidden state per pawn
- current disease stage
- key transition ticks
- debug readout
- save/load persistence via Scribe

Excluded:

- multiple disease support
- immunity history across maps
- world pawn tracking
- automatic stage progression (tick-based)

## Implementation notes

For v0.2, the system is specific to DP_PathogenicFlu (the standalone respiratory disease).

The hidden state is kept minimal to avoid over-generalizing. Future features (incubation, exposure accumulation) will extend this foundation.

Debug actions added:
- "Expose selected pawn (hidden infection)" - simulate hidden exposure
- "Inspect hidden disease state" - show current stage and timing info
- "Clear hidden disease state (selected pawn)" - clear one pawn's state
- "Clear all hidden disease states (map)" - clear all states on current map
- "Force symptom onset (apply visible disease)" - transition to visible HediffDef

## Acceptance criteria

- [x] selected pawn can receive a hidden disease state
- [x] hidden state persists through save/load
- [x] hidden state can be cleared
- [x] debug readout shows current stage
- [x] hidden state is separate from visible `HediffDef`

## Implementation detail: Visible and hidden state are separate

**Important nuance discovered during testing:**

The visible disease (`DP_PathogenicFlu` HediffDef) and the hidden disease state (`PawnDiseaseState`) are **separate systems** that must be synchronized:

- **Visible:** `DP_PathogenicFlu` is a standard RimWorld `HediffDef` that appears in the health tab
- **Hidden:** `PawnDiseaseState` tracks pre-symptomatic progression before the HediffDef is applied
- **Synchronization:** Debug actions were updated to keep both in sync:
  - "Apply pathogenic flu" now creates/updates both the visible HediffDef AND the hidden PawnDiseaseState
  - "Remove pathogenic flu" clears both
  - "Log disease state" checks both: if a pawn has visible HediffDef but no hidden state, it creates one

This separation allows the disease simulation to track incubation before symptoms appear, while still using RimWorld's native Hediff system for the visible disease representation.

## v0.2 Expansion: Residue System Removal

**Merged from Feature 010**

This feature was expanded to include removal of the old residue system from the v0.2 active core.

### Changes made (v0.2.1 cleanup):

1. **MapComponent**: Residue spawning logic is permanently disabled
   - `TryProcessPawnMovement()` returns false immediately
   - Tick-based scan does nothing active
   - Residue tracking dictionaries retained only for save compatibility

2. **Harmony Patch**: Movement hook is permanently disabled
   - Patch retained for save compatibility but inactive
   - No longer toggleable via settings

3. **Settings**: Removed `UseLegacyResidue` flag
   - Legacy residue settings retained internally for save compat only
   - No longer user-facing in the settings UI

4. **Debug Actions**: Updated status action
   - Removed references to legacy residue toggle
   - Shows current disease simulation state instead

5. **Cleanup Tools**: Kept only for legacy save cleanup
   - "Clear legacy residue" button in settings
   - Legacy cleanup debug action

### Rationale

The v0.2 disease simulation uses hidden state tracking (airborne/proximity) rather than floor residue. The residue system is no longer a user-facing feature - only cleanup of old saves is supported.

Legacy saves with residue are still supported - the cleanup tools remain available.