using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Dyze.RimWorld.CoreExample
{
    public class PathogenicResidueMapComponent : MapComponent
    {
        private readonly Dictionary<int, int> lastResidueTickByPawnId = new Dictionary<int, int>();
        private readonly Dictionary<int, IntVec3> lastCheckedCellByPawnId = new Dictionary<int, IntVec3>();

        public PathogenicResidueMapComponent(Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            DyzePathogenicResidueSettings settings = DyzeCoreExampleMod.Settings;

            if (settings == null || !settings.Enabled)
            {
                return;
            }

            settings.ClampValues();

            if (Find.TickManager.TicksGame % settings.CheckIntervalTicks != 0)
            {
                return;
            }

            TrySpawnResidueForSickPawns(settings);
        }

        private void TrySpawnResidueForSickPawns(DyzePathogenicResidueSettings settings)
        {
            var pawns = map.mapPawns.AllPawnsSpawned;

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];

                if (!TryGetResidueSpawnChance(pawn, settings, out float spawnChance))
                {
                    RememberCheckedCell(pawn);
                    continue;
                }

                if (!HasPawnMovedIfRequired(pawn, settings))
                {
                    RememberCheckedCell(pawn);
                    continue;
                }

                if (IsOnCooldown(pawn, settings))
                {
                    RememberCheckedCell(pawn);
                    continue;
                }

                if (CellAlreadyHasPathogenicResidue(pawn.Position))
                {
                    RememberCheckedCell(pawn);
                    continue;
                }

                if (!Rand.Chance(spawnChance))
                {
                    RememberCheckedCell(pawn);
                    continue;
                }

                if (TryPlaceResidueAt(pawn.Position))
                {
                    lastResidueTickByPawnId[pawn.thingIDNumber] = Find.TickManager.TicksGame;
                }

                RememberCheckedCell(pawn);
            }
        }

        private bool TryGetResidueSpawnChance(
            Pawn pawn,
            DyzePathogenicResidueSettings settings,
            out float spawnChance
        )
        {
            spawnChance = 0f;

            if (!IsValidPawn(pawn, settings))
            {
                return false;
            }

            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            float highestFactor = 0f;

            for (int i = 0; i < hediffs.Count; i++)
            {
                Hediff hediff = hediffs[i];

                if (!TryGetResidueExtension(hediff, out DyzePathogenicResidueHediffExtension extension))
                {
                    continue;
                }

                if (hediff.Severity < extension.minSeverity)
                {
                    continue;
                }

                if (extension.spawnChanceFactor > highestFactor)
                {
                    highestFactor = extension.spawnChanceFactor;
                }
            }

            if (highestFactor <= 0f)
            {
                return false;
            }

            spawnChance = Mathf.Clamp01(settings.SpawnChancePerCheck * highestFactor);
            return spawnChance > 0f;
        }

        private bool IsValidPawn(Pawn pawn, DyzePathogenicResidueSettings settings)
        {
            if (pawn == null || pawn.Dead || !pawn.Spawned)
            {
                return false;
            }

            if (pawn.Map != map)
            {
                return false;
            }

            if (pawn.RaceProps == null || !pawn.RaceProps.Humanlike)
            {
                return false;
            }

            if (settings.AffectColonistsOnly && !pawn.IsColonist)
            {
                return false;
            }

            if (pawn.health?.hediffSet?.hediffs == null)
            {
                return false;
            }

            return true;
        }

        private bool TryGetResidueExtension(
            Hediff hediff,
            out DyzePathogenicResidueHediffExtension extension
        )
        {
            extension = null;

            if (hediff?.def == null)
            {
                return false;
            }

            extension = hediff.def.GetModExtension<DyzePathogenicResidueHediffExtension>();

            if (extension == null)
            {
                return false;
            }

            if (!extension.enabled)
            {
                return false;
            }

            if (extension.spawnChanceFactor <= 0f)
            {
                return false;
            }

            return true;
        }

        private bool HasPawnMovedIfRequired(Pawn pawn, DyzePathogenicResidueSettings settings)
        {
            if (!settings.RequireMovement)
            {
                return true;
            }

            if (!lastCheckedCellByPawnId.TryGetValue(pawn.thingIDNumber, out IntVec3 lastCell))
            {
                return false;
            }

            return lastCell != pawn.Position;
        }

        private bool IsOnCooldown(Pawn pawn, DyzePathogenicResidueSettings settings)
        {
            if (settings.MinTicksBetweenResiduePerPawn <= 0)
            {
                return false;
            }

            if (!lastResidueTickByPawnId.TryGetValue(pawn.thingIDNumber, out int lastTick))
            {
                return false;
            }

            int elapsedTicks = Find.TickManager.TicksGame - lastTick;
            return elapsedTicks < settings.MinTicksBetweenResiduePerPawn;
        }

        private void RememberCheckedCell(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            lastCheckedCellByPawnId[pawn.thingIDNumber] = pawn.Position;
        }

        private bool CellAlreadyHasPathogenicResidue(IntVec3 cell)
        {
            List<Thing> things = cell.GetThingList(map);

            for (int i = 0; i < things.Count; i++)
            {
                if (things[i].def == DyzeThingDefOf.Dyze_Filth_PathogenicResidue)
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryPlaceResidueAt(IntVec3 cell)
        {
            if (!cell.InBounds(map))
            {
                return false;
            }

            if (cell.Fogged(map))
            {
                return false;
            }

            if (!cell.Walkable(map))
            {
                return false;
            }

            FilthMaker.TryMakeFilth(
                cell,
                map,
                DyzeThingDefOf.Dyze_Filth_PathogenicResidue,
                1,
                FilthSourceFlags.None
            );

            return true;
        }
    }
}