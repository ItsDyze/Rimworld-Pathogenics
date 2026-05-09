using System.Linq;
using LudeonTK;
using RimWorld;
using Verse;

namespace Dyze.RimWorld.CoreExample
{
    public static class DyzeDebugActions
    {
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
    }
}