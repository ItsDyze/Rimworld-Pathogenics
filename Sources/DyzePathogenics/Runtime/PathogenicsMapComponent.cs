using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Dyze.RimWorld.Pathogenics.Simulation;
using Dyze.RimWorld.Pathogenics.Integration;

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

        public Dictionary<string, PawnDiseaseState> PawnDiseaseStates => PathogenicsGameComponent.Instance?.PawnDiseaseStates;

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
                RemoveDeprecatedPathogenicFluFromMap();
            }
        }

        private void RemoveDeprecatedPathogenicFluFromMap()
        {
            if (map?.mapPawns?.AllPawnsSpawned == null)
            {
                return;
            }

            var pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn == null)
                {
                    continue;
                }

                RemoveVisibleHediff(pawn, PathogenicsDiseaseRegistry.LegacyPathogenicFluDefName);

                foreach (PawnDiseaseState state in GameComponent?.GetDiseaseStates(pawn) ?? new List<PawnDiseaseState>())
                {
                    if (PathogenicsDiseaseRegistry.IsLegacyPathogenicFlu(state))
                    {
                        GameComponent?.ClearDiseaseState(pawn, state.DiseaseDefName);
                    }
                }
            }
        }

        private Dictionary<string, PawnDiseaseState> Registry => GameComponent?.PawnDiseaseStates;

        private IEnumerable<KeyValuePair<string, PawnDiseaseState>> DiseaseStateEntries =>
            Registry != null ? Registry : Enumerable.Empty<KeyValuePair<string, PawnDiseaseState>>();

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

            List<string> keysToRemove = null;
            foreach (KeyValuePair<string, PawnDiseaseState> kvp in DiseaseStateEntries)
            {
                PawnDiseaseState state = kvp.Value;
                int pawnId = state?.PawnId ?? -1;
                if (state != null && state.PreserveAcrossMaps)
                {
                    continue;
                }

                if (!currentPawnIds.Contains(pawnId) && ResolveTrackedPawn(pawnId) == null)
                {
                    keysToRemove ??= new List<string>();
                    keysToRemove.Add(kvp.Key);
                }
            }

            if (keysToRemove == null)
            {
                return;
            }

            for (int i = 0; i < keysToRemove.Count; i++)
            {
                if (DyzePathogenicsMod.Settings?.EnableDebugLogging == true && Registry.TryGetValue(keysToRemove[i], out PawnDiseaseState staleState))
                {
                    DyzeLog.Message($"Removing stale Pathogenics state for pawn ID {staleState?.PawnId ?? -1} ({staleState?.DiseaseDefName ?? "<unknown>"}, stage={staleState?.GetStageLabel() ?? "<null>"}).");
                }

                Registry.Remove(keysToRemove[i]);
            }
        }

        public PawnDiseaseState GetOrCreateDiseaseState(Pawn pawn, string diseaseDefName = PathogenicsDiseaseRegistry.DefaultDiseaseDefName)
        {
            return GameComponent?.GetOrCreateDiseaseState(pawn, diseaseDefName);
        }

        public PawnDiseaseState GetDiseaseState(Pawn pawn)
        {
            if (pawn == null)
            {
                return null;
            }

            PawnDiseaseState state = GameComponent?.TryGetDiseaseState(pawn);
            if (state == null)
            {
                string visibleDiseaseDefName = GetVisiblePathogenicsDiseaseDefName(pawn);
                if (!visibleDiseaseDefName.NullOrEmpty())
                {
                    if (PathogenicsDiseaseRegistry.IsLegacyPathogenicFlu(visibleDiseaseDefName))
                    {
                        RemoveVisibleHediff(pawn, PathogenicsDiseaseRegistry.LegacyPathogenicFluDefName);
                        GameComponent?.ClearDiseaseState(pawn, visibleDiseaseDefName);
                        return null;
                    }

                    state = EnsureSymptomaticDiseaseState(pawn, visibleDiseaseDefName);
                }
            }

            if (state != null && pawn.Map != null)
            {
                state.MapId = pawn.Map.uniqueID;
                state.PreserveAcrossMaps = pawn.IsColonist;
            }

            return state;
        }

        public PawnDiseaseState GetDiseaseState(Pawn pawn, string diseaseDefName)
        {
            PawnDiseaseState state = GameComponent?.TryGetDiseaseState(pawn, diseaseDefName);
            if (state != null && pawn?.Map != null)
            {
                state.MapId = pawn.Map.uniqueID;
                state.PreserveAcrossMaps = pawn.IsColonist;
            }

            return state;
        }

        public List<PawnDiseaseState> GetDiseaseStates(Pawn pawn)
        {
            return GameComponent?.GetDiseaseStates(pawn) ?? new List<PawnDiseaseState>();
        }

        public List<KeyValuePair<Pawn, PawnDiseaseState>> GetActiveDiseaseStates()
        {
            List<KeyValuePair<Pawn, PawnDiseaseState>> activeStates = new List<KeyValuePair<Pawn, PawnDiseaseState>>();
            HashSet<string> addedStateKeys = new HashSet<string>();

            var pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (!IsAliveRelevantMapPawn(pawn))
                {
                    continue;
                }

                foreach (PawnDiseaseState state in GetDiseaseStates(pawn))
                {
                    if (state == null || !state.HasDiseaseState())
                    {
                        continue;
                    }

                    activeStates.Add(new KeyValuePair<Pawn, PawnDiseaseState>(pawn, state));
                    addedStateKeys.Add(PathogenicsGameComponent.MakeDiseaseStateKey(state));
                }
            }

            foreach (Pawn pawn in GetAliveCaravanColonists())
            {
                if (pawn == null || pawn.Dead || !pawn.IsColonist)
                {
                    continue;
                }

                foreach (PawnDiseaseState state in GetDiseaseStates(pawn))
                {
                    if (state == null || !state.HasDiseaseState())
                    {
                        continue;
                    }

                    string key = PathogenicsGameComponent.MakeDiseaseStateKey(state);
                    if (addedStateKeys.Contains(key))
                    {
                        continue;
                    }

                    activeStates.Add(new KeyValuePair<Pawn, PawnDiseaseState>(pawn, state));
                    addedStateKeys.Add(key);
                }
            }

            return activeStates;
        }

        private static bool IsAliveRelevantMapPawn(Pawn pawn)
        {
            return pawn != null && pawn.Spawned && !pawn.Dead;
        }

        private List<string> GetVisiblePathogenicsDiseaseDefNames(Pawn pawn)
        {
            List<string> diseaseDefNames = new List<string>();
            if (pawn?.health?.hediffSet == null)
            {
                return diseaseDefNames;
            }

            foreach (PathogenicsDiseaseProfile profile in PathogenicsDiseaseRegistry.AvailableProfiles())
            {
                HediffDef hediffDef = profile.HediffDef;
                if (hediffDef != null && pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef) != null)
                {
                    diseaseDefNames.Add(hediffDef.defName);
                }
            }

            return diseaseDefNames;
        }

        private string GetVisiblePathogenicsDiseaseDefName(Pawn pawn)
        {
            return GetVisiblePathogenicsDiseaseDefNames(pawn).FirstOrDefault();
        }

        private bool PawnHasVisiblePathogenicsDisease(Pawn pawn, PawnDiseaseState state)
        {
            HediffDef hediffDef = PathogenicsDiseaseRegistry.GetProfile(state)?.HediffDef;
            return hediffDef != null && pawn.health?.hediffSet?.GetFirstHediffOfDef(hediffDef) != null;
        }

        private PawnDiseaseState EnsureSymptomaticDiseaseState(Pawn pawn, string diseaseDefName = PathogenicsDiseaseRegistry.DefaultDiseaseDefName)
        {
            if (pawn == null)
            {
                return null;
            }

            if (PathogenicsDiseaseRegistry.IsLegacyPathogenicFlu(diseaseDefName))
            {
                RemoveVisibleHediff(pawn, PathogenicsDiseaseRegistry.LegacyPathogenicFluDefName);
                return null;
            }

            int currentTick = Find.TickManager.TicksGame;
            PawnDiseaseState diseaseState = GetOrCreateDiseaseState(pawn, diseaseDefName);
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

            GameComponent?.ClearDiseaseState(pawn);
        }

        public void ClearDiseaseState(Pawn pawn, string diseaseDefName)
        {
            if (pawn == null)
            {
                return;
            }

            GameComponent?.ClearDiseaseState(pawn, diseaseDefName);
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

                foreach (string visibleDiseaseDefName in GetVisiblePathogenicsDiseaseDefNames(pawn))
                {
                    if (PathogenicsDiseaseRegistry.IsLegacyPathogenicFlu(visibleDiseaseDefName))
                    {
                        RemoveVisibleHediff(pawn, PathogenicsDiseaseRegistry.LegacyPathogenicFluDefName);
                        continue;
                    }

                    if (GameComponent?.TryGetDiseaseState(pawn, visibleDiseaseDefName) == null)
                    {
                        EnsureSymptomaticDiseaseState(pawn, visibleDiseaseDefName);
                    }
                }

                foreach (PawnDiseaseState state in GetDiseaseStates(pawn).ToList())
                {
                    if (PathogenicsDiseaseRegistry.IsLegacyPathogenicFlu(state))
                    {
                        GameComponent?.ClearDiseaseState(pawn, state.DiseaseDefName);
                        continue;
                    }

                    bool hasVisibleHediff = PawnHasVisiblePathogenicsDisease(pawn, state);

                    if (state.Stage >= SimulatedDiseaseStage.Recovering && hasVisibleHediff)
                    {
                        RemoveVisibleHediff(pawn, state);
                        state.VisibleHediffApplied = false;
                        continue;
                    }

                    if (state.Stage == SimulatedDiseaseStage.Symptomatic)
                    {
                        if (!hasVisibleHediff)
                        {
                            ApplyVisibleHediff(pawn, state);
                        }
                        else
                        {
                            state.VisibleHediffApplied = true;
                        }
                    }
                    else if (hasVisibleHediff)
                    {
                        RemoveVisibleHediff(pawn, state);
                        state.VisibleHediffApplied = false;
                    }
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
            List<string> keysToClear = null;

            foreach (KeyValuePair<string, PawnDiseaseState> kvp in DiseaseStateEntries)
            {
                PawnDiseaseState state = kvp.Value;
                if (state == null || state.Stage != SimulatedDiseaseStage.Exposed || state.Exposure <= 0f)
                {
                    continue;
                }

                state.AddExposure(-decayPerInterval);

                if (state.Exposure <= 0f)
                {
                    keysToClear ??= new List<string>();
                    keysToClear.Add(kvp.Key);
                }
            }

            if (keysToClear == null)
            {
                return;
            }

            for (int i = 0; i < keysToClear.Count; i++)
            {
                if (DyzePathogenicsMod.Settings?.EnableDebugLogging == true && Registry.TryGetValue(keysToClear[i], out PawnDiseaseState expiredExposureState))
                {
                    DyzeLog.Message($"Clearing expired exposure for pawn ID {expiredExposureState?.PawnId ?? -1} ({expiredExposureState?.DiseaseDefName ?? "<unknown>"}).");
                }

                Registry.Remove(keysToClear[i]);
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

            DyzeLog.Message($"Pawn (ID: {state.PawnId}) has accumulated enough {state.DiseaseDefName} exposure and is now incubating.");
        }

        public void ProcessStageTransitions()
        {
            if (Registry == null)
            {
                return;
            }

            int currentTick = Find.TickManager.TicksGame;
            List<string> keysToRemove = null;
            List<KeyValuePair<Pawn, PawnDiseaseState>> symptomsToNotify = null;
            List<KeyValuePair<Pawn, PawnDiseaseState>> recoveriesToNotify = null;

            foreach (KeyValuePair<string, PawnDiseaseState> kvp in DiseaseStateEntries)
            {
                PawnDiseaseState state = kvp.Value;
                if (state == null || !state.HasDiseaseState())
                {
                    continue;
                }

                if (PathogenicsDiseaseRegistry.IsLegacyPathogenicFlu(state))
                {
                    Pawn deprecatedPawn = ResolveTrackedPawn(state.PawnId);
                    RemoveVisibleHediff(deprecatedPawn, PathogenicsDiseaseRegistry.LegacyPathogenicFluDefName);
                    keysToRemove ??= new List<string>();
                    keysToRemove.Add(kvp.Key);
                    continue;
                }

                Pawn pawn = ResolveTrackedPawn(state.PawnId);
                if (pawn == null)
                {
                    if (state.PreserveAcrossMaps)
                    {
                        continue;
                    }

                    keysToRemove ??= new List<string>();
                    keysToRemove.Add(kvp.Key);
                    continue;
                }

                state.MapId = pawn.Map?.uniqueID ?? state.MapId;
                state.PreserveAcrossMaps = pawn.IsColonist;

                if (pawn.Dead)
                {
                    keysToRemove ??= new List<string>();
                    keysToRemove.Add(kvp.Key);
                    continue;
                }

                switch (state.Stage)
                {
                    case SimulatedDiseaseStage.Incubating:
                        if (state.InfectiousStartTick > 0 && currentTick >= state.InfectiousStartTick)
                        {
                            state.Stage = SimulatedDiseaseStage.PreSymptomaticInfectious;
                            DyzeLog.Message($"Pawn {pawn.LabelShort} ({state.DiseaseDefName}) is now pre-symptomatic infectious.");
                        }
                        break;

                    case SimulatedDiseaseStage.PreSymptomaticInfectious:
                        if (state.SymptomOnsetTick > 0 && currentTick >= state.SymptomOnsetTick)
                        {
                            state.Stage = SimulatedDiseaseStage.Symptomatic;
                            state.RecoveringTick = currentTick + SymptomaticDurationTicks;
                            ApplyVisibleHediff(pawn, state);

                            if (pawn.IsColonist)
                            {
                                symptomsToNotify ??= new List<KeyValuePair<Pawn, PawnDiseaseState>>();
                                symptomsToNotify.Add(new KeyValuePair<Pawn, PawnDiseaseState>(pawn, state));
                            }

                            DyzeLog.Message($"Pawn {pawn.LabelShort} has developed visible {state.DiseaseDefName} symptoms!");
                        }
                        break;

                    case SimulatedDiseaseStage.Symptomatic:
                        if (state.RecoveringTick > 0 && currentTick >= state.RecoveringTick)
                        {
                            state.Stage = SimulatedDiseaseStage.Recovering;
                            state.RecoveredTick = currentTick + RecoveringDurationTicks;
                            RemoveVisibleHediff(pawn, state);
                            state.VisibleHediffApplied = false;
                            DyzeLog.Message($"Pawn {pawn.LabelShort} ({state.DiseaseDefName}) is now recovering.");
                        }
                        break;

                    case SimulatedDiseaseStage.Recovering:
                        if (state.RecoveredTick > 0 && currentTick >= state.RecoveredTick)
                        {
                            state.Stage = SimulatedDiseaseStage.Recovered;
                            RemoveVisibleHediff(pawn, state);
                            state.VisibleHediffApplied = false;
                            DyzeLog.Message($"Pawn {pawn.LabelShort} has recovered from {state.DiseaseDefName}.");

                            if (pawn.IsColonist)
                            {
                                recoveriesToNotify ??= new List<KeyValuePair<Pawn, PawnDiseaseState>>();
                                recoveriesToNotify.Add(new KeyValuePair<Pawn, PawnDiseaseState>(pawn, state));
                            }

                            keysToRemove ??= new List<string>();
                            keysToRemove.Add(kvp.Key);
                        }
                        break;
                }
            }

            if (symptomsToNotify != null)
            {
                foreach (KeyValuePair<Pawn, PawnDiseaseState> notification in symptomsToNotify)
                {
                    SendSymptomOnsetNotification(notification.Key, notification.Value);
                }
            }

            if (recoveriesToNotify != null)
            {
                foreach (KeyValuePair<Pawn, PawnDiseaseState> notification in recoveriesToNotify)
                {
                    DiseaseLetterUtility.SendRecoveryLetter(notification.Key, notification.Value);
                }
            }

            if (keysToRemove != null)
            {
                foreach (string key in keysToRemove)
                {
                    Registry.Remove(key);
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

        private void ApplyVisibleHediff(Pawn pawn, PawnDiseaseState state)
        {
            if (pawn == null || pawn.health == null || state == null)
            {
                return;
            }

            HediffDef hediffDef = PathogenicsDiseaseRegistry.GetProfile(state)?.HediffDef;
            if (hediffDef == null)
            {
                return;
            }

            Hediff existingHediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
            if (existingHediff != null)
            {
                existingHediff.Severity = 0.001f;
                state.VisibleHediffApplied = true;
                return;
            }

            Hediff newHediff = HediffMaker.MakeHediff(hediffDef, pawn);
            newHediff.Severity = 0.001f;
            pawn.health.AddHediff(newHediff);
            state.VisibleHediffApplied = true;
        }

        private static void RemoveVisibleHediff(Pawn pawn, PawnDiseaseState state)
        {
            HediffDef hediffDef = PathogenicsDiseaseRegistry.GetProfile(state)?.HediffDef;
            RemoveVisibleHediff(pawn, hediffDef?.defName);
        }

        private static void RemoveVisibleHediff(Pawn pawn, string hediffDefName = null)
        {
            if (pawn?.health?.hediffSet == null || hediffDefName.NullOrEmpty())
            {
                return;
            }

            HediffDef hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail(hediffDefName);
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

        private void SendSymptomOnsetNotification(Pawn pawn, PawnDiseaseState state)
        {
            if (pawn == null || !pawn.IsColonist)
            {
                return;
            }

            DiseaseLetterUtility.SendSymptomOnsetLetter(pawn, state);
        }

        public void AddExposureToPawn(Pawn pawn, float amount, string diseaseDefName = PathogenicsDiseaseRegistry.DefaultDiseaseDefName)
        {
            if (pawn == null)
            {
                return;
            }

            if (PathogenicsDiseaseRegistry.IsLegacyPathogenicFlu(diseaseDefName))
            {
                return;
            }

            if (DyzePathogenicsMod.Settings?.AffectColonistsOnly == true && !pawn.IsColonist)
            {
                return;
            }

            string requestedDiseaseDefName = diseaseDefName.NullOrEmpty() ? PathogenicsDiseaseRegistry.DefaultDiseaseDefName : diseaseDefName;
            PawnDiseaseState state = GetOrCreateDiseaseState(pawn, requestedDiseaseDefName);
            if (state == null)
            {
                return;
            }

            int currentTick = Find.TickManager.TicksGame;
            if (state.Stage == SimulatedDiseaseStage.None || state.Stage == SimulatedDiseaseStage.Recovered)
            {
                state.Stage = SimulatedDiseaseStage.Exposed;
                state.DiseaseDefName = requestedDiseaseDefName;
                state.ExposedTick = currentTick;
                state.ClearExposure();
                state.VisibleHediffApplied = false;
            }

            bool thresholdCrossed = state.AddExposure(amount);
            if (DyzePathogenicsMod.Settings?.EnableDebugLogging == true)
            {
                DyzeLog.Message($"Added {amount:F4} exposure to {pawn.LabelShort} for {state.DiseaseDefName}; current={state.Exposure:F2}/1.00, stage={state.GetStageLabel()}.");
            }

            if (thresholdCrossed && state.Stage == SimulatedDiseaseStage.Exposed)
            {
                StartIncubation(state, currentTick);
            }
        }
    }
}
