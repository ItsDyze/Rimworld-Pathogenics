using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace Dyze.RimWorld.Pathogenics
{
    internal static class PathogenicsPawnLookup
    {
        public static Pawn FindAnyPawnById(int pawnId)
        {
            if (pawnId <= 0)
            {
                return null;
            }

            foreach (Map map in Find.Maps)
            {
                if (map?.mapPawns?.AllPawnsSpawned == null)
                {
                    continue;
                }

                List<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
                for (int i = 0; i < pawns.Count; i++)
                {
                    Pawn pawn = pawns[i];
                    if (pawn != null && pawn.thingIDNumber == pawnId)
                    {
                        return pawn;
                    }
                }
            }

            foreach (Pawn pawn in GetCandidatePawnsFromWorldAndTravel())
            {
                if (pawn != null && pawn.thingIDNumber == pawnId)
                {
                    return pawn;
                }
            }

            return null;
        }

        public static string DescribePawnLocation(Pawn pawn)
        {
            if (pawn == null)
            {
                return "unknown";
            }

            if (pawn.Map != null)
            {
                return $"map {pawn.Map.uniqueID}";
            }

            if (IsLikelyTravelingPawn(pawn))
            {
                return "caravan/off-map";
            }

            return "off-map";
        }

        private static bool IsLikelyTravelingPawn(Pawn pawn)
        {
            foreach (Pawn candidate in GetTravelingPawns())
            {
                if (candidate != null && candidate.thingIDNumber == pawn.thingIDNumber)
                {
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<Pawn> GetCandidatePawnsFromWorldAndTravel()
        {
            HashSet<int> seen = new HashSet<int>();

            foreach (Pawn pawn in GetTravelingPawns())
            {
                if (pawn != null && seen.Add(pawn.thingIDNumber))
                {
                    yield return pawn;
                }
            }

            foreach (Pawn pawn in GetWorldPawns())
            {
                if (pawn != null && seen.Add(pawn.thingIDNumber))
                {
                    yield return pawn;
                }
            }
        }

        private static IEnumerable<Pawn> GetTravelingPawns()
        {
            Type pawnsFinderType = typeof(PawnsFinder);
            string[] memberNames =
            {
                "AllMapsCaravansAndTravelingTransportPods_Alive_Colonists",
                "AllCaravansAndTravelingTransportPods_Alive_Colonists",
                "AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonists",
                "AllCaravansAndTravelingTransportPods_Alive_FreeColonists",
                "AllMapsCaravansAndTravelingTransportPods_Alive",
                "AllCaravansAndTravelingTransportPods_Alive"
            };

            return GetPawnSequenceFromMembers(pawnsFinderType, memberNames);
        }

        private static IEnumerable<Pawn> GetWorldPawns()
        {
            object worldPawns = Find.World?.worldPawns;
            if (worldPawns == null)
            {
                return Enumerable.Empty<Pawn>();
            }

            return GetPawnSequenceFromMembers(
                worldPawns.GetType(),
                new[]
                {
                    "AllPawnsAliveOrDead",
                    "AllPawnsAlive",
                    "AllPawnsDead",
                    "AllPawnsUnusual"
                },
                worldPawns);
        }

        private static IEnumerable<Pawn> GetPawnSequenceFromMembers(Type declaringType, string[] memberNames, object instance = null)
        {
            for (int i = 0; i < memberNames.Length; i++)
            {
                string memberName = memberNames[i];
                PropertyInfo property = declaringType.GetProperty(memberName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
                if (property != null)
                {
                    object value = property.GetValue(instance, null);
                    IEnumerable<Pawn> pawns = CoercePawnEnumerable(value);
                    if (pawns != null)
                    {
                        return pawns;
                    }
                }

                MethodInfo method = declaringType.GetMethod(memberName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance, null, Type.EmptyTypes, null);
                if (method != null)
                {
                    object value = method.Invoke(instance, null);
                    IEnumerable<Pawn> pawns = CoercePawnEnumerable(value);
                    if (pawns != null)
                    {
                        return pawns;
                    }
                }
            }

            return Enumerable.Empty<Pawn>();
        }

        private static IEnumerable<Pawn> CoercePawnEnumerable(object value)
        {
            if (value is IEnumerable<Pawn> pawns)
            {
                return pawns;
            }

            if (value is IEnumerable<object> pawnObjects)
            {
                return pawnObjects.OfType<Pawn>();
            }

            return null;
        }
    }
}
