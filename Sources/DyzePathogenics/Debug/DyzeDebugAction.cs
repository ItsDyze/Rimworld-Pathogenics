using System.Linq;
using LudeonTK;
using RimWorld;
using Verse;

namespace Dyze.RimWorld.Pathogenics
{
    public static class DyzeDebugActions
    {
        private const string PathogenicFluDefName = "DP_PathogenicFlu";
        private const float DefaultPathogenicFluSeverity = 0.15f;

        [DebugAction(
            "Dyze Pathogenics",
            "Apply pathogenic flu to selected pawn",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap
        )]
        public static void ApplyPathogenicFluToPawn(Pawn pawn)
        {
            if (pawn == null)
            {
                Messages.Message(
                    "No pawn selected.",
                    MessageTypeDefOf.RejectInput,
                    false
                );
                return;
            }

            HediffDef pathogenicFlu = GetPathogenicFluDef();
            if (pathogenicFlu == null)
            {
                Messages.Message(
                    $"Could not find HediffDef '{PathogenicFluDefName}'.",
                    MessageTypeDefOf.RejectInput,
                    false
                );
                return;
            }

            Hediff existing = pawn.health?.hediffSet?.GetFirstHediffOfDef(pathogenicFlu);
            if (existing != null)
            {
                if (existing.Severity < DefaultPathogenicFluSeverity)
                {
                    existing.Severity = DefaultPathogenicFluSeverity;
                }

                Messages.Message(
                    $"{pawn.LabelShort} already has {pathogenicFlu.label}. Severity refreshed.",
                    MessageTypeDefOf.NeutralEvent,
                    false
                );
                return;
            }

            Hediff hediff = HediffMaker.MakeHediff(pathogenicFlu, pawn);
            hediff.Severity = DefaultPathogenicFluSeverity;
            pawn.health.AddHediff(hediff);

            Messages.Message(
                $"Applied {pathogenicFlu.label} to {pawn.LabelShort}.",
                MessageTypeDefOf.PositiveEvent,
                false
            );
        }

        [DebugAction(
            "Dyze Pathogenics",
            "Remove pathogenic flu from selected pawn",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap
        )]
        public static void RemovePathogenicFluFromPawn(Pawn pawn)
        {
            if (pawn == null)
            {
                Messages.Message(
                    "No pawn selected.",
                    MessageTypeDefOf.RejectInput,
                    false
                );
                return;
            }

            HediffDef pathogenicFlu = GetPathogenicFluDef();
            if (pathogenicFlu == null)
            {
                Messages.Message(
                    $"Could not find HediffDef '{PathogenicFluDefName}'.",
                    MessageTypeDefOf.RejectInput,
                    false
                );
                return;
            }

            var hediffs = pawn.health?.hediffSet?.hediffs;
            if (hediffs == null)
            {
                Messages.Message(
                    $"{pawn.LabelShort} has no health conditions to inspect.",
                    MessageTypeDefOf.RejectInput,
                    false
                );
                return;
            }

            int removedCount = 0;

            foreach (Hediff hediff in hediffs.Where(hediff => hediff.def == pathogenicFlu).ToList())
            {
                pawn.health.RemoveHediff(hediff);
                removedCount++;
            }

            if (removedCount <= 0)
            {
                Messages.Message(
                    $"{pawn.LabelShort} does not have {pathogenicFlu.label}.",
                    MessageTypeDefOf.NeutralEvent,
                    false
                );
                return;
            }

            Messages.Message(
                $"Removed {pathogenicFlu.label} from {pawn.LabelShort}.",
                MessageTypeDefOf.PositiveEvent,
                false
            );
        }

        [DebugAction(
            "Dyze Pathogenics",
            "Log disease state for selected pawn",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap
        )]
        public static void LogPawnDiseaseState(Pawn pawn)
        {
            if (pawn == null)
            {
                DyzeLog.DevAction("No pawn selected.");
                return;
            }

            var mapComponent = pawn.Map?.GetComponent<PathogenicsMapComponent>();
            if (mapComponent == null)
            {
                DyzeLog.DevAction($"Pawn {pawn.LabelShort} has no map component.");
                return;
            }

            var diseaseState = mapComponent.GetDiseaseState(pawn);
            if (diseaseState == null)
            {
                DyzeLog.DevAction($"Pawn {pawn.LabelShort} has no active disease state.");
                return;
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            builder.AppendLine($"Pawn: {pawn.LabelShort}");
            builder.AppendLine($"Disease State: {diseaseState}");

            DyzeLog.DevAction(builder.ToString());
        }

        [DebugAction(
            "Dyze Pathogenics",
            "Log mod status",
            actionType = DebugActionType.Action,
            allowedGameStates = AllowedGameStates.Playing
        )]
        public static void LogModStatus()
        {
            DyzePathogenicsSettings settings = DyzePathogenicsMod.Settings;

            string text =
                $"Disease Simulation: {settings?.Enabled}\n" +
                $"Affect Colonists Only: {settings?.AffectColonistsOnly}\n" +
                $"Debug Logging: {settings?.EnableDebugLogging}\n\n" +
                "v0.2 uses hidden disease state (PawnDiseaseState).";

            Find.WindowStack.Add(new Dialog_MessageBox(text));
        }

        private static HediffDef GetPathogenicFluDef()
        {
            return DefDatabase<HediffDef>.GetNamedSilentFail(PathogenicFluDefName);
        }
    }
}