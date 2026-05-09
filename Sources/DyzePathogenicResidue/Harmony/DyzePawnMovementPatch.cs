using HarmonyLib;
using Verse;
using Verse.AI;

namespace Dyze.RimWorld.PathogenicResidue
{
    [HarmonyPatch(typeof(Pawn_PathFollower), "TryEnterNextPathCell")]
    public static class DyzePawnMovementPatch
    {
        public static void Postfix(Pawn ___pawn)
        {
            DyzePathogenicResidueSettings settings = DyzePathogenicResidueMod.Settings;

            if (settings == null || !settings.Enabled || !settings.UseMovementHook)
            {
                return;
            }

            Pawn pawn = ___pawn;

            if (pawn?.Map == null || !pawn.Spawned)
            {
                return;
            }

            PathogenicResidueMapComponent component =
                pawn.Map.GetComponent<PathogenicResidueMapComponent>();

            component?.TryProcessPawnMovement(pawn);
        }
    }
}