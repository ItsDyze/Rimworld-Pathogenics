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
    /// Heuristic: A pawn is considered "wearing a mask" if worn apparel looks like
    /// respiratory protection by one of these compile-safe signals:
    /// - it covers the FullHead, Jaw, or Mouth body part groups
    /// - it uses the FaceCover apparel layer
    /// - it provides ToxicEnvironmentResistance
    /// 
    /// This is intentionally broader than a single body-part-group check because vanilla
    /// and modded face masks can surface through different apparel defs/layouts.
    /// </summary>
    public static class MaskUtility
    {
        private static BodyPartGroupDef JawBodyPartGroupDef => DefDatabase<BodyPartGroupDef>.GetNamedSilentFail("Jaw");
        private static BodyPartGroupDef MouthBodyPartGroupDef => DefDatabase<BodyPartGroupDef>.GetNamedSilentFail("Mouth");
        private static ApparelLayerDef FaceCoverApparelLayerDef => DefDatabase<ApparelLayerDef>.GetNamedSilentFail("FaceCover");
        private static StatDef ToxicEnvironmentResistanceStatDef => DefDatabase<StatDef>.GetNamedSilentFail("ToxicEnvironmentResistance");

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

                // Check if this apparel covers the breathing path / lower face
                if (ProvidesRespiratoryProtection(apparel.def))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Check if an apparel def provides respiratory protection.
        /// </summary>
        /// <param name="apparelDef">The apparel def to check</param>
        /// <returns>True if it looks like mask/face respiratory protection</returns>
        private static bool ProvidesRespiratoryProtection(ThingDef apparelDef)
        {
            if (apparelDef?.apparel == null)
                return false;

            return HasRespiratoryBodyPartCoverage(apparelDef) ||
                   HasFaceCoverLayer(apparelDef) ||
                   HasToxicEnvironmentResistance(apparelDef);
        }

        private static bool HasRespiratoryBodyPartCoverage(ThingDef apparelDef)
        {
            if (apparelDef.apparel.bodyPartGroups == null)
                return false;

            BodyPartGroupDef jawGroup = JawBodyPartGroupDef;
            BodyPartGroupDef mouthGroup = MouthBodyPartGroupDef;
            foreach (BodyPartGroupDef bodyPartGroup in apparelDef.apparel.bodyPartGroups)
            {
                if (bodyPartGroup == BodyPartGroupDefOf.FullHead ||
                    (jawGroup != null && bodyPartGroup == jawGroup) ||
                    (mouthGroup != null && bodyPartGroup == mouthGroup))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasFaceCoverLayer(ThingDef apparelDef)
        {
            if (apparelDef.apparel.layers == null)
                return false;

            ApparelLayerDef faceCoverLayer = FaceCoverApparelLayerDef;
            if (faceCoverLayer == null)
                return false;

            foreach (ApparelLayerDef layer in apparelDef.apparel.layers)
            {
                if (layer == faceCoverLayer)
                    return true;
            }

            return false;
        }

        private static bool HasToxicEnvironmentResistance(ThingDef apparelDef)
        {
            StatDef toxicEnvironmentResistance = ToxicEnvironmentResistanceStatDef;
            if (toxicEnvironmentResistance == null)
                return false;

            return apparelDef.GetStatValueAbstract(toxicEnvironmentResistance) > 0f;
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
                BodyPartGroupDef jawGroup = JawBodyPartGroupDef;
                BodyPartGroupDef mouthGroup = MouthBodyPartGroupDef;
                foreach (Apparel apparel in pawn.apparel.WornApparel)
                {
                    if (apparel?.def?.apparel != null)
                    {
                        foreach (var bpg in apparel.def.apparel.bodyPartGroups)
                        {
                            if (bpg == BodyPartGroupDefOf.FullHead ||
                                (jawGroup != null && bpg == jawGroup) ||
                                (mouthGroup != null && bpg == mouthGroup))
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
