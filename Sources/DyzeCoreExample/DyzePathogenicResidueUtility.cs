using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Dyze.RimWorld.CoreExample
{
    public static class DyzePathogenicResidueUtility
    {
        public static bool TryGetResidueSpawnChance(
            Pawn pawn,
            out float spawnChance
        )
        {
            spawnChance = 0f;

            if (DyzeThingDefOf.Dyze_Filth_PathogenicResidue == null)
            {
                return false;
            }

            if (pawn?.health?.hediffSet?.hediffs == null)
            {
                return false;
            }

            float highestFactor = 0f;
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;

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

            float baseChance = DyzeCoreExampleMod.Settings?.SpawnChancePerCheck ?? 0.08f;
            spawnChance = Mathf.Clamp01(baseChance * highestFactor);
            return spawnChance > 0f;
        }

        public static bool TryGetResidueExtension(
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

        public static bool CellAlreadyHasPathogenicResidue(IntVec3 cell, Map map)
        {
            if (map == null || !cell.InBounds(map))
            {
                return false;
            }

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

        public static bool TryPlaceResidueAt(IntVec3 cell, Map map, bool allowDuplicate = false)
        {
            if (map == null)
            {
                return false;
            }

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

            if (!allowDuplicate && CellAlreadyHasPathogenicResidue(cell, map))
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