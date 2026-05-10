using RimWorld;
using Verse;

namespace Dyze.RimWorld.Pathogenics
{
    /// <summary>
    /// Represents the hidden simulation state of a disease before it becomes visible.
    /// This is separate from the HediffDef which represents visible disease symptoms.
    /// </summary>
    public enum SimulatedDiseaseStage
    {
        None = 0,
        Exposed = 1,
        Incubating = 2,
        PreSymptomaticInfectious = 3,
        Symptomatic = 4,
        Recovering = 5,
        Recovered = 6
    }

    /// <summary>
    /// Tracks hidden disease state for a single pawn.
    /// This state controls when the visible HediffDef should be applied.
    /// For v0.2, this is specific to DP_PathogenicFlu.
    /// </summary>
    public class PawnDiseaseState : IExposable
    {
        public int PawnId;
        public SimulatedDiseaseStage Stage = SimulatedDiseaseStage.None;
        public int ExposedTick = -1;
        public int InfectiousStartTick = -1;
        public int SymptomOnsetTick = -1;
        public int RecoveringTick = -1;
        public int RecoveredTick = -1;

        // Track which map this state belongs to (for cleanup)
        public int MapId = -1;

        public PawnDiseaseState()
        {
        }

        public PawnDiseaseState(int pawnId, int mapId)
        {
            PawnId = pawnId;
            MapId = mapId;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref PawnId, "pawnId");
            Scribe_Values.Look(ref Stage, "stage", SimulatedDiseaseStage.None);
            Scribe_Values.Look(ref ExposedTick, "exposedTick", -1);
            Scribe_Values.Look(ref InfectiousStartTick, "infectiousStartTick", -1);
            Scribe_Values.Look(ref SymptomOnsetTick, "symptomOnsetTick", -1);
            Scribe_Values.Look(ref RecoveringTick, "recoveringTick", -1);
            Scribe_Values.Look(ref RecoveredTick, "recoveredTick", -1);
            Scribe_Values.Look(ref MapId, "mapId", -1);
        }

        public bool HasDiseaseState()
        {
            return Stage != SimulatedDiseaseStage.None && Stage != SimulatedDiseaseStage.Recovered;
        }

        public bool IsInfectious()
        {
            return Stage == SimulatedDiseaseStage.PreSymptomaticInfectious || Stage == SimulatedDiseaseStage.Symptomatic;
        }

        public void Clear()
        {
            Stage = SimulatedDiseaseStage.None;
            ExposedTick = -1;
            InfectiousStartTick = -1;
            SymptomOnsetTick = -1;
            RecoveringTick = -1;
            RecoveredTick = -1;
        }

        public string GetStageLabel()
        {
            return Stage switch
            {
                SimulatedDiseaseStage.None => "Healthy",
                SimulatedDiseaseStage.Exposed => "Exposed",
                SimulatedDiseaseStage.Incubating => "Incubating",
                SimulatedDiseaseStage.PreSymptomaticInfectious => "Pre-symptomatic infectious",
                SimulatedDiseaseStage.Symptomatic => "Symptomatic (visible)",
                SimulatedDiseaseStage.Recovering => "Recovering",
                SimulatedDiseaseStage.Recovered => "Recovered",
                _ => "Unknown"
            };
        }

        public string GetDebugInfo(Pawn pawn)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            sb.AppendLine($"Pawn: {pawn?.LabelShort ?? "Unknown (ID: " + PawnId + ")"}");
            sb.AppendLine($"Stage: {GetStageLabel()}");
            sb.AppendLine($"Exposed tick: {(ExposedTick > 0 ? ExposedTick.ToString() : "N/A")}");
            sb.AppendLine($"Infectious start: {(InfectiousStartTick > 0 ? InfectiousStartTick.ToString() : "N/A")}");
            sb.AppendLine($"Symptom onset: {(SymptomOnsetTick > 0 ? SymptomOnsetTick.ToString() : "N/A")}");
            sb.AppendLine($"Recovering: {(RecoveringTick > 0 ? RecoveringTick.ToString() : "N/A")}");
            sb.AppendLine($"Recovered: {(RecoveredTick > 0 ? RecoveredTick.ToString() : "N/A")}");

            int currentTick = Find.TickManager.TicksGame;
            if (ExposedTick > 0 && currentTick > ExposedTick)
            {
                sb.AppendLine($"Ticks since exposed: {currentTick - ExposedTick}");
            }
            if (InfectiousStartTick > 0 && currentTick > InfectiousStartTick)
            {
                sb.AppendLine($"Ticks infectious: {currentTick - InfectiousStartTick}");
            }

            return sb.ToString();
        }
    }
}