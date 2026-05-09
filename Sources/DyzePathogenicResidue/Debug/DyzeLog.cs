using Verse;

namespace Dyze.RimWorld.PathogenicResidue
{
    public static class DyzeLog
    {
        private const string Prefix = "[DyzePathogenicResidue]";

        public static void Message(string message)
        {
            if (DyzePathogenicResidueMod.Settings?.EnableDebugLogging == true)
            {
                Log.Message($"{Prefix} {message}");
            }
        }

        public static void DevAction(string message)
        {
            Log.Message($"{Prefix} {message}");
        }

        public static void Warning(string message)
        {
            Log.Warning($"{Prefix} {message}");
        }

        public static void Error(string message)
        {
            Log.Error($"{Prefix} {message}");
        }
    }
}