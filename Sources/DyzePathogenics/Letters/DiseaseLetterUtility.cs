using System;
using RimWorld;
using Verse;
using Dyze.RimWorld.Pathogenics.Integration;

namespace Dyze.RimWorld.Pathogenics
{
    /// <summary>
    /// Utility for creating disease-related letters and notifications.
    /// </summary>
    public static class DiseaseLetterUtility
    {
        /// <summary>
        /// Send a letter when a colonist develops visible symptoms.
        /// </summary>
        public static void SendSymptomOnsetLetter(Pawn pawn)
        {
            if (pawn == null)
                return;

            PathogenicsDiseaseProfile profile = GetProfileForPawn(pawn);
            string diseaseLabel = profile?.HediffDef?.label ?? "disease";
            string label = "DP_DiseaseDetectedLabel".Translate(diseaseLabel.CapitalizeFirst()).ToString();
            string text = GetSymptomOnsetText(pawn, diseaseLabel);

            LetterDef letterDef = LetterDefOf.ThreatSmall;
            LookTargets lookTargets = new LookTargets(pawn);

            Find.LetterStack.ReceiveLetter(label, text, letterDef, lookTargets);
        }

        /// <summary>
        /// Get the text for symptom onset notification.
        /// </summary>
        private static string GetSymptomOnsetText(Pawn pawn, string diseaseLabel)
        {
            // Base text
            string baseText = "DP_DiseaseDetectedDesc".Translate(pawn.Named("PAWN"), diseaseLabel).ToString();

            // Add transmission warning if settings allow
            if (DyzePathogenicsMod.Settings?.ShowTransmissionWarning == true)
            {
                string warning = "\n\n" + "DP_TransmissionWarning".Translate();
                return baseText + warning;
            }

            return baseText;
        }

        /// <summary>
        /// Send a letter when a colonist recovers.
        /// </summary>
        public static void SendRecoveryLetter(Pawn pawn)
        {
            if (pawn == null)
                return;

            PathogenicsDiseaseProfile profile = GetProfileForPawn(pawn);
            string diseaseLabel = profile?.HediffDef?.label ?? "disease";
            string label = "DP_DiseaseRecoveredLabel".Translate(diseaseLabel.CapitalizeFirst()).ToString();
            string text = "DP_DiseaseRecoveredDesc".Translate(pawn.Named("PAWN"), diseaseLabel).ToString();

            LetterDef letterDef = LetterDefOf.PositiveEvent;
            LookTargets lookTargets = new LookTargets(pawn);

            Find.LetterStack.ReceiveLetter(label, text, letterDef, lookTargets);
        }

        private static PathogenicsDiseaseProfile GetProfileForPawn(Pawn pawn)
        {
            PawnDiseaseState state = PathogenicsGameComponent.Instance?.TryGetDiseaseState(pawn);
            return PathogenicsDiseaseRegistry.GetProfile(state);
        }

        /// <summary>
        /// Send a message (non-letter notification) for less critical events.
        /// </summary>
        public static void SendMessage(string text, MessageTypeDef messageType = null)
        {
            messageType ??= MessageTypeDefOf.NeutralEvent;
            Messages.Message(text, messageType, false);
        }
    }
}