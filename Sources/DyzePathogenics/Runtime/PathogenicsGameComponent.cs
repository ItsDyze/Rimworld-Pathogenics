using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Dyze.RimWorld.Pathogenics.Integration;

namespace Dyze.RimWorld.Pathogenics
{
    /// <summary>
    /// Global source of truth for Pathogenics disease state.
    ///
    /// State used to live in the per-map component, which made cross-map lookups,
    /// caravans, and temporary maps fragile. The game component owns the durable
    /// registry so disease state follows pawns across map changes and save/load.
    /// </summary>
    public class PathogenicsGameComponent : GameComponent
    {
        private Dictionary<int, PawnDiseaseState> pawnDiseaseStates = new Dictionary<int, PawnDiseaseState>();
        private HashSet<int> checkedOutsiderPawnIds = new HashSet<int>();

        public Dictionary<int, PawnDiseaseState> PawnDiseaseStates => pawnDiseaseStates;
        public int CheckedOutsiderPawnCount => checkedOutsiderPawnIds.Count;

        public PathogenicsGameComponent(Game game)
        {
        }

        public static PathogenicsGameComponent Instance => Current.Game?.GetComponent<PathogenicsGameComponent>();

        public override void ExposeData()
        {
            base.ExposeData();

            List<PawnDiseaseState> statesToSave = null;
            List<int> checkedPawnIdsToSave = null;

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                statesToSave = pawnDiseaseStates.Values
                    .Where(state => state != null &&
                                    state.PawnId > 0 &&
                                    !PathogenicsDiseaseRegistry.IsLegacyPathogenicFlu(state))
                    .ToList();
                checkedPawnIdsToSave = checkedOutsiderPawnIds.ToList();
            }

            Scribe_Collections.Look(ref statesToSave, "pawnDiseaseStates", LookMode.Deep);
            Scribe_Collections.Look(ref checkedPawnIdsToSave, "checkedOutsiderPawnIds", LookMode.Value);

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                pawnDiseaseStates.Clear();
                if (statesToSave != null)
                {
                    for (int i = 0; i < statesToSave.Count; i++)
                    {
                        PawnDiseaseState state = statesToSave[i];
                        if (state == null || state.PawnId <= 0)
                        {
                            continue;
                        }

                        pawnDiseaseStates[state.PawnId] = state;
                    }
                }

