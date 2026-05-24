using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Dyze.RimWorld.Pathogenics;

namespace Dyze.RimWorld.Pathogenics.Simulation
{
    /// <summary>
    /// Utility for calculating disease infectiousness based on infection stage.
    ///
    /// Infectiousness follows each disease progression independently:
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
            return GetInfectiousnessForPawn(pawn);
        }

        public static float GetInfectiousness(Pawn pawn, PawnDiseaseState diseaseState)
        {
            if (pawn == null || diseaseState == null)
            {
                return 0f;
            }

            return GetInfectiousnessForStage(diseaseState.Stage);
        }

        public static List<PawnDiseaseState> GetInfectiousDiseaseStates(Pawn pawn)
        {
            return PathogenicsGameComponent.Instance?.GetDiseaseStates(pawn)
                .Where(state => state != null && GetInfectiousnessForStage(state.Stage) > 0f)
                .ToList() ?? new List<PawnDiseaseState>();
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
            return GetInfectiousDiseaseStates(pawn).Count > 0;
        }

        public static float GetInfectiousnessForPawn(Pawn pawn)
        {
            if (pawn == null)
            {
                return 0f;
            }

            return PathogenicsGameComponent.Instance?.GetDiseaseStates(pawn)
                .Select(state => GetInfectiousnessForStage(state.Stage))
                .DefaultIfEmpty(0f)
                .Max() ?? 0f;
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

            List<PawnDiseaseState> states = PathogenicsGameComponent.Instance?.GetDiseaseStates(pawn) ?? new List<PawnDiseaseState>();
            if (states.Count == 0)
            {
                return $"Pawn {pawn.LabelShort}: No disease state";
            }

            return $"Pawn {pawn.LabelShort}: " + string.Join("; ", states.Select(state =>
            {
                float infectiousness = GetInfectiousnessForStage(state.Stage);
                string label = GetInfectiousnessLabel(infectiousness);
                return $"{state.DiseaseDefName} Stage={state.GetStageLabel()}, Infectiousness={infectiousness:F2} ({label})";
            }).ToArray());
        }
    }
}
