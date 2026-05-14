using System;
using RimWorld;
using Verse;
using Dyze.RimWorld.Pathogenics;

namespace Dyze.RimWorld.Pathogenics.Simulation
{
    /// <summary>
    /// Worker for outsider importation (v0.3 feature 7).
    /// 
    /// Detects newly spawned outsider pawns (visitors, traders, raiders, refugees, prisoners, quest pawns)
    /// and rolls for disease importation based on configuration.
    /// </summary>
    public static class DiseaseImportationWorker
    {
        // ===== CONFIGURATION: Importation parameters =====

        /// <summary>
        /// Interval between outsider spawn checks (500 ticks ≈ 8 seconds).
        /// </summary>
        public const int SpawnCheckIntervalTicks = 500;

        /// <summary>
        /// Default chance that an outsider imports the disease (0.15 = 15%).
        /// </summary>
        public const float DefaultImportChance = 0.15f;

        /// <summary>
        /// Disease state distribution when imported:
        /// 70% incubating, 25% pre-symptomatic infectious, 5% symptomatic
        /// </summary>
        public const float IncubatingDistribution = 0.70f;
        public const float PreSymptomaticInfectiousDistribution = 0.25f;
        // Remaining 0.05 = symptomatic

        /// <summary>
        /// Debug logging interval.
        /// </summary>
        private const int DebugLogIntervalTicks = 3000; // ~50 seconds

        /// <summary>
        /// Track which pawns have been checked to avoid double-checking.
        /// Key: pawn ID, Value: last tick checked
        /// </summary>
        private static FastStartupCache<int, int> checkedPawnCache = new FastStartupCache<int, int>();

        /// <summary>
        /// Process outsider spawn detection for the map.
        /// Should be called from MapComponentTick at regular intervals.
        /// </summary>
        public static void ProcessOutsiderSpawns(Map map)
        {
            if (map == null)
                return;

            // Skip if outsider importation is disabled
            if (DyzePathogenicsMod.Settings?.EnableOutsiderImportation != true)
                return;

            int currentTick = Find.TickManager.TicksGame;
            if (currentTick % SpawnCheckIntervalTicks != 0)
                return;

            PathogenicsMapComponent mapComponent = map.GetComponent<PathogenicsMapComponent>();
            if (mapComponent == null)
                return;

            // Get all spawned pawns
            var allPawns = map.mapPawns.AllPawnsSpawned;
            if (allPawns.Count == 0)
                return;

            // Check each pawn for potential outsider status
            for (int i = 0; i < allPawns.Count; i++)
            {
                Pawn pawn = allPawns[i];
                if (pawn == null || !pawn.Spawned || pawn.Dead)
                    continue;

                // Skip non-humanlike
                if (!pawn.RaceProps.Humanlike)
                    continue;

                // Skip colonists (they're not "outsiders")
                if (pawn.IsColonist)
                    continue;

                // Skip if already checked recently
                if (IsPawnRecentlyChecked(pawn.thingIDNumber, currentTick))
                    continue;

                // Mark as checked
                MarkPawnChecked(pawn.thingIDNumber, currentTick);

                // Check if this is an outsider (visitor, trader, raider, refugee, prisoner, quest pawn)
                if (!IsOutsider(pawn))
                    continue;

                // Roll for disease import
                TryImportDisease(pawn, mapComponent, currentTick);
            }
        }

        /// <summary>
        /// Check if a pawn has been checked within the cache window.
        /// </summary>
        private static bool IsPawnRecentlyChecked(int pawnId, int currentTick)
        {
            if (!checkedPawnCache.TryGetValue(pawnId, out int lastChecked))
                return false;

            // Allow re-checking after a reasonable interval (in case they leave and return)
            // For now, we don't re-check pawns that have already been checked
            return true;
        }

        /// <summary>
        /// Mark a pawn as checked.
        /// </summary>
        private static void MarkPawnChecked(int pawnId, int currentTick)
        {
            checkedPawnCache[pawnId] = currentTick;
        }

        /// <summary>
        /// Determine if a pawn is an outsider (non-colony pawn).
        /// </summary>
        private static bool IsOutsider(Pawn pawn)
        {
            if (pawn == null)
                return false;

            // Check if pawn belongs to a colony faction
            if (pawn.Faction != null && pawn.Faction.IsPlayer)
                return false;

            // Also check if they're a colonist (should have been caught above, but double-check)
            if (pawn.IsColonist)
                return false;

            // Check for specific outsider types:
            // - Traders: usually non-hostile and may be visitors
            // - Refugees: should come from in-mission or spawned
            // - Prisoners: hostile but can be captured
            // - Quest pawns: can be visitors or guests
            
            // Note: Simple approach - if not player faction, they're an outsider
            // The mod's intent is that outsiders bring the disease from outside
            
            return true;
        }

