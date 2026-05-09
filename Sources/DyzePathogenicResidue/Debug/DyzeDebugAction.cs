using System.Linq;
using LudeonTK;
using RimWorld;
using Verse;

namespace Dyze.RimWorld.PathogenicResidue
{
    public static class DyzeDebugActions
    {
        private const string PathogenicFluDefName = "PR_PathogenicFlu";
        private const float DefaultPathogenicFluSeverity = 0.15f;

        [DebugAction(
            "Dyze Pathogenic Residue",
            "Spawn residue on clicked cell",
            actionType = DebugActionType.ToolMap,
            allowedGameStates = AllowedGameStates.PlayingOnMap
        )]
        public static void SpawnResidueOnCell()
        {
            Map map = Find.CurrentMap;
            IntVec3 cell = UI.MouseCell();

            bool placed = DyzePathogenicResidueUtility.TryPlaceResidueAt(
                cell,
                map,
                allowDuplicate: true
            );

            if (placed)
            {
                Messages.Message(
                    "Spawned pathogenic residue.",
                    MessageTypeDefOf.NeutralEvent,
                    false
                );
            }
            else
            {
                Messages.Message(
                    "Could not spawn pathogenic residue on that cell.",
                    MessageTypeDefOf.RejectInput,
                    false
                );
            }
        }

        [DebugAction(
            "Dyze Pathogenic Residue",
            "Inspect clicked pawn residue status",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap
        )]
        public static void InspectPawnResidueStatus(Pawn pawn)
        {
            if (pawn == null)
            {
                DyzeLog.DevAction("No pawn selected.");
                return;
            }

            bool qualifies = DyzePathogenicResidueUtility.TryGetResidueSpawnChance(
                pawn,
                out float spawnChance
            );

            System.Text.StringBuilder builder = new System.Text.StringBuilder();

            builder.AppendLine($"Pawn: {pawn.LabelShort}");
            builder.AppendLine($"Qualifies: {qualifies}");
            builder.AppendLine($"Spawn chance: {spawnChance:P1}");

            if (pawn.health?.hediffSet?.hediffs != null)
            {
                builder.AppendLine("Hediffs:");

                foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
                {
                    bool hasExtension = DyzePathogenicResidueUtility.TryGetResidueExtension(
                        hediff,
                        out DyzePathogenicResidueHediffExtension extension
                    );

                    if (!hasExtension)
                    {
                        builder.AppendLine($"- {hediff.def.defName}: no residue extension");
                        continue;
                    }

                    builder.AppendLine(
                        $"- {hediff.def.defName}: severity={hediff.Severity:0.###}, minSeverity={extension.minSeverity:0.###}, factor={extension.spawnChanceFactor:0.###}, enabled={extension.enabled}"
                    );
                }
            }

            DyzeLog.DevAction(builder.ToString());
        }

        [DebugAction(
            "Dyze Pathogenic Residue",
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
            "Dyze Pathogenic Residue",
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
            "Dyze Pathogenic Residue",
            "Log residue-capable HediffDefs",
            actionType = DebugActionType.Action,
            allowedGameStates = AllowedGameStates.Playing
        )]
        public static void LogResidueCapableHediffDefs()
        {
            var defs = DefDatabase<HediffDef>
                .AllDefs
                .Where(def => def.GetModExtension<DyzePathogenicResidueHediffExtension>() != null)
                .OrderBy(def => def.defName)
                .ToList();

            System.Text.StringBuilder builder = new System.Text.StringBuilder();

            builder.AppendLine($"Residue-capable HediffDefs: {defs.Count}");

            foreach (HediffDef def in defs)
            {
                DyzePathogenicResidueHediffExtension extension =
                    def.GetModExtension<DyzePathogenicResidueHediffExtension>();

                builder.AppendLine(
                    $"- {def.defName}: enabled={extension.enabled}, factor={extension.spawnChanceFactor}, minSeverity={extension.minSeverity}"
                );
            }

            DyzeLog.DevAction(builder.ToString());
        }

        [DebugAction(
            "Dyze Pathogenic Residue",
            "Log movement hook status",
            actionType = DebugActionType.Action,
            allowedGameStates = AllowedGameStates.Playing
        )]
        public static void LogMovementHookStatus()
        {
            DyzePathogenicResidueSettings settings = DyzePathogenicResidueMod.Settings;

            string text =
                $"Movement hook enabled in settings: {settings?.UseMovementHook}\n" +
                $"Residue enabled: {settings?.Enabled}\n" +
                $"Harmony patch target: Verse.AI.Pawn_PathFollower.TryEnterNextPathCell";

            Find.WindowStack.Add(new Dialog_MessageBox(text));
        }

        private static HediffDef GetPathogenicFluDef()
        {
            return DefDatabase<HediffDef>.GetNamedSilentFail(PathogenicFluDefName);
        }
    }
}
