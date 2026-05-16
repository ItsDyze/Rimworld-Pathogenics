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

        // Track whether this disease state belongs to a player-controlled colonist and should survive off-map travel.
        public bool PreserveAcrossMaps = false;

        // Track whether the visible hediff has been applied
        public bool VisibleHediffApplied = false;

        // ===== EXPOSURE ACCUMULATION (v0.2.1) =====
        // Exposure accumulates from transmission events (0.0 to 1.0)
        // When exposure >= threshold, incubation begins
        public float Exposure = 0f;

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
            Scribe_Values.Look(ref PreserveAcrossMaps, "preserveAcrossMaps", false);

            // Visible hediff tracking
            Scribe_Values.Look(ref VisibleHediffApplied, "visibleHediffApplied", false);

            // Exposure accumulation (v0.2.1)
            Scribe_Values.Look(ref Exposure, "exposure", 0f);
        }

        public bool HasDiseaseState()
        {
            return Stage != SimulatedDiseaseStage.None && Stage != SimulatedDiseaseStage.Recovered;
        }

        public bool IsInfectious()
        {
            return Stage == SimulatedDiseaseStage.PreSymptomaticInfectious ||
                   Stage == SimulatedDiseaseStage.Symptomatic ||
                   Stage == SimulatedDiseaseStage.Recovering;
        }

        public void Clear()
        {
            Stage = SimulatedDiseaseStage.None;
            ExposedTick = -1;
            InfectiousStartTick = -1;
            SymptomOnsetTick = -1;
            RecoveringTick = -1;
            RecoveredTick = -1;
            VisibleHediffApplied = false;
            Exposure = 0f;
        }

        /// <summary>
        /// Add exposure amount to this pawn's disease state.
        /// Returns true if exposure threshold was crossed (incubation begins).
        /// </summary>
        public bool AddExposure(float amount)
        {
            Exposure += amount;

            if (Exposure < 0f)
            {
                Exposure = 0f;
            }
            else if (Exposure > 1.0f)
            {
                Exposure = 1.0f;
            }

            return Exposure >= 1.0f;
        }

        /// <summary>
        /// Clear exposure accumulation for this pawn.
        /// </summary>
        public void ClearExposure()
        {
            Exposure = 0f;
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

            // Exposure accumulation (v0.2.1)
            sb.AppendLine($"Exposure: {Exposure:F2} / 1.00");

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