        /// <summary>
        /// Roll for disease importation and seed the disease state if successful.
        /// </summary>
        private static void TryImportDisease(Pawn pawn, PathogenicsMapComponent mapComponent, int currentTick)
        {
            if (pawn == null || mapComponent == null)
                return;

            // Check if the pawn already has disease state
            PawnDiseaseState existingState = mapComponent.GetDiseaseState(pawn);
            if (existingState != null && existingState.HasDiseaseState())
            {
                // Pawn already has disease state, no need to import
                return;
            }

            // Get configured import chance
            float importChance = DyzePathogenicsMod.Settings?.OutsiderImportChance ?? DefaultImportChance;

            // Roll for import
            if (Rand.Value > importChance)
            {
                // No disease imported
                if (DyzePathogenicsMod.Settings?.EnableDebugLogging == true)
                {
                    DyzeLog.Message($"Outsider {pawn.LabelShort} arrived but did not import the disease.");
                }
                return;
            }

            // Disease imported - assign hidden disease state
            PawnDiseaseState state = mapComponent.GetOrCreateDiseaseState(pawn);

            // Determine which disease state to assign based on distribution
            float roll = Rand.Value;
            if (roll < IncubatingDistribution)
            {
                // 70% incubating
                state.Stage = SimulatedDiseaseStage.Incubating;
                // Pre-symptomatic infectious starts at +0.5 days
                state.InfectiousStartTick = currentTick + 30000; // ~0.5 days
                // Symptom onset at +1.5 days
                state.SymptomOnsetTick = currentTick + 90000; // ~1.5 days
                
                if (DyzePathogenicsMod.Settings?.EnableDebugLogging == true)
                {
                    DyzeLog.Message($"Outsider {pawn.LabelShort} imported disease (incubating).");
                }
            }
            else if (roll < IncubatingDistribution + PreSymptomaticInfectiousDistribution)
            {
                // 25% pre-symptomatic infectious (already infectious but no symptoms yet)
                state.Stage = SimulatedDiseaseStage.PreSymptomaticInfectious;
                // Already infectious as of now
                state.InfectiousStartTick = currentTick;
                // Symptom onset at +1.0 day from now
                state.SymptomOnsetTick = currentTick + 60000; // ~1.0 days
                
                if (DyzePathogenicsMod.Settings?.EnableDebugLogging == true)
                {
                    DyzeLog.Message($"Outsider {pawn.LabelShort} imported disease (pre-symptomatic infectious).");
                }
            }
            else
            {
                // 5% symptomatic (visible disease, should be rare for newcomers)
                state.Stage = SimulatedDiseaseStage.Symptomatic;
                state.SymptomOnsetTick = currentTick;
                state.RecoveringTick = currentTick + 60000; // ~1.0 days
                
                // Apply visible hediff for symptomatic
                ApplyVisibleHediff(pawn, state);
                
                if (DyzePathogenicsMod.Settings?.EnableDebugLogging == true)
                {
                    DyzeLog.Message($"Outsider {pawn.LabelShort} imported disease (symptomatic - visible!).");
                }
            }

            state.ExposedTick = currentTick;
        }

        /// <summary>
        /// Apply visible hediff to a pawn (for symptomatic cases).
        /// </summary>
        private static void ApplyVisibleHediff(Pawn pawn, PawnDiseaseState state)
        {
            if (pawn == null || pawn.health == null)
                return;

            HediffDef hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("DP_PathogenicFlu");
            if (hediffDef == null)
                return;

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

        /// <summary>
        /// Clear the checked pawn cache (e.g., when map is loaded or reset).
        /// </summary>
        public static void ClearCache()
        {
            checkedPawnCache.Clear();
        }
    }

    /// <summary>
    /// Simple cache for fast lookups without garbage collection overhead.
    /// </summary>
    public class FastStartupCache<TKey, TValue>
    {
        private System.Collections.Generic.Dictionary<TKey, TValue> dictionary = new System.Collections.Generic.Dictionary<TKey, TValue>();

        public TValue this[TKey key]
        {
            get
            {
                dictionary.TryGetValue(key, out TValue value);
                return value;
            }
            set
            {
                dictionary[key] = value;
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return dictionary.TryGetValue(key, out value);
        }

        public void Clear()
        {
            dictionary.Clear();
        }
    }
}