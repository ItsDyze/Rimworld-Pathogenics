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
        public const int SpawnCheckIntervalTicks = 500;
        public const float DefaultImportChance = 0.15f;
        public const float IncubatingDistribution = 0.70f;
        public const float PreSymptomaticInfectiousDistribution = 0.25f;

        public static void ProcessOutsiderSpawns(Map map)
        {
            if (map == null)
            {
                return;
            }

            if (DyzePathogenicsMod.Settings?.EnableOutsiderImportation != true)
            {
                return;
            }

            int currentTick = Find.TickManager.TicksGame;
            if (currentTick % SpawnCheckIntervalTicks != 0)
            {
                return;
            }

            PathogenicsMapComponent mapComponent = map.GetComponent<PathogenicsMapComponent>();
            PathogenicsGameComponent gameComponent = PathogenicsGameComponent.Instance;
            if (mapComponent == null || gameComponent == null)
            {
                return;
            }

            var allPawns = map.mapPawns.AllPawnsSpawned;
            if (allPawns.Count == 0)
            {
                return;
            }

            for (int i = 0; i < allPawns.Count; i++)
            {
                Pawn pawn = allPawns[i];
                if (pawn == null || !pawn.Spawned || pawn.Dead)
                {
                    continue;
                }

                if (!pawn.RaceProps.Humanlike)
                {
                    continue;
                }

                if (pawn.IsColonist)
                {
                    continue;
                }

                if (gameComponent.HasCheckedOutsider(pawn.thingIDNumber))
                {
                    continue;
                }

                gameComponent.MarkOutsiderChecked(pawn.thingIDNumber);

                if (!IsOutsider(pawn))
                {
                    continue;
                }

                TryImportDisease(pawn, mapComponent, currentTick);
            }
        }

        private static bool IsOutsider(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            if (pawn.Faction != null && pawn.Faction.IsPlayer)
            {
                return false;
            }

            if (pawn.IsColonist)
            {
                return false;
            }

            if (pawn.HostileTo(Faction.OfPlayer))
            {
                return true;
            }

            if (pawn.TraderKind != null)
            {
                return true;
            }

            if (pawn.IsPrisonerOfColony)
            {
                return true;
            }

            if (pawn.IsQuestLodger())
            {
                return true;
            }

            if (pawn.guest != null)
            {
                return true;
            }

            return true;
        }

        private static void TryImportDisease(Pawn pawn, PathogenicsMapComponent mapComponent, int currentTick)
        {
            if (pawn == null || mapComponent == null)
            {
                return;
            }

            PawnDiseaseState existingState = mapComponent.GetDiseaseState(pawn);
            if (existingState != null && existingState.HasDiseaseState())
            {
                return;
            }

            float importChance = DyzePathogenicsMod.Settings?.OutsiderImportChance ?? DefaultImportChance;
            if (Rand.Value > importChance)
            {
                if (DyzePathogenicsMod.Settings?.EnableDebugLogging == true)
                {
                    DyzeLog.Message($"Outsider {pawn.LabelShort} arrived but did not import the disease.");
                }

                return;
            }

            PawnDiseaseState state = mapComponent.GetOrCreateDiseaseState(pawn);
            if (state == null)
            {
                return;
            }

            state.ExposedTick = currentTick;
            state.Exposure = 0f;
            state.VisibleHediffApplied = false;
            state.RecoveringTick = -1;
            state.RecoveredTick = -1;
            state.MapId = pawn.Map?.uniqueID ?? state.MapId;
            state.PreserveAcrossMaps = pawn.IsColonist;

            float roll = Rand.Value;
            if (roll < IncubatingDistribution)
            {
                state.Stage = SimulatedDiseaseStage.Incubating;
                state.InfectiousStartTick = currentTick + 30000;
                state.SymptomOnsetTick = currentTick + 90000;

                if (DyzePathogenicsMod.Settings?.EnableDebugLogging == true)
                {
                    DyzeLog.Message($"Outsider {pawn.LabelShort} imported disease (incubating).");
                }
            }
            else if (roll < IncubatingDistribution + PreSymptomaticInfectiousDistribution)
            {
                state.Stage = SimulatedDiseaseStage.PreSymptomaticInfectious;
                state.InfectiousStartTick = currentTick;
                state.SymptomOnsetTick = currentTick + 60000;

                if (DyzePathogenicsMod.Settings?.EnableDebugLogging == true)
                {
                    DyzeLog.Message($"Outsider {pawn.LabelShort} imported disease (pre-symptomatic infectious).");
                }
            }
            else
            {
                state.Stage = SimulatedDiseaseStage.Symptomatic;
                state.InfectiousStartTick = currentTick;
                state.SymptomOnsetTick = currentTick;
                state.RecoveringTick = currentTick + 60000;
                ApplyVisibleHediff(pawn, state);

                if (DyzePathogenicsMod.Settings?.EnableDebugLogging == true)
                {
                    DyzeLog.Message($"Outsider {pawn.LabelShort} imported disease (symptomatic - visible!).");
                }
            }
        }

        private static void ApplyVisibleHediff(Pawn pawn, PawnDiseaseState state)
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
                state.VisibleHediffApplied = true;
                return;
            }

            Hediff newHediff = HediffMaker.MakeHediff(hediffDef, pawn);
            newHediff.Severity = 0.001f;
            pawn.health.AddHediff(newHediff);
            state.VisibleHediffApplied = true;
        }

        public static void ClearCache()
        {
            PathogenicsGameComponent.Instance?.ClearCheckedOutsiderCache();
        }
    }
}
