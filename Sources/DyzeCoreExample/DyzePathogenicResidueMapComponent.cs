using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Dyze.RimWorld.CoreExample
{
    public class PathogenicResidueMapComponent : MapComponent
    {
        private Dictionary<int, int> lastResidueTickByPawnId = new Dictionary<int, int>();
        private Dictionary<int, IntVec3> lastCheckedCellByPawnId = new Dictionary<int, IntVec3>();

        public PathogenicResidueMapComponent(Map map) : base(map)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Collections.Look(
                ref lastResidueTickByPawnId,
                "lastResidueTickByPawnId",
                LookMode.Value,
                LookMode.Value
            );

            Scribe_Collections.Look(
                ref lastCheckedCellByPawnId,
                "lastCheckedCellByPawnId",
                LookMode.Value,
                LookMode.Value
            );

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                lastResidueTickByPawnId ??= new Dictionary<int, int>();
                lastCheckedCellByPawnId ??= new Dictionary<int, IntVec3>();

                CleanStalePawnData();
            }
        }

        private void CleanStalePawnData()
        {
            HashSet<int> currentPawnIds = new HashSet<int>();

            var pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                currentPawnIds.Add(pawns[i].thingIDNumber);
            }

            RemoveMissingPawnIds(lastResidueTickByPawnId, currentPawnIds);
            RemoveMissingPawnIds(lastCheckedCellByPawnId, currentPawnIds);
        }

        private void RemoveMissingPawnIds<TValue>(Dictionary<int, TValue> dictionary, HashSet<int> currentPawnIds)
        {
            List<int> idsToRemove = null;

            foreach (int pawnId in dictionary.Keys)
            {
                if (!currentPawnIds.Contains(pawnId))
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
                dictionary.Remove(idsToRemove[i]);
            }
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

                

                if (!IsValidPawn(pawn, settings) || 
                    !DyzePathogenicResidueUtility.TryGetResidueSpawnChance(pawn, out float spawnChance))
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

                if (DyzePathogenicResidueUtility.CellAlreadyHasPathogenicResidue(pawn.Position, map))
                {
                    RememberCheckedCell(pawn);
                    continue;
                }

                if (!Rand.Chance(spawnChance))
                {
                    RememberCheckedCell(pawn);
                    continue;
                }

                if (DyzePathogenicResidueUtility.TryPlaceResidueAt(pawn.Position, map))
                {
                    lastResidueTickByPawnId[pawn.thingIDNumber] = Find.TickManager.TicksGame;
                }

                RememberCheckedCell(pawn);
            }
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
    }
}