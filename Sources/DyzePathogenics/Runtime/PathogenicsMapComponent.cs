using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Dyze.RimWorld.Pathogenics.Simulation;

namespace Dyze.RimWorld.Pathogenics
{
    /// <summary>
    /// Per-map runner and compatibility layer for Pathogenics.
    ///
    /// The authoritative disease registry now lives in <see cref="PathogenicsGameComponent"/>.
    /// This component keeps map-local ticking/debug helpers and imports legacy map-owned state
    /// from older saves so upgrades remain seamless.
    /// </summary>
    public class PathogenicsMapComponent : MapComponent
    {
        private const int PreSymptomaticInfectiousDurationTicks = 30000;
        private const int SymptomaticDurationTicks = 60000;
        private const int RecoveringDurationTicks = 240000;

        private const float ExposureDecayPerDay = 0.5f;
        private const int TicksPerDay = 60_000;
        private const int ExposureDecayIntervalTicks = 250;

        private List<PawnDiseaseState> legacyLoadedStates;

        public PathogenicsMapComponent(Map map) : base(map)
        {
        }

        public Dictionary<int, PawnDiseaseState> PawnDiseaseStates => PathogenicsGameComponent.Instance?.PawnDiseaseStates;

        private PathogenicsGameComponent GameComponent => PathogenicsGameComponent.Instance;

        public override void ExposeData()
        {
            base.ExposeData();

            // Compatibility import for saves created before the global registry existed.
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                Scribe_Collections.Look(ref legacyLoadedStates, "pawnDiseaseStates", LookMode.Deep);
            }
            else if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (legacyLoadedStates != null && legacyLoadedStates.Count > 0)
                {
                    GameComponent?.ImportLegacyStates(legacyLoadedStates);
                }

