using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Dyze.RimWorld.Pathogenics.Simulation;

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
        // Timeline: infectious start +0.5d, symptom onset +1.5d, infectious end +5d
        private const int PreSymptomaticInfectiousDurationTicks = 30000;  // ~0.5 days (from incubation start)
        private const int SymptomaticDurationTicks = 60000;            // ~1.0 days (visible disease duration)
        private const int RecoveringDurationTicks = 240000;            // ~4.0 days (recovery after symptoms end)
        // Total: 0.5 + 1.0 + 4.0 = 5.5 days from incubation start to recovery

        // ===== CONFIGURATION: Exposure accumulation (v0.2.1) =====
        // Exposure decays over time so brief contact fades away unless reinforced.
        // v0.2.2: Reduced from 1.5 to 0.5 per day for more forgiving gameplay.
        // 0.5 per day = one 0.25 debug-step fades in about 12 in-game hours.
        private const float ExposureDecayPerDay = 0.5f;
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
                Pawn pawn = pawns[i];
                if (pawn == null || !pawn.Spawned || pawn.Dead)
                {
                    continue;
                }

                currentPawnIds.Add(pawn.thingIDNumber);
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

            // Skip if disease simulation is disabled
            if (DyzePathogenicsMod.Settings?.Enabled != true)
                return;

            if (Find.TickManager.TicksGame % ExposureDecayIntervalTicks == 0)
            {
                CleanStaleDiseaseStates();
            }

            // v0.2: The active core uses hidden disease state tracking.
            // v0.2.1: Process exposure accumulation and decay
            ProcessExposureDecay();

            // v0.2.1: Process respiratory proximity transmission
            RespiratoryTransmissionWorker.ProcessTransmission(map);

            // v0.3: Process outsider importation
            DiseaseImportationWorker.ProcessOutsiderSpawns(map);

            // v0.3: Process disease stage transitions (incubation → symptom onset → recovery)
            ProcessStageTransitions();
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
        /// Timeline: incubating starts now → infectious at +0.5d → symptoms at +1.5d → recovery at +5d
        /// </summary>
        private void StartIncubation(PawnDiseaseState state, int currentTick)
        {
            state.Stage = SimulatedDiseaseStage.Incubating;
            state.ExposedTick = currentTick;
            state.ClearExposure();
            state.VisibleHediffApplied = false;
            // Pre-symptomatic infectious starts at +0.5 days (30000 ticks)
            state.InfectiousStartTick = currentTick + PreSymptomaticInfectiousDurationTicks;
            // Symptom onset at +1.5 days (30000 + 60000 = 90000 ticks)
            state.SymptomOnsetTick = currentTick + PreSymptomaticInfectiousDurationTicks + SymptomaticDurationTicks;
            state.RecoveringTick = -1;
            state.RecoveredTick = -1;

            DyzeLog.Message($"Pawn (ID: {state.PawnId}) has accumulated enough exposure and is now incubating.");
        }

        /// <summary>
        /// Process stage transitions based on scheduled ticks.
        /// Called every tick to check if a pawn should advance to the next disease stage.
        /// </summary>
        public void ProcessStageTransitions()
        {
            int currentTick = Find.TickManager.TicksGame;
            List<int> pawnIdsToRemove = null;
            List<Pawn> pawnsToNotify = null;

            foreach (var kvp in pawnDiseaseStates)
            {
                PawnDiseaseState state = kvp.Value;

                // Skip if no active disease state
                if (!state.HasDiseaseState())
                {
                    continue;
                }

                // Find the pawn
                Pawn pawn = FindPawnById(state.PawnId);
                if (pawn == null || pawn.Dead || !pawn.Spawned)
                {
                    pawnIdsToRemove ??= new List<int>();
                    pawnIdsToRemove.Add(kvp.Key);
                    continue;
                }

                // Process stage transitions based on current tick
                switch (state.Stage)
                {
                    case SimulatedDiseaseStage.Incubating:
                        // Transition to pre-symptomatic infectious when infectious start tick is reached
                        if (state.InfectiousStartTick > 0 && currentTick >= state.InfectiousStartTick)
                        {
                            state.Stage = SimulatedDiseaseStage.PreSymptomaticInfectious;
                            DyzeLog.Message($"Pawn {pawn.LabelShort} is now pre-symptomatic infectious.");
                        }
                        break;

                    case SimulatedDiseaseStage.PreSymptomaticInfectious:
                        // Transition to symptomatic (apply visible hediff) when symptom onset tick is reached
                        if (state.SymptomOnsetTick > 0 && currentTick >= state.SymptomOnsetTick)
                        {
                            state.Stage = SimulatedDiseaseStage.Symptomatic;
                            state.RecoveringTick = currentTick + SymptomaticDurationTicks;
                            ApplyVisibleHediff(pawn);

                            // Track for notification if this is a colonist
                            if (pawn.IsColonist)
                            {
                                pawnsToNotify ??= new List<Pawn>();
                                pawnsToNotify.Add(pawn);
                            }
                            DyzeLog.Message($"Pawn {pawn.LabelShort} has developed visible symptoms!");
                        }
                        break;

                    case SimulatedDiseaseStage.Symptomatic:
                        // Transition to recovering when recovering tick is reached
                        if (state.RecoveringTick > 0 && currentTick >= state.RecoveringTick)
                        {
                            state.Stage = SimulatedDiseaseStage.Recovering;
                            state.RecoveredTick = currentTick + RecoveringDurationTicks;
                            DyzeLog.Message($"Pawn {pawn.LabelShort} is now recovering.");
                        }
                        break;

                    case SimulatedDiseaseStage.Recovering:
                        // Transition to recovered when recovered tick is reached
                        if (state.RecoveredTick > 0 && currentTick >= state.RecoveredTick)
                        {
                            state.Stage = SimulatedDiseaseStage.Recovered;
                            DyzeLog.Message($"Pawn {pawn.LabelShort} has recovered from the disease.");

                            // Clean up state for recovered pawns
                            pawnIdsToRemove ??= new List<int>();
                            pawnIdsToRemove.Add(kvp.Key);
                        }
                        break;
                }
            }

            // Send notifications for colonist symptom onset
            if (pawnsToNotify != null)
            {
                foreach (Pawn pawn in pawnsToNotify)
                {
                    SendSymptomOnsetNotification(pawn);
                }
            }

            // Clean up recovered pawns
            if (pawnIdsToRemove != null)
            {
                foreach (int pawnId in pawnIdsToRemove)
                {
                    pawnDiseaseStates.Remove(pawnId);
                }
            }
        }

        /// <summary>
        /// Find a pawn by ID in the current map.
        /// </summary>
        private Pawn FindPawnById(int pawnId)
        {
            var pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                if (pawns[i].thingIDNumber == pawnId)
                {
                    return pawns[i];
                }
            }
            return null;
        }

        /// <summary>
        /// Apply the visible disease hediff to a pawn.
        /// </summary>
        private void ApplyVisibleHediff(Pawn pawn)
        {
            if (pawn == null || pawn.health == null)
            {
                return;
            }

            // Check if pawn already has the hediff
            HediffDef hediffDef = DefDatabase<HediffDef>.GetNamed("DP_PathogenicFlu");
            Hediff existingHediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
            if (existingHediff != null)
            {
                // Already has the hediff, just update severity
                existingHediff.Severity = 0.001f;
                return;
            }

            // Add the new hediff
            Hediff newHediff = HediffMaker.MakeHediff(hediffDef, pawn);
            newHediff.Severity = 0.001f;
            pawn.health.AddHediff(newHediff);

            // Mark visible hediff as applied
            PawnDiseaseState state = GetDiseaseState(pawn);
            if (state != null)
            {
                state.VisibleHediffApplied = true;
            }
        }

        /// <summary>
        /// Send a notification letter when a colonist develops symptoms.
        /// </summary>
        private void SendSymptomOnsetNotification(Pawn pawn)
        {
            if (pawn == null || !pawn.IsColonist)
            {
                return;
            }

            string label = "PathogenicFluDetected".Translate();
            string text = "PathogenicFluDetectedDesc".Translate(pawn.Named("PAWN")).ToString();

            LetterDef letterDef = LetterDefOf.ThreatSmall;
            LookTargets lookTargets = new LookTargets(pawn);

            Find.LetterStack.ReceiveLetter(label, text, letterDef, lookTargets);
        }

        /// <summary>
        /// Add exposure to a pawn's disease state.
        /// </summary>
        public void AddExposureToPawn(Pawn pawn, float amount)
        {
            if (pawn == null)
                return;

            // Check AffectColonistsOnly setting
            if (DyzePathogenicsMod.Settings?.AffectColonistsOnly == true && !pawn.IsColonist)
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

            if (thresholdCrossed && state.Stage == SimulatedDiseaseStage.Exposed)
            {
                // Immediate transition to incubating
                StartIncubation(state, currentTick);
            }
        }
    }
}