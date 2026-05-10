using RimWorld;
using Verse;

namespace Dyze.RimWorld.Pathogenics
{
    [DefOf]
    public static class DyzeThingDefOf
    {
        static DyzeThingDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DyzeThingDefOf));
        }
    }
}