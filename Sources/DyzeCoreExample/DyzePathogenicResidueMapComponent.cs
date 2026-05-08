using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Dyze.RimWorld.CoreExample
{
    public class DyzePathogenicResidueMapComponent : MapComponent
    {

        private const int CheckIntervalTicks = 250;
        private const float SpawnChancePerCheck = 0.08f;

        public DyzePathogenicResidueMapComponent(Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            
            if(Find.TickManager.TicksGame % CheckIntervalTicks == 0)
            {
                TrySpawnResidueForSickPawns();
            }
        }

        private void TrySpawnResidueForSickPawns()
        {
            List<Pawn> allPawns = map.mapPawns.AllPawns;

            for(int i = 0; i < allPawns.Count; i++)
            {
                Pawn pawn = allPawns[i];
                
                if(!ShouldPawnLeaveResidue(pawn))
                {
                    continue;
                }

                if(!Rand.Chance(SpawnChancePerCheck))
                {
                    continue;
                }

                TryPlaceResidueAt(pawn.Position);
            }
        }

        private bool ShouldPawnLeaveResidue(Pawn pawn)
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

            if(pawn.health?.hediffSet?.hediffs == null)
            {
                return false;
            }

            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;

            for(int i = 0; i < hediffs.Count; i++)
            {
                Hediff hediff = hediffs[i];
                if(IsResidueRelevantHediff(hediff))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsResidueRelevantHediff(Hediff hediff)
        {
            if (hediff?.def == null)
            {
                return false;
            }

            if (!hediff.def.isBad)
            {
                return false;
            }

            // Exclude wounds and missing limbs. We want sickness-like conditions for now.
            if (hediff is Hediff_Injury || hediff is Hediff_MissingPart)
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