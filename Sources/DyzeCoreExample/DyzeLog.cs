using Verse;

namespace Dyze.RimWorld.CoreExample
{
    public static class DyzeLog
    {
        private const string Prefix = "[DyzeCoreExample]";

        public static void Message(string message)
        {
            if (DyzeCoreExampleMod.Settings?.EnableDebugLogging == true)
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