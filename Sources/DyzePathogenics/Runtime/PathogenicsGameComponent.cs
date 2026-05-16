using System.Collections.Generic;
using System.Linq;
using Verse;

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
                    .Where(state => state != null && state.PawnId > 0)
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

        public PawnDiseaseState GetOrCreateDiseaseState(Pawn pawn)
        {
            if (pawn == null)
            {
                return null;
            }

            int pawnId = pawn.thingIDNumber;
            if (!pawnDiseaseStates.TryGetValue(pawnId, out PawnDiseaseState state))
            {
                state = new PawnDiseaseState(pawnId, pawn.Map?.uniqueID ?? -1);
                pawnDiseaseStates[pawnId] = state;
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
            pawnDiseaseStates.Clear();
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
    }
}
