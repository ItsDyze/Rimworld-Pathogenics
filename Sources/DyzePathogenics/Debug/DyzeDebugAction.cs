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
            "Expose selected pawn (hidden infection)",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap
        )]
        public static void ExposePawnToPathogenicFlu(Pawn pawn)
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

            var mapComponent = pawn.Map?.GetComponent<PathogenicsMapComponent>();
            if (mapComponent == null)
            {
                Messages.Message(
                    "Could not get map component.",
                    MessageTypeDefOf.RejectInput,
                    false
                );
                return;
            }

            int currentTick = Find.TickManager.TicksGame;
            PawnDiseaseState diseaseState = mapComponent.GetOrCreateDiseaseState(pawn);
            diseaseState.Stage = SimulatedDiseaseStage.Exposed;
            diseaseState.ExposedTick = currentTick;
            diseaseState.InfectiousStartTick = -1;
            diseaseState.SymptomOnsetTick = -1;
            diseaseState.RecoveringTick = -1;
            diseaseState.RecoveredTick = -1;

            Messages.Message(
                $"{pawn.LabelShort} now has a hidden exposed state.",
                MessageTypeDefOf.PositiveEvent,
                false
            );
        }

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

                EnsureSymptomaticDiseaseState(pawn);

                Messages.Message(
                    $"{pawn.LabelShort} already has {pathogenicFlu.label}. Severity refreshed and disease state synchronized.",
                    MessageTypeDefOf.NeutralEvent,
                    false
                );
                return;
            }

            Hediff hediff = HediffMaker.MakeHediff(pathogenicFlu, pawn);
            hediff.Severity = DefaultPathogenicFluSeverity;
            pawn.health.AddHediff(hediff);

            EnsureSymptomaticDiseaseState(pawn);

            Messages.Message(
                $"Applied {pathogenicFlu.label} to {pawn.LabelShort} and synchronized hidden disease state.",
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

            var mapComponent = pawn.Map?.GetComponent<PathogenicsMapComponent>();
            mapComponent?.ClearDiseaseState(pawn);

            Messages.Message(
                $"Removed {pathogenicFlu.label} from {pawn.LabelShort} and cleared hidden disease state.",
                MessageTypeDefOf.PositiveEvent,
                false
            );
        }

        [DebugAction(
            "Dyze Pathogenics",
            "Clear hidden disease state for selected pawn",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap
        )]
        public static void ClearHiddenDiseaseState(Pawn pawn)
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

            var mapComponent = pawn.Map?.GetComponent<PathogenicsMapComponent>();
            if (mapComponent == null)
            {
                Messages.Message(
                    "Could not get map component.",
                    MessageTypeDefOf.RejectInput,
                    false
                );
                return;
            }

            mapComponent.ClearDiseaseState(pawn);

            Messages.Message(
                $"Cleared hidden disease state for {pawn.LabelShort}.",
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

            PawnDiseaseState diseaseState = mapComponent.GetDiseaseState(pawn);
            if (diseaseState == null && PawnHasVisiblePathogenicFlu(pawn))
            {
                diseaseState = EnsureSymptomaticDiseaseState(pawn);
            }

            if (diseaseState == null || !diseaseState.HasDiseaseState())
            {
                DyzeLog.DevAction($"Pawn {pawn.LabelShort} has no active disease state.");
                return;
            }

            DyzeLog.DevAction(diseaseState.GetDebugInfo(pawn));
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

        private static PawnDiseaseState EnsureSymptomaticDiseaseState(Pawn pawn)
        {
            var mapComponent = pawn.Map?.GetComponent<PathogenicsMapComponent>();
            if (mapComponent == null)
            {
                return null;
            }

            int currentTick = Find.TickManager.TicksGame;
            PawnDiseaseState diseaseState = mapComponent.GetOrCreateDiseaseState(pawn);

            if (diseaseState.ExposedTick < 0)
            {
                diseaseState.ExposedTick = currentTick;
            }

            if (diseaseState.InfectiousStartTick < 0)
            {
                diseaseState.InfectiousStartTick = currentTick;
            }

            diseaseState.Stage = SimulatedDiseaseStage.Symptomatic;
            diseaseState.SymptomOnsetTick = currentTick;

            return diseaseState;
        }

        private static bool PawnHasVisiblePathogenicFlu(Pawn pawn)
        {
            HediffDef pathogenicFlu = GetPathogenicFluDef();
            return pathogenicFlu != null && pawn.health?.hediffSet?.GetFirstHediffOfDef(pathogenicFlu) != null;
        }

        private static HediffDef GetPathogenicFluDef()
        {
            return DefDatabase<HediffDef>.GetNamedSilentFail(PathogenicFluDefName);
        }
    }
}
