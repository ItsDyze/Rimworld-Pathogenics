using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Dyze.RimWorld.Pathogenics
{
    /// <summary>
    /// Map component for Pathogenics v0.2+.
    /// 
    /// ACTIVE: Hidden disease state tracking for standalone respiratory disease.
    /// The residue system has been completely removed.
    /// </summary>
    public class PathogenicsMapComponent : MapComponent
    {
        // ===== ACTIVE: Hidden disease state tracking for v0.2 standalone disease =====
        private Dictionary<int, PawnDiseaseState> pawnDiseaseStates = new Dictionary<int, PawnDiseaseState>();

        // Public accessor for debug actions
        public Dictionary<int, PawnDiseaseState> PawnDiseaseStates => pawnDiseaseStates;

        // ===== CONFIGURATION: Disease progression timing (v0.2) =====
        // Configuration for hidden disease progression (could move to settings later)
        private const int IncubationDurationTicks = 60000; // ~17 days at default speed
        private const int PreSymptomaticInfectiousDurationTicks = 12000; // ~3.3 days
        private const int SymptomaticDurationTicks = 60000; // ~17 days
        private const int RecoveringDurationTicks = 24000; // ~6.7 days

        // ===== CONFIGURATION: Exposure accumulation (v0.2.1) =====
        // Exposure decays over time so brief contact fades away unless reinforced.
        // 1.5 per day = one 0.25 debug-step fades in about 4 in-game hours.
        private const float ExposureDecayPerDay = 1.5f;
        // Ticks per day at default game speed
        private const int TicksPerDay = 60_000;
        // Process decay in coarse intervals instead of every tick.
        private const int ExposureDecayIntervalTicks = 250;

        public PathogenicsMapComponent(Map map) : base(map)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();

            // Hidden disease state persistence
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                List<PawnDiseaseState> statesToSave = pawnDiseaseStates.Values.ToList();
                Scribe_Collections.Look(
                    ref statesToSave,
                    "pawnDiseaseStates",
                    LookMode.Deep
                );
            }
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                List<PawnDiseaseState> loadedStates = null;
                Scribe_Collections.Look(
                    ref loadedStates,
                    "pawnDiseaseStates",
                    LookMode.Deep
                );

                if (loadedStates != null)
                {
                    pawnDiseaseStates.Clear();
                    foreach (var state in loadedStates)
                    {
                        if (state != null && state.PawnId > 0)
                        {
                            pawnDiseaseStates[state.PawnId] = state;
                        }
                    }
                }
            }

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                pawnDiseaseStates ??= new Dictionary<int, PawnDiseaseState>();

                CleanStaleDiseaseStates();
            }
        }

        private void CleanStaleDiseaseStates()
        {
            HashSet<int> currentPawnIds = new HashSet<int>();

            var pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                currentPawnIds.Add(pawns[i].thingIDNumber);
            }

            List<int> idsToRemove = null;
            foreach (int pawnId in pawnDiseaseStates.Keys)
            {
                if (!currentPawnIds.Contains(pawnId))
                {
                    idsToRemove ??= new List<int>();
                    idsToRemove.Add(pawnId);
                }
            }

            if (idsToRemove != null)
            {
                for (int i = 0; i < idsToRemove.Count; i++)
                {
                    pawnDiseaseStates.Remove(idsToRemove[i]);
                }
            }
        }

        /// <summary>
        /// Get disease state for a pawn, creating one if it doesn't exist.
        /// </summary>
        public PawnDiseaseState GetOrCreateDiseaseState(Pawn pawn)
        {
            if (pawn == null)
                return null;

            int pawnId = pawn.thingIDNumber;

            if (!pawnDiseaseStates.TryGetValue(pawnId, out PawnDiseaseState state))
            {
                state = new PawnDiseaseState(pawnId, map?.uniqueID ?? 0);
                pawnDiseaseStates[pawnId] = state;
            }

            return state;
        }

        /// <summary>
        /// Get disease state for a pawn if it exists.
        /// </summary>
        public PawnDiseaseState GetDiseaseState(Pawn pawn)
        {
            if (pawn == null)
                return null;

            pawnDiseaseStates.TryGetValue(pawn.thingIDNumber, out PawnDiseaseState state);
            return state;
        }

        /// <summary>
        /// Clear hidden disease state for a specific pawn.
        /// </summary>
        public void ClearDiseaseState(Pawn pawn)
        {
            if (pawn == null)
                return;

            pawnDiseaseStates.Remove(pawn.thingIDNumber);
        }

        /// <summary>
        /// Clear all hidden disease states on this map.
        /// </summary>
        public void ClearAllDiseaseStates()
        {
            pawnDiseaseStates.Clear();
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            // v0.2: The active core uses hidden disease state tracking.
            // v0.2.1: Process exposure accumulation and decay
            ProcessExposureDecay();
        }

        /// <summary>
        /// Process exposure decay for all pawns with disease state.
        /// Called every tick for timely decay.
        /// </summary>
        private void ProcessExposureDecay()
        {
            int currentTick = Find.TickManager.TicksGame;
            if (currentTick % ExposureDecayIntervalTicks != 0)
            {
                return;
            }

            float decayPerInterval = ExposureDecayPerDay * ExposureDecayIntervalTicks / TicksPerDay;
            List<int> pawnIdsToClear = null;

            foreach (var kvp in pawnDiseaseStates)
            {
                PawnDiseaseState state = kvp.Value;
                if (state.Stage != SimulatedDiseaseStage.Exposed || state.Exposure <= 0f)
                {
                    continue;
                }

                state.AddExposure(-decayPerInterval);

                if (state.Exposure <= 0f)
                {
                    pawnIdsToClear ??= new List<int>();
                    pawnIdsToClear.Add(kvp.Key);
                }
            }

            if (pawnIdsToClear == null)
            {
                return;
            }

            for (int i = 0; i < pawnIdsToClear.Count; i++)
            {
                pawnDiseaseStates.Remove(pawnIdsToClear[i]);
            }
        }

        /// <summary>
        /// Transition a pawn from Exposed (accumulated exposure) to Incubating stage.
        /// </summary>
        private void StartIncubation(PawnDiseaseState state, int currentTick)
        {
            state.Stage = SimulatedDiseaseStage.Incubating;
            state.InfectiousStartTick = currentTick + IncubationDurationTicks;
            state.SymptomOnsetTick = state.InfectiousStartTick + PreSymptomaticInfectiousDurationTicks;

            DyzeLog.Message($"Pawn (ID: {state.PawnId}) has accumulated enough exposure and is now incubating.");
        }

        /// <summary>
        /// Add exposure to a pawn's disease state.
        /// </summary>
        public void AddExposureToPawn(Pawn pawn, float amount)
        {
            if (pawn == null)
                return;

            PawnDiseaseState state = GetOrCreateDiseaseState(pawn);
            int currentTick = Find.TickManager.TicksGame;

            // Initialize exposed state if needed
            if (state.Stage == SimulatedDiseaseStage.None || state.Stage == SimulatedDiseaseStage.Recovered)
            {
                state.Stage = SimulatedDiseaseStage.Exposed;
                state.ExposedTick = currentTick;
                state.ClearExposure();
            }

            // Add exposure
            bool thresholdCrossed = state.AddExposure(amount);

            if (thresholdCrossed)
            {
                // Immediate transition to incubating
                StartIncubation(state, currentTick);
            }
        }
    }
}