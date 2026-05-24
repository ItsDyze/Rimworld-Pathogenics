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
    /// Each entry is keyed by pawn id + disease defName so a pawn can carry multiple
    /// simultaneous simulated diseases or exposure tracks.
    /// </summary>
    public class PathogenicsGameComponent : GameComponent
    {
        private Dictionary<string, PawnDiseaseState> pawnDiseaseStates = new Dictionary<string, PawnDiseaseState>();
        private HashSet<int> checkedOutsiderPawnIds = new HashSet<int>();

        public Dictionary<string, PawnDiseaseState> PawnDiseaseStates => pawnDiseaseStates;
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
                    .Where(state => IsPersistableState(state))
                    .ToList();
                checkedPawnIdsToSave = checkedOutsiderPawnIds.ToList();
            }

            // Save format intentionally remains a flat list for compatibility with older saves.
            Scribe_Collections.Look(ref statesToSave, "pawnDiseaseStates", LookMode.Deep);
            Scribe_Collections.Look(ref checkedPawnIdsToSave, "checkedOutsiderPawnIds", LookMode.Value);

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                pawnDiseaseStates = new Dictionary<string, PawnDiseaseState>();
                if (statesToSave != null)
                {
                    ImportLegacyStates(statesToSave);
                }

                checkedOutsiderPawnIds = checkedPawnIdsToSave != null
                    ? new HashSet<int>(checkedPawnIdsToSave)
                    : new HashSet<int>();
            }

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                pawnDiseaseStates ??= new Dictionary<string, PawnDiseaseState>();
                checkedOutsiderPawnIds ??= new HashSet<int>();
                RebuildRegistryKeys();
                RemoveDeprecatedPathogenicFluState();
            }
        }

        public static string MakeDiseaseStateKey(int pawnId, string diseaseDefName)
        {
            string normalizedDisease = NormalizeDiseaseDefName(diseaseDefName);
            return pawnId + "|" + normalizedDisease;
        }

        public static string MakeDiseaseStateKey(PawnDiseaseState state)
        {
            return state == null ? null : MakeDiseaseStateKey(state.PawnId, state.DiseaseDefName);
        }

        public static string NormalizeDiseaseDefName(string diseaseDefName)
        {
            return diseaseDefName.NullOrEmpty() ? PathogenicsDiseaseRegistry.DefaultDiseaseDefName : diseaseDefName;
        }

        private static bool IsPersistableState(PawnDiseaseState state)
        {
            return state != null &&
                   state.PawnId > 0 &&
                   !PathogenicsDiseaseRegistry.IsLegacyPathogenicFlu(state);
        }

        private void RebuildRegistryKeys()
        {
            if (pawnDiseaseStates == null)
            {
                pawnDiseaseStates = new Dictionary<string, PawnDiseaseState>();
                return;
            }

            Dictionary<string, PawnDiseaseState> rebuilt = new Dictionary<string, PawnDiseaseState>();
            foreach (PawnDiseaseState state in pawnDiseaseStates.Values.ToList())
            {
                AddOrReplaceState(rebuilt, state);
            }

            pawnDiseaseStates = rebuilt;
        }

        /// <summary>
        /// DP_PathogenicFlu remains defined for save compatibility, but it is deprecated and
        /// should not persist as active Pathogenics state after a save loads.
        /// </summary>
        private void RemoveDeprecatedPathogenicFluState()
        {
            foreach (KeyValuePair<string, PawnDiseaseState> kvp in pawnDiseaseStates.ToList())
            {
                if (!PathogenicsDiseaseRegistry.IsLegacyPathogenicFlu(kvp.Value))
                {
                    continue;
                }

                RemoveVisiblePathogenicsHediff(PathogenicsPawnLookup.FindAnyPawnById(kvp.Value.PawnId), kvp.Value);
                pawnDiseaseStates.Remove(kvp.Key);
            }
        }

        /// <summary>
        /// Compatibility accessor. Returns the most advanced active state for the pawn,
        /// or the newest inactive state if no active state exists. Use disease-specific
        /// overloads when disease identity matters.
        /// </summary>
        public PawnDiseaseState TryGetDiseaseState(Pawn pawn)
        {
            return GetDiseaseStates(pawn)
                .OrderByDescending(state => state.HasDiseaseState())
                .ThenByDescending(state => (int)state.Stage)
                .ThenByDescending(MaxTick)
                .FirstOrDefault();
        }

        public PawnDiseaseState TryGetDiseaseState(Pawn pawn, string diseaseDefName)
        {
            if (pawn == null)
            {
                return null;
            }

            pawnDiseaseStates.TryGetValue(MakeDiseaseStateKey(pawn.thingIDNumber, diseaseDefName), out PawnDiseaseState state);
            return state;
        }

        public List<PawnDiseaseState> GetDiseaseStates(Pawn pawn)
        {
            if (pawn == null || pawnDiseaseStates == null)
            {
                return new List<PawnDiseaseState>();
            }

            int pawnId = pawn.thingIDNumber;
            return pawnDiseaseStates.Values
                .Where(state => state != null && state.PawnId == pawnId)
                .OrderBy(state => state.DiseaseDefName)
                .ToList();
        }

        public PawnDiseaseState GetOrCreateDiseaseState(Pawn pawn, string diseaseDefName = PathogenicsDiseaseRegistry.DefaultDiseaseDefName)
        {
            if (pawn == null)
            {
                return null;
            }

            string normalizedDisease = NormalizeDiseaseDefName(diseaseDefName);
            if (PathogenicsDiseaseRegistry.IsLegacyPathogenicFlu(normalizedDisease))
            {
                return null;
            }

            string key = MakeDiseaseStateKey(pawn.thingIDNumber, normalizedDisease);
            if (!pawnDiseaseStates.TryGetValue(key, out PawnDiseaseState state))
            {
                state = new PawnDiseaseState(pawn.thingIDNumber, pawn.Map?.uniqueID ?? -1, normalizedDisease);
                pawnDiseaseStates[key] = state;
            }
            else if (state.DiseaseDefName.NullOrEmpty())
            {
                state.DiseaseDefName = normalizedDisease;
            }

            if (pawn.Map != null)
            {
                state.MapId = pawn.Map.uniqueID;
            }

            state.PreserveAcrossMaps = pawn.IsColonist;
            return state;
        }

        public void ClearDiseaseState(Pawn pawn, string diseaseDefName)
        {
            if (pawn == null)
            {
                return;
            }

            PawnDiseaseState state = TryGetDiseaseState(pawn, diseaseDefName);
            if (state != null)
            {
                RemoveVisiblePathogenicsHediff(pawn, state);
            }

            pawnDiseaseStates.Remove(MakeDiseaseStateKey(pawn.thingIDNumber, diseaseDefName));
        }

        public void ClearDiseaseState(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            foreach (PawnDiseaseState state in GetDiseaseStates(pawn).ToList())
            {
                RemoveVisiblePathogenicsHediff(pawn, state);
                pawnDiseaseStates.Remove(MakeDiseaseStateKey(state));
            }
        }

        public bool ClearDiseaseState(PawnDiseaseState state)
        {
            if (state == null)
            {
                return false;
            }

            return pawnDiseaseStates.Remove(MakeDiseaseStateKey(state));
        }

        public void ClearAllDiseaseStates()
        {
            foreach (PawnDiseaseState state in pawnDiseaseStates.Values.ToList())
            {
                Pawn pawn = PathogenicsPawnLookup.FindAnyPawnById(state.PawnId);
                RemoveVisiblePathogenicsHediff(pawn, state);
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
                AddOrReplaceState(pawnDiseaseStates, candidate);
            }
        }

        private static void AddOrReplaceState(Dictionary<string, PawnDiseaseState> registry, PawnDiseaseState candidate)
        {
            if (registry == null || candidate == null || candidate.PawnId <= 0)
            {
                return;
            }

            if (candidate.DiseaseDefName.NullOrEmpty())
            {
                candidate.DiseaseDefName = PathogenicsDiseaseRegistry.DefaultDiseaseDefName;
            }

            if (PathogenicsDiseaseRegistry.IsLegacyPathogenicFlu(candidate))
            {
                return;
            }

            string key = MakeDiseaseStateKey(candidate);
            if (!registry.TryGetValue(key, out PawnDiseaseState existing) || ShouldReplaceExisting(existing, candidate))
            {
                registry[key] = candidate;
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