                checkedOutsiderPawnIds = checkedPawnIdsToSave != null
                    ? new HashSet<int>(checkedPawnIdsToSave)
                    : new HashSet<int>();
            }

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                pawnDiseaseStates ??= new Dictionary<int, PawnDiseaseState>();
                checkedOutsiderPawnIds ??= new HashSet<int>();
                RemoveDeprecatedPathogenicFluState();
            }
        }

        /// <summary>
        /// DP_PathogenicFlu remains defined for save compatibility, but it is deprecated and
        /// should not persist as active Pathogenics state after a save loads.
        /// </summary>
        private void RemoveDeprecatedPathogenicFluState()
        {
            foreach (KeyValuePair<int, PawnDiseaseState> kvp in pawnDiseaseStates.ToList())
            {
                if (!PathogenicsDiseaseRegistry.IsLegacyPathogenicFlu(kvp.Value))
                {
                    continue;
                }

                RemoveVisiblePathogenicsHediff(PathogenicsPawnLookup.FindAnyPawnById(kvp.Key), kvp.Value);
                pawnDiseaseStates.Remove(kvp.Key);
            }
        }

        public PawnDiseaseState TryGetDiseaseState(Pawn pawn)
        {
            if (pawn == null)
            {
                return null;
            }

            pawnDiseaseStates.TryGetValue(pawn.thingIDNumber, out PawnDiseaseState state);
            return state;
        }

        public PawnDiseaseState GetOrCreateDiseaseState(Pawn pawn, string diseaseDefName = PathogenicsDiseaseRegistry.DefaultDiseaseDefName)
        {
            if (pawn == null)
            {
                return null;
            }

            if (PathogenicsDiseaseRegistry.IsLegacyPathogenicFlu(diseaseDefName))
            {
                return null;
            }

            int pawnId = pawn.thingIDNumber;
            if (!pawnDiseaseStates.TryGetValue(pawnId, out PawnDiseaseState state))
            {
                state = new PawnDiseaseState(pawnId, pawn.Map?.uniqueID ?? -1, diseaseDefName);
                pawnDiseaseStates[pawnId] = state;
            }
            else if (!diseaseDefName.NullOrEmpty() && !state.HasDiseaseState())
            {
                state.DiseaseDefName = diseaseDefName;
            }
            else if (state.DiseaseDefName.NullOrEmpty())
            {
                state.DiseaseDefName = PathogenicsDiseaseRegistry.DefaultDiseaseDefName;
            }

            if (pawn.Map != null)
            {
                state.MapId = pawn.Map.uniqueID;
            }

            state.PreserveAcrossMaps = pawn.IsColonist;
            return state;
        }

        public void ClearDiseaseState(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            pawnDiseaseStates.Remove(pawn.thingIDNumber);
        }

        public void ClearAllDiseaseStates()
        {
            foreach (KeyValuePair<int, PawnDiseaseState> kvp in pawnDiseaseStates.ToList())
            {
                Pawn pawn = PathogenicsPawnLookup.FindAnyPawnById(kvp.Key);
                RemoveVisiblePathogenicsHediff(pawn, kvp.Value);
            }

            pawnDiseaseStates.Clear();
            checkedOutsiderPawnIds.Clear();
        }

        public bool HasCheckedOutsider(int pawnId)
        {
            return pawnId > 0 && checkedOutsiderPawnIds.Contains(pawnId);
        }

        public void MarkOutsiderChecked(int pawnId)
        {
            if (pawnId > 0)
            {
                checkedOutsiderPawnIds.Add(pawnId);
            }
        }

        public void ClearCheckedOutsiderCache()
        {
            checkedOutsiderPawnIds.Clear();
        }

        public void ImportLegacyStates(IEnumerable<PawnDiseaseState> legacyStates)
        {
            if (legacyStates == null)
            {
                return;
            }

            foreach (PawnDiseaseState candidate in legacyStates)
            {
                if (candidate == null || candidate.PawnId <= 0)
                {
                    continue;
                }

                // Old map-owned saves may still contain the deprecated flu state; do not
                // re-import it after the game component's load-time migration has cleared it.
                if (PathogenicsDiseaseRegistry.IsLegacyPathogenicFlu(candidate))
                {
                    continue;
                }

                if (!pawnDiseaseStates.TryGetValue(candidate.PawnId, out PawnDiseaseState existing) || ShouldReplaceExisting(existing, candidate))
                {
                    pawnDiseaseStates[candidate.PawnId] = candidate;
                }
            }
        }

        private static bool ShouldReplaceExisting(PawnDiseaseState existing, PawnDiseaseState candidate)
        {
            if (existing == null)
            {
                return true;
            }

            bool existingActive = existing.HasDiseaseState();
            bool candidateActive = candidate.HasDiseaseState();
            if (candidateActive != existingActive)
            {
                return candidateActive;
            }

            if ((int)candidate.Stage != (int)existing.Stage)
            {
                return (int)candidate.Stage > (int)existing.Stage;
            }

            if (candidate.VisibleHediffApplied != existing.VisibleHediffApplied)
            {
                return candidate.VisibleHediffApplied;
            }

            int candidateProgressTick = MaxTick(candidate);
            int existingProgressTick = MaxTick(existing);
            return candidateProgressTick > existingProgressTick;
        }

        private static int MaxTick(PawnDiseaseState state)
        {
            if (state == null)
            {
                return -1;
            }

            return new[]
            {
                state.ExposedTick,
                state.InfectiousStartTick,
                state.SymptomOnsetTick,
                state.RecoveringTick,
                state.RecoveredTick
            }.Max();
        }

        private static void RemoveVisiblePathogenicsHediff(Pawn pawn, PawnDiseaseState state = null)
        {
            if (pawn?.health?.hediffSet == null)
            {
                return;
            }

            HediffDef hediffDef = PathogenicsDiseaseRegistry.GetProfile(state)?.HediffDef ?? PathogenicsDiseaseRegistry.DefaultProfile?.HediffDef;
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
    }
}
