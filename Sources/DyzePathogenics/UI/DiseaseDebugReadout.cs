using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Dyze.RimWorld.Pathogenics.Integration;

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
        private const float ColumnWidth = 160f;

        /// <summary>
        /// Render the debug readout if enabled.
        /// Uses OnGUI for reliable rendering.
        /// </summary>
        public static void OnGUI()
        {
            if (DyzePathogenicsMod.Settings?.ShowDebugReadout != true)
                return;

            // Only render during gameplay
            if (Find.CurrentMap == null)
                return;

            // Find the current map
            Map map = Find.CurrentMap;
            if (map == null)
                return;

            PathogenicsMapComponent mapComponent = map.GetComponent<PathogenicsMapComponent>();
            if (mapComponent == null)
                return;

            List<KeyValuePair<Pawn, PawnDiseaseState>> activeStates = mapComponent.GetActiveDiseaseStates();
            if (activeStates.Count == 0)
                return;

            // Calculate required height
            int pawnCount = Math.Min(activeStates.Count, 15); // Limit to 15 to avoid overflow
            float height = Padding + (pawnCount + 3) * LineHeight + Padding;
            float width = ColumnWidth * 4 + Padding * 2;

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
            Rect diseaseHeaderRect = new Rect(rect.x + Padding + ColumnWidth * 2, y, ColumnWidth, LineHeight);
            Rect maskHeaderRect = new Rect(rect.x + Padding + ColumnWidth * 3, y, ColumnWidth, LineHeight);
            Widgets.Label(pawnHeaderRect, "Pawn");
            Widgets.Label(stateHeaderRect, "State (Exposure)");
            Widgets.Label(diseaseHeaderRect, "Disease");
            Widgets.Label(maskHeaderRect, "Mask"); // v0.3.1: New mask column

            // Draw pawn states
            int drawn = 0;
            foreach (var kvp in activeStates)
            {
                if (drawn >= pawnCount)
                    break;

                y += LineHeight;

                Pawn pawn = kvp.Key;
                PawnDiseaseState state = kvp.Value;

                string pawnName = pawn.LabelShort;
                string stateInfo = state.GetStageLabel();
                string diseaseInfo = GetDiseaseLabel(state);

                // Add exposure info if relevant
                if (state.Stage == SimulatedDiseaseStage.Exposed)
                {
                    stateInfo = $"{state.Exposure:F2}/1.0";
                }

                // v0.3.1: Get mask status
                bool isMasked = Simulation.MaskUtility.IsWearingMask(pawn);
                string maskInfo = isMasked ? "YES" : "-";

                Rect pawnRect = new Rect(rect.x + Padding, y, ColumnWidth, LineHeight);
                Rect stateRect = new Rect(rect.x + Padding + ColumnWidth, y, ColumnWidth, LineHeight);
                Rect diseaseRect = new Rect(rect.x + Padding + ColumnWidth * 2, y, ColumnWidth, LineHeight);
                Rect maskRect = new Rect(rect.x + Padding + ColumnWidth * 3, y, ColumnWidth, LineHeight);

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
                Widgets.Label(diseaseRect, diseaseInfo);
                
                // v0.3.1: Show mask status in separate color
                GUI.color = isMasked ? new Color(0.3f, 1f, 0.3f) : new Color(0.5f, 0.5f, 0.5f);
                Widgets.Label(maskRect, maskInfo);
                
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

        private static string GetDiseaseLabel(PawnDiseaseState state)
        {
            if (state == null)
            {
                return "<null>";
            }

            PathogenicsDiseaseProfile profile = PathogenicsDiseaseRegistry.GetProfile(state);
            return profile?.HediffDef?.label?.CapitalizeFirst() ?? state.DiseaseDefName ?? "Unknown";
        }

    }

    /// <summary>
    /// Harmony patch to inject the debug readout rendering.
    /// </summary>
    [HarmonyPatch(typeof(UIRoot_Play), nameof(UIRoot_Play.UIRootOnGUI))]
    public static class UIRoot_Play_UIRootOnGUI_Patch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            DiseaseDebugReadout.OnGUI();
        }
    }
}
