using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Dyze.RimWorld.CoreExample
{
    public class DyzePathogenicResidueMapComponent : MapComponent
    {

        public DyzePathogenicResidueMapComponent(Map map) : base(map)
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
            List<Pawn> allPawns = map.mapPawns.AllPawns;

            for(int i = 0; i < allPawns.Count; i++)
            {
                Pawn pawn = allPawns[i];
                
                if(!TryGetResidueSpawnChance(pawn, settings, out float spawnChance))
                {
                    continue;
                }

                if(!Rand.Chance(spawnChance))
                {
                    continue;
                }

                TryPlaceResidueAt(pawn.Position);
            }
        }

        private bool TryGetResidueSpawnChance(Pawn pawn, DyzePathogenicResidueSettings settings, out float spawnChance)
        {
            spawnChance = 0f;

            if(!IsValidPawn(pawn, settings))
            {
                return false;
            }

            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            float highestFactor = 0f;

            for(int i = 0; i < hediffs.Count; i++)
            {
                Hediff hediff = hediffs[i];
                if(!TryGetResidueExtension(hediff, out DyzePathogenicResidueHediffExtension extension))
                {
                    continue;
                }

                if(hediff.Severity < extension.minSeverity)
                {
                    continue;
                }

                if(extension.spawnChanceFactor > highestFactor)
                {
                    highestFactor = extension.spawnChanceFactor;
                }
            }

            if(highestFactor <= 0f)
            {
                return false;
            }

            spawnChance = Mathf.Clamp01(settings.SpawnChancePerCheck * highestFactor);
            return spawnChance > 0f;
        }

        private bool IsValidPawn(Pawn pawn, DyzePathogenicResidueSettings settings)
        {
            // Todo: Dead pawn shall leave residue if un-frozen. Is it still a pawn? Maybe I can just check for corpse instead of pawn?
            if(pawn == null || pawn.Dead || !pawn.Spawned)
            {
                return false;
            }

            if(pawn.Map != map)
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

            if(pawn.health?.hediffSet?.hediffs == null)
            {
                return false;
            }

            return true;
        }

        private bool TryGetResidueExtension(Hediff hediff, out DyzePathogenicResidueHediffExtension extension)
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

            if(!extension.enabled)
            {
                return false;
            }

            if(extension.spawnChanceFactor <= 0f)
            {
                return false;
            }

            return true;
        }

        private void TryPlaceResidueAt(IntVec3 cell)
        {
            if (!cell.InBounds(map))
            {
                return;
            }

            if (cell.Fogged(map))
            {
                return;
            }

            if (!cell.Walkable(map))
            {
                return;
            }

            FilthMaker.TryMakeFilth(
                cell,
                map,
                DyzeThingDefOf.Dyze_Filth_PathogenicResidue,
                1,
                FilthSourceFlags.None
            );
        }
    }
}