                legacyLoadedStates = null;
            }
        }

        private Dictionary<int, PawnDiseaseState> Registry => GameComponent?.PawnDiseaseStates;

        private IEnumerable<KeyValuePair<int, PawnDiseaseState>> DiseaseStateEntries =>
            Registry != null ? Registry : Enumerable.Empty<KeyValuePair<int, PawnDiseaseState>>();

        private void CleanStaleDiseaseStates()
        {
            if (Registry == null)
            {
                return;
            }

            HashSet<int> currentPawnIds = new HashSet<int>();

            var pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (!IsAliveRelevantMapPawn(pawn))
                {
                    continue;
                }

                currentPawnIds.Add(pawn.thingIDNumber);
            }

            foreach (Pawn pawn in GetAliveCaravanColonists())
            {
                if (pawn == null || pawn.Dead || !pawn.IsColonist)
                {
                    continue;
                }

                currentPawnIds.Add(pawn.thingIDNumber);
            }

            List<int> idsToRemove = null;
            foreach (KeyValuePair<int, PawnDiseaseState> kvp in DiseaseStateEntries)
            {
                int pawnId = kvp.Key;
                PawnDiseaseState state = kvp.Value;
                if (state != null && state.PreserveAcrossMaps)
                {
                    continue;
                }

                if (!currentPawnIds.Contains(pawnId) && ResolveTrackedPawn(pawnId) == null)
                {
                    idsToRemove ??= new List<int>();
                    idsToRemove.Add(pawnId);
                }
            }

            if (idsToRemove == null)
            {
                return;
            }

            for (int i = 0; i < idsToRemove.Count; i++)
            {
                Registry.Remove(idsToRemove[i]);
            }
        }

        public PawnDiseaseState GetOrCreateDiseaseState(Pawn pawn)
        {
            return GameComponent?.GetOrCreateDiseaseState(pawn);
        }

        public PawnDiseaseState GetDiseaseState(Pawn pawn)
        {
            if (pawn == null)
            {
                return null;
            }

            PawnDiseaseState state = GameComponent?.TryGetDiseaseState(pawn);
            if (state == null && PawnHasVisiblePathogenicFlu(pawn))
            {
                state = EnsureSymptomaticDiseaseState(pawn);
            }

            if (state != null && pawn.Map != null)
            {
                state.MapId = pawn.Map.uniqueID;
                state.PreserveAcrossMaps = pawn.IsColonist;
            }

            return state;
        }

        public List<KeyValuePair<Pawn, PawnDiseaseState>> GetActiveDiseaseStates()
        {
            List<KeyValuePair<Pawn, PawnDiseaseState>> activeStates = new List<KeyValuePair<Pawn, PawnDiseaseState>>();
            HashSet<int> addedPawnIds = new HashSet<int>();

            var pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (!IsAliveRelevantMapPawn(pawn))
                {
                    continue;
                }

                PawnDiseaseState state = GameComponent?.TryGetDiseaseState(pawn);
                if (state == null || !state.HasDiseaseState())
                {
                    continue;
                }

                activeStates.Add(new KeyValuePair<Pawn, PawnDiseaseState>(pawn, state));
                addedPawnIds.Add(pawn.thingIDNumber);
            }

            foreach (Pawn pawn in GetAliveCaravanColonists())
            {
                if (pawn == null || pawn.Dead || !pawn.IsColonist)
                {
                    continue;
                }

                if (addedPawnIds.Contains(pawn.thingIDNumber))
                {
                    continue;
                }

                PawnDiseaseState state = GameComponent?.TryGetDiseaseState(pawn);
                if (state == null || !state.HasDiseaseState())
                {
                    continue;
                }

                activeStates.Add(new KeyValuePair<Pawn, PawnDiseaseState>(pawn, state));
                addedPawnIds.Add(pawn.thingIDNumber);
            }

            return activeStates;
        }

        private static bool IsAliveRelevantMapPawn(Pawn pawn)
        {
            return pawn != null && pawn.Spawned && !pawn.Dead;
        }

        private bool PawnHasVisiblePathogenicFlu(Pawn pawn)
        {
            HediffDef pathogenicFlu = DefDatabase<HediffDef>.GetNamedSilentFail("DP_PathogenicFlu");
            return pathogenicFlu != null && pawn.health?.hediffSet?.GetFirstHediffOfDef(pathogenicFlu) != null;
        }

        private PawnDiseaseState EnsureSymptomaticDiseaseState(Pawn pawn)
        {
            if (pawn == null)
            {
                return null;
            }

            int currentTick = Find.TickManager.TicksGame;
            PawnDiseaseState diseaseState = GetOrCreateDiseaseState(pawn);
            if (diseaseState == null)
            {
                return null;
            }

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
            if (diseaseState.RecoveringTick < 0)
            {
                diseaseState.RecoveringTick = currentTick + SymptomaticDurationTicks;
            }

            diseaseState.VisibleHediffApplied = true;
            diseaseState.MapId = pawn.Map?.uniqueID ?? diseaseState.MapId;
            diseaseState.PreserveAcrossMaps = pawn.IsColonist;
            return diseaseState;
        }

        private Pawn ResolveTrackedPawn(int pawnId)
        {
            Pawn pawn = FindPawnById(pawnId);
            if (pawn != null)
            {
                return pawn;
            }

            foreach (Pawn caravanPawn in GetAliveCaravanColonists())
            {
                if (caravanPawn != null && caravanPawn.thingIDNumber == pawnId)
                {
                    return caravanPawn;
                }
            }

            Pawn worldPawn = PathogenicsPawnLookup.FindAnyPawnById(pawnId);
            return worldPawn != null && !worldPawn.Dead ? worldPawn : null;
        }

        private static IEnumerable<Pawn> GetAliveCaravanColonists()
        {
            Type pawnsFinderType = typeof(PawnsFinder);
            string[] memberNames =
            {
                "AllMapsCaravansAndTravelingTransportPods_Alive_Colonists",
                "AllCaravansAndTravelingTransportPods_Alive_Colonists",
                "AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonists",
                "AllCaravansAndTravelingTransportPods_Alive_FreeColonists",
                "AllMapsCaravansAndTravelingTransportPods_Alive",
                "AllCaravansAndTravelingTransportPods_Alive"
            };

            for (int i = 0; i < memberNames.Length; i++)
            {
                string memberName = memberNames[i];

                var property = pawnsFinderType.GetProperty(memberName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (property != null)
                {
                    if (property.GetValue(null, null) is IEnumerable<Pawn> pawns)
                    {
                        return pawns;
                    }

                    if (property.GetValue(null, null) is IEnumerable<object> pawnObjects)
                    {
                        return pawnObjects.OfType<Pawn>();
                    }
                }

                var method = pawnsFinderType.GetMethod(memberName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, null, Type.EmptyTypes, null);
                if (method != null)
                {
                    if (method.Invoke(null, null) is IEnumerable<Pawn> pawns)
                    {
                        return pawns;
                    }

                    if (method.Invoke(null, null) is IEnumerable<object> pawnObjects)
                    {
                        return pawnObjects.OfType<Pawn>();
                    }
                }
            }

            return Enumerable.Empty<Pawn>();
        }

        public void ClearDiseaseState(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            RemoveVisibleHediff(pawn);
            GameComponent?.ClearDiseaseState(pawn);
        }

        public void ClearAllDiseaseStates()
        {
            GameComponent?.ClearAllDiseaseStates();
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            if (Find.TickManager.TicksGame % ExposureDecayIntervalTicks == 0)
            {
                ResyncVisibleDiseaseStates();
                CleanStaleDiseaseStates();
            }

            // Disabling Pathogenics now pauses both hidden and visible progression.
            // Hediff progression is frozen by HediffComp_SeverityPerDay while disabled.
            if (DyzePathogenicsMod.Settings?.Enabled != true)
            {
                return;
            }

            ProcessExposureDecay();
            RespiratoryTransmissionWorker.ProcessTransmission(map);
            DiseaseImportationWorker.ProcessOutsiderSpawns(map);
            ProcessStageTransitions();
        }

        private void ResyncVisibleDiseaseStates()
        {
            var pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (!IsAliveRelevantMapPawn(pawn))
                {
                    continue;
                }

                PawnDiseaseState state = GameComponent?.TryGetDiseaseState(pawn);
                bool hasVisibleHediff = PawnHasVisiblePathogenicFlu(pawn);

                if (hasVisibleHediff && state == null)
                {
                    EnsureSymptomaticDiseaseState(pawn);
                    continue;
                }

                if (state == null)
                {
                    continue;
                }

                if (state.Stage >= SimulatedDiseaseStage.Recovering && hasVisibleHediff)
                {
                    RemoveVisibleHediff(pawn);
                    state.VisibleHediffApplied = false;
                    continue;
                }

                if (state.Stage == SimulatedDiseaseStage.Symptomatic)
                {
                    if (!hasVisibleHediff)
                    {
                        ApplyVisibleHediff(pawn);
                    }
                    else
                    {
                        state.VisibleHediffApplied = true;
                    }
                }
                else if (hasVisibleHediff)
                {
                    RemoveVisibleHediff(pawn);
                    state.VisibleHediffApplied = false;
                }
            }
        }

        private void ProcessExposureDecay()
        {
            if (Registry == null)
            {
                return;
            }

            int currentTick = Find.TickManager.TicksGame;
            if (currentTick % ExposureDecayIntervalTicks != 0)
            {
                return;
            }

            float decayPerInterval = ExposureDecayPerDay * ExposureDecayIntervalTicks / TicksPerDay;
            List<int> pawnIdsToClear = null;

            foreach (KeyValuePair<int, PawnDiseaseState> kvp in DiseaseStateEntries)
            {
                PawnDiseaseState state = kvp.Value;
                if (state == null || state.Stage != SimulatedDiseaseStage.Exposed || state.Exposure <= 0f)
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
                Registry.Remove(pawnIdsToClear[i]);
            }
        }

        private void StartIncubation(PawnDiseaseState state, int currentTick)
        {
            state.Stage = SimulatedDiseaseStage.Incubating;
            state.ExposedTick = currentTick;
            state.ClearExposure();
            state.VisibleHediffApplied = false;
            state.InfectiousStartTick = currentTick + PreSymptomaticInfectiousDurationTicks;
            state.SymptomOnsetTick = currentTick + PreSymptomaticInfectiousDurationTicks + SymptomaticDurationTicks;
            state.RecoveringTick = -1;
            state.RecoveredTick = -1;

            DyzeLog.Message($"Pawn (ID: {state.PawnId}) has accumulated enough exposure and is now incubating.");
        }

        public void ProcessStageTransitions()
        {
            if (Registry == null)
            {
                return;
            }

            int currentTick = Find.TickManager.TicksGame;
            List<int> pawnIdsToRemove = null;
            List<Pawn> pawnsToNotify = null;
            List<Pawn> pawnsToNotifyRecovered = null;

            foreach (KeyValuePair<int, PawnDiseaseState> kvp in DiseaseStateEntries)
            {
                PawnDiseaseState state = kvp.Value;
                if (state == null || !state.HasDiseaseState())
                {
                    continue;
                }

                Pawn pawn = ResolveTrackedPawn(state.PawnId);
                if (pawn == null)
                {
                    if (state.PreserveAcrossMaps)
                    {
                        continue;
                    }

                    pawnIdsToRemove ??= new List<int>();
                    pawnIdsToRemove.Add(kvp.Key);
                    continue;
                }

                state.MapId = pawn.Map?.uniqueID ?? state.MapId;
                state.PreserveAcrossMaps = pawn.IsColonist;

                if (pawn.Dead)
                {
                    pawnIdsToRemove ??= new List<int>();
                    pawnIdsToRemove.Add(kvp.Key);
                    continue;
                }

                switch (state.Stage)
                {
                    case SimulatedDiseaseStage.Incubating:
                        if (state.InfectiousStartTick > 0 && currentTick >= state.InfectiousStartTick)
                        {
                            state.Stage = SimulatedDiseaseStage.PreSymptomaticInfectious;
                            DyzeLog.Message($"Pawn {pawn.LabelShort} is now pre-symptomatic infectious.");
                        }
                        break;

                    case SimulatedDiseaseStage.PreSymptomaticInfectious:
                        if (state.SymptomOnsetTick > 0 && currentTick >= state.SymptomOnsetTick)
                        {
                            state.Stage = SimulatedDiseaseStage.Symptomatic;
                            state.RecoveringTick = currentTick + SymptomaticDurationTicks;
                            ApplyVisibleHediff(pawn);

                            if (pawn.IsColonist)
                            {
                                pawnsToNotify ??= new List<Pawn>();
                                pawnsToNotify.Add(pawn);
                            }

                            DyzeLog.Message($"Pawn {pawn.LabelShort} has developed visible symptoms!");
                        }
                        break;

                    case SimulatedDiseaseStage.Symptomatic:
                        if (state.RecoveringTick > 0 && currentTick >= state.RecoveringTick)
                        {
                            state.Stage = SimulatedDiseaseStage.Recovering;
                            state.RecoveredTick = currentTick + RecoveringDurationTicks;
                            RemoveVisibleHediff(pawn);
                            state.VisibleHediffApplied = false;
                            DyzeLog.Message($"Pawn {pawn.LabelShort} is now recovering.");
                        }
                        break;

                    case SimulatedDiseaseStage.Recovering:
                        if (state.RecoveredTick > 0 && currentTick >= state.RecoveredTick)
                        {
                            state.Stage = SimulatedDiseaseStage.Recovered;
                            RemoveVisibleHediff(pawn);
                            state.VisibleHediffApplied = false;
                            DyzeLog.Message($"Pawn {pawn.LabelShort} has recovered from the disease.");

                            if (pawn.IsColonist)
                            {
                                pawnsToNotifyRecovered ??= new List<Pawn>();
                                pawnsToNotifyRecovered.Add(pawn);
                            }

                            pawnIdsToRemove ??= new List<int>();
                            pawnIdsToRemove.Add(kvp.Key);
                        }
                        break;
                }
            }

            if (pawnsToNotify != null)
            {
                foreach (Pawn pawn in pawnsToNotify)
                {
                    SendSymptomOnsetNotification(pawn);
                }
            }

            if (pawnsToNotifyRecovered != null)
            {
                foreach (Pawn pawn in pawnsToNotifyRecovered)
                {
                    DiseaseLetterUtility.SendRecoveryLetter(pawn);
                }
            }

            if (pawnIdsToRemove != null)
            {
                foreach (int pawnId in pawnIdsToRemove)
                {
                    Registry.Remove(pawnId);
                }
            }
        }

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

        private void ApplyVisibleHediff(Pawn pawn)
        {
            if (pawn == null || pawn.health == null)
            {
                return;
            }

            HediffDef hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("DP_PathogenicFlu");
            if (hediffDef == null)
            {
                return;
            }

            Hediff existingHediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
            if (existingHediff != null)
            {
                existingHediff.Severity = 0.001f;
                PawnDiseaseState existingState = GameComponent?.TryGetDiseaseState(pawn);
                if (existingState != null)
                {
                    existingState.VisibleHediffApplied = true;
                }
                return;
            }

            Hediff newHediff = HediffMaker.MakeHediff(hediffDef, pawn);
            newHediff.Severity = 0.001f;
            pawn.health.AddHediff(newHediff);

            PawnDiseaseState state = GameComponent?.TryGetDiseaseState(pawn);
            if (state != null)
            {
                state.VisibleHediffApplied = true;
            }
        }

        private static void RemoveVisibleHediff(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null)
            {
                return;
            }

            HediffDef hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("DP_PathogenicFlu");
            if (hediffDef == null)
            {
                return;
            }

            Hediff existingHediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
            if (existingHediff != null)
            {
                pawn.health.RemoveHediff(existingHediff);
            }
        }

        private void SendSymptomOnsetNotification(Pawn pawn)
        {
            if (pawn == null || !pawn.IsColonist)
            {
                return;
            }

            DiseaseLetterUtility.SendSymptomOnsetLetter(pawn);
        }

        public void AddExposureToPawn(Pawn pawn, float amount)
        {
            if (pawn == null)
            {
                return;
            }

            if (DyzePathogenicsMod.Settings?.AffectColonistsOnly == true && !pawn.IsColonist)
            {
                return;
            }

            PawnDiseaseState state = GetOrCreateDiseaseState(pawn);
            if (state == null)
            {
                return;
            }

            int currentTick = Find.TickManager.TicksGame;
            if (state.Stage == SimulatedDiseaseStage.None || state.Stage == SimulatedDiseaseStage.Recovered)
            {
                state.Stage = SimulatedDiseaseStage.Exposed;
                state.ExposedTick = currentTick;
                state.ClearExposure();
                state.VisibleHediffApplied = false;
            }

            bool thresholdCrossed = state.AddExposure(amount);
            if (thresholdCrossed && state.Stage == SimulatedDiseaseStage.Exposed)
            {
                StartIncubation(state, currentTick);
            }
        }
    }
}
