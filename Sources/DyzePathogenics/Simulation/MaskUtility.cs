using RimWorld;
using Verse;
using Dyze.RimWorld.Pathogenics;

namespace Dyze.RimWorld.Pathogenics.Simulation
{
    /// <summary>
    /// Utility for detecting whether a pawn is wearing a mask or other face-covering apparel.
    /// 
    /// v0.3.1: Masks reduce disease exposure during respiratory transmission.
    /// 
    /// Heuristic: A pawn is considered "wearing a mask" if they have any worn apparel
    /// that covers the FullHead body part group. This includes:
    /// - Dust masks (basic respiratory protection)
    /// - Advanced dust masks
    /// - Full face helmets/goggles
    /// - Any other apparel that covers the full head including mouth/nose
    /// 
    /// Note: This uses a compile-safe heuristic based on body part group coverage,
    /// as specific apparel def checking would require maintaining a list of def names.
    /// </summary>
    public static class MaskUtility
    {
        /// <summary>
        /// Multiplier applied when only the SOURCE pawn is wearing a mask.
        /// Reduces emitted exposure by 75% (only 25% escapes).
        /// </summary>
        public const float SourceMaskMultiplier = 0.25f;

        /// <summary>
        /// Multiplier applied when only the TARGET pawn is wearing a mask.
        /// Reduces received exposure by 75% (only 25% is inhaled).
        /// </summary>
        public const float TargetMaskMultiplier = 0.25f;

        /// <summary>
        /// Multiplier applied when BOTH source and target are wearing masks.
        /// Exposure is completely blocked (0% transmission).
        /// </summary>
        public const float BothMasksMultiplier = 0.0f;

        /// <summary>
        /// Multiplier applied when NEITHER pawn is wearing a mask.
        /// Normal transmission (100% exposure).
        /// </summary>
        public const float NoMaskMultiplier = 1.0f;

        /// <summary>
        /// Check if a pawn is wearing a mask or face-covering apparel.
        /// </summary>
        /// <param name="pawn">The pawn to check</param>
        /// <returns>True if the pawn has apparel covering the full head (mask-like protection)</returns>
        public static bool IsWearingMask(Pawn pawn)
        {
            if (pawn == null)
                return false;

            // Check if pawn has an apparel tracker
            if (pawn.apparel == null)
                return false;

            // Iterate through worn apparel
            foreach (Apparel apparel in pawn.apparel.WornApparel)
            {
                if (apparel == null || apparel.def == null)
                    continue;

                // Check if this apparel covers the full head
                if (CoversFullHead(apparel.def))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Check if an apparel def covers the full head (including mouth/nose).
        /// </summary>
        /// <param name="apparelDef">The apparel def to check</param>
        /// <returns>True if it covers the full head</returns>
        private static bool CoversFullHead(ThingDef apparelDef)
        {
            if (apparelDef?.apparel == null)
                return false;

            // Check body part groups covered by this apparel
            foreach (BodyPartGroupDef bodyPartGroup in apparelDef.apparel.bodyPartGroups)
            {
                // FullHead includes coverage of mouth and nose
                if (bodyPartGroup == BodyPartGroupDefOf.FullHead)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Get the exposure multiplier based on mask status of both source and target pawns.
        /// </summary>
        /// <param name="sourcePawn">The infectious source pawn</param>
        /// <param name="targetPawn">The potentially exposed target pawn</param>
        /// <returns>Exposure multiplier to apply to transmission</returns>
        public static float GetExposureMultiplier(Pawn sourcePawn, Pawn targetPawn)
        {
            bool sourceMasked = IsWearingMask(sourcePawn);
            bool targetMasked = IsWearingMask(targetPawn);

            // Both masked: complete protection
            if (sourceMasked && targetMasked)
                return BothMasksMultiplier;

            // Source only masked: reduced emission
            if (sourceMasked)
                return SourceMaskMultiplier;

            // Target only masked: reduced intake
            if (targetMasked)
                return TargetMaskMultiplier;

            // Neither masked: normal transmission
            return NoMaskMultiplier;
        }

        /// <summary>
        /// Get a debug info string for a pawn's mask status.
        /// </summary>
        /// <param name="pawn">The pawn to check</param>
        /// <returns>Debug string describing mask status</returns>
        public static string GetDebugInfo(Pawn pawn)
        {
            if (pawn == null)
                return "Pawn is null";

            bool masked = IsWearingMask(pawn);
            
            // Find what apparel is providing the mask effect
            if (masked && pawn.apparel != null)
            {
                var maskedApparel = new System.Collections.Generic.List<string>();
                foreach (Apparel apparel in pawn.apparel.WornApparel)
                {
                    if (apparel?.def?.apparel != null)
                    {
                        foreach (var bpg in apparel.def.apparel.bodyPartGroups)
                        {
                            if (bpg == BodyPartGroupDefOf.FullHead)
                            {
                                maskedApparel.Add(apparel.LabelShort);
                                break;
                            }
                        }
                    }
                }
                
                if (maskedApparel.Count > 0)
                    return $"Masked ({string.Join(", ", maskedApparel)})";
            }

            return masked ? "Masked" : "Not masked";
        }
    }
}
