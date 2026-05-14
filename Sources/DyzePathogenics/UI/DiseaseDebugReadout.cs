using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Dyze.RimWorld.Pathogenics
{
    /// <summary>
    /// UI readout showing disease simulation state for debugging and balancing.
    /// Only visible when debug display is enabled in settings.
    /// </summary>
    public class DiseaseDebugReadout
    {
        private const float LineHeight = 20f;
        private const float Padding = 10f;
        private const float ColumnWidth = 180f;

        /// <summary>
        /// Render the debug readout if enabled.
        /// Uses OnGUI for reliable rendering.
        /// </summary>
        public static void OnGUI()
        {
            if (DyzePathogenicsMod.Settings?.ShowDebugReadout != true)
                return;

            // Only render during gameplay
            if (Find.CurrentMap == null || !Find.GameInfo.Started)
                return;

            // Find the current map
            Map map = Find.CurrentMap;
            if (map == null)
                return;

            PathogenicsMapComponent mapComponent = map.GetComponent<PathogenicsMapComponent>();
            if (mapComponent == null || mapComponent.PawnDiseaseStates.Count == 0)
                return;

            // Calculate required height
            int pawnCount = Math.Min(mapComponent.PawnDiseaseStates.Count, 15); // Limit to 15 to avoid overflow
            float height = Padding + (pawnCount + 3) * LineHeight + Padding;
            float width = ColumnWidth * 2 + Padding * 2;

            // Position in top-right corner
            Rect rect = new Rect(UI.screenWidth - width - 10f, 10f, width, height);

            // Draw background
            GUI.color = new Color(0f, 0f, 0f, 0.85f);
            GUI.DrawTexture(rect, BaseContent.WhiteTex);
            GUI.color = Color.white;
            GUI.Box(rect, "");

            // Save text settings
            GameFont oldFont = Text.Font;
            Color oldColor = GUI.color;

            // Draw title
            Text.Font = GameFont.Tiny;
            GUI.color = Color.white;
            Rect titleRect = new Rect(rect.x + Padding, rect.y + Padding, rect.width - Padding * 2, LineHeight);
            Widgets.Label(titleRect, "=== Pathogenics Debug ===");

            // Draw column headers
            float y = titleRect.y + LineHeight;
            Rect pawnHeaderRect = new Rect(rect.x + Padding, y, ColumnWidth, LineHeight);
            Rect stateHeaderRect = new Rect(rect.x + Padding + ColumnWidth, y, ColumnWidth, LineHeight);
            Widgets.Label(pawnHeaderRect, "Pawn");
            Widgets.Label(stateHeaderRect, "State (Exposure)");

            // Draw pawn states
            int drawn = 0;
            foreach (var kvp in mapComponent.PawnDiseaseStates)
            {
                if (drawn >= pawnCount)
                    break;

                y += LineHeight;

                Pawn pawn = FindPawnById(map, kvp.Key);
                PawnDiseaseState state = kvp.Value;

                string pawnName = pawn != null ? pawn.LabelShort : $"ID:{kvp.Key}";
                string stateInfo = state.GetStageLabel();

                // Add exposure info if relevant
                if (state.Stage == SimulatedDiseaseStage.Exposed)
                {
                    stateInfo = $"{state.Exposure:F2}/1.0";
                }

                Rect pawnRect = new Rect(rect.x + Padding, y, ColumnWidth, LineHeight);
                Rect stateRect = new Rect(rect.x + Padding + ColumnWidth, y, ColumnWidth, LineHeight);

                // Color code based on stage
                if (state.IsInfectious())
                    GUI.color = new Color(1f, 0.3f, 0.3f); // Red for infectious
                else if (state.Stage == SimulatedDiseaseStage.Incubating || 
                         state.Stage == SimulatedDiseaseStage.PreSymptomaticInfectious)
                    GUI.color = Color.yellow;
                else if (state.Stage == SimulatedDiseaseStage.Exposed)
                    GUI.color = new Color(1f, 0.6f, 0f); // Orange for exposed
                else
                    GUI.color = new Color(0.7f, 0.7f, 0.7f); // Gray for other

                Widgets.Label(pawnRect, pawnName);
                Widgets.Label(stateRect, stateInfo);
                drawn++;
            }

            // Draw settings summary
            y += LineHeight;
            Rect settingsRect = new Rect(rect.x + Padding, y, rect.width - Padding * 2, LineHeight);
            GUI.color = new Color(0.5f, 0.8f, 1f); // Light blue
            string settingsSummary = $"Sim:{DyzePathogenicsMod.Settings?.Enabled} | Out:{DyzePathogenicsMod.Settings?.EnableOutsiderImportation} | Resp:{DyzePathogenicsMod.Settings?.EnableRespiratorySpread}";
            Widgets.Label(settingsRect, settingsSummary);

            // Restore text settings
            Text.Font = oldFont;
            GUI.color = oldColor;
        }

        /// <summary>
        /// Find a pawn by ID in the map.
        /// </summary>
        private static Pawn FindPawnById(Map map, int pawnId)
        {
            var pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                if (pawns[i].thingIDNumber == pawnId)
                {
                    return pawns[i];
                }
            }
            return null;
        }
    }

    /// <summary>
    /// Harmony patch to inject the debug readout rendering.
    /// </summary>
    [HarmonyPatch(typeof(UIRoot_Game), "OnGUI")]
    public static class UIRoot_Game_OnGUI_Patch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            DiseaseDebugReadout.OnGUI();
        }
    }
}