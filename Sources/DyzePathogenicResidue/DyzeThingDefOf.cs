using RimWorld;
using Verse;

namespace Dyze.RimWorld.PathogenicResidue
{
    [DefOf]
    public static class DyzeThingDefOf
    {
        public static ThingDef Dyze_Filth_PathogenicResidue;

        static DyzeThingDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DyzeThingDefOf));
        }
    }
}