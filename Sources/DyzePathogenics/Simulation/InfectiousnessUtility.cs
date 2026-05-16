using RimWorld;
using Verse;
using Dyze.RimWorld.Pathogenics;

namespace Dyze.RimWorld.Pathogenics.Simulation
{
    /// <summary>
    /// Utility for calculating disease infectiousness based on infection stage.
    ///
    /// Infectiousness follows the disease progression:
    /// - Exposed/Incubating: 0.00 (not yet infectious)
    /// - PreSymptomaticInfectious: 0.50 (contagious before symptoms appear)
    /// - Symptomatic: 1.00 (fully contagious with visible symptoms)
    /// - Recovering: 0.25 (reduced contagiousness during recovery)
    /// - Recovered: 0.00 (no longer infectious)
    /// </summary>
    public static class InfectiousnessUtility
    {
        public static float GetInfectiousness(Pawn pawn)
        {
            if (pawn == null)
            {
                return 0f;
            }

            PawnDiseaseState diseaseState = PathogenicsGameComponent.Instance?.TryGetDiseaseState(pawn);
            if (diseaseState == null)
            {
                return 0f;
            }

            return GetInfectiousnessForStage(diseaseState.Stage);
        }

        public static float GetInfectiousnessForStage(SimulatedDiseaseStage stage)
        {
            return stage switch
            {
                SimulatedDiseaseStage.None => 0f,
                SimulatedDiseaseStage.Exposed => 0f,
                SimulatedDiseaseStage.Incubating => 0f,
                SimulatedDiseaseStage.PreSymptomaticInfectious => 0.5f,
                SimulatedDiseaseStage.Symptomatic => 1.0f,
                SimulatedDiseaseStage.Recovering => 0.25f,
                SimulatedDiseaseStage.Recovered => 0f,
                _ => 0f
            };
        }

        public static bool IsInfectious(Pawn pawn)
        {
            return GetInfectiousness(pawn) > 0f;
        }

        public static float GetInfectiousnessForPawn(Pawn pawn)
        {
            return GetInfectiousness(pawn);
        }

        public static string GetInfectiousnessLabel(float infectiousness)
        {
            return infectiousness switch
            {
                0f => "Not infectious",
                > 0f and < 0.5f => "Low infectiousness",
                0.5f => "Moderate infectiousness",
                > 0.5f and < 1.0f => "High infectiousness",
                1.0f => "Fully infectious",
                _ => "Unknown"
            };
        }

        public static string GetDebugInfo(Pawn pawn)
        {
            if (pawn == null)
            {
                return "Pawn is null";
            }

            PawnDiseaseState diseaseState = PathogenicsGameComponent.Instance?.TryGetDiseaseState(pawn);
            if (diseaseState == null)
            {
                return $"Pawn {pawn.LabelShort}: No disease state";
            }

            float infectiousness = GetInfectiousnessForStage(diseaseState.Stage);
            string label = GetInfectiousnessLabel(infectiousness);
            return $"Pawn {pawn.LabelShort}: Stage={diseaseState.GetStageLabel()}, Infectiousness={infectiousness:F2} ({label})";
        }
    }
}
