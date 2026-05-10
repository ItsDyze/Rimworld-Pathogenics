using HarmonyLib;
using Verse;
using Verse.AI;

namespace Dyze.RimWorld.Pathogenics
{
    /// <summary>
    /// Harmony patch for pawn movement.
    /// 
    /// This patch was previously used for movement-based residue spawning.
    /// The residue system has been completely removed in v0.2+.
    /// The v0.2 disease simulation uses hidden state tracking (PawnDiseaseState) instead.
    /// </summary>
    [HarmonyPatch(typeof(Pawn_PathFollower), "TryEnterNextPathCell")]
    public static class DyzePawnMovementPatch
    {
        public static void Postfix(Pawn ___pawn)
        {
            // v0.2+: Movement-based residue spawning has been removed.
            // The disease simulation uses hidden disease state system (PawnDiseaseState).
        }
    }
}