using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
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
            // TODO: Future v0.2 features may use this tick for proximity-based transmission calculations
            // when the disease simulation is more fully developed.
        }
    }
}