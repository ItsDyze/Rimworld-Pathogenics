using RimWorld;
using Verse;

namespace Dyze.RimWorld.Pathogenics
{
    /// <summary>
    /// Utility class for Pathogenics v0.2+.
    /// 
    /// The residue system has been completely removed.
    /// Disease simulation uses hidden state tracking (PawnDiseaseState).
    /// </summary>
    public static class DyzePathogenicsUtility
    {
        // The v0.2 disease simulation uses:
        // - PathogenicsMapComponent.pawnDiseaseStates for hidden state
        // - PawnDiseaseState class for per-pawn tracking
        // No residue system exists in v0.2+
    }
}