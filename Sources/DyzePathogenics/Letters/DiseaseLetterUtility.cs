using System;
using RimWorld;
using Verse;

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

            string label = "DP_PathogenicFluDetectedLabel".Translate();
            string text = GetSymptomOnsetText(pawn);

            LetterDef letterDef = LetterDefOf.ThreatSmall;
            LookTargets lookTargets = new LookTargets(pawn);

            Find.LetterStack.ReceiveLetter(label, text, letterDef, lookTargets);
        }

        /// <summary>
        /// Get the text for symptom onset notification.
        /// </summary>
        private static string GetSymptomOnsetText(Pawn pawn)
        {
            // Base text
            string baseText = "DP_PathogenicFluDetectedDesc".Translate(pawn.Named("PAWN")).ToString();

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

            string label = "DP_PathogenicFluRecoveredLabel".Translate();
            string text = "DP_PathogenicFluRecoveredDesc".Translate(pawn.Named("PAWN")).ToString();

            LetterDef letterDef = LetterDefOf.PositiveEvent;
            LookTargets lookTargets = new LookTargets(pawn);

            Find.LetterStack.ReceiveLetter(label, text, letterDef, lookTargets);
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