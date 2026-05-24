using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Dyze.RimWorld.Pathogenics
{
    /// <summary>
    /// Main mod class for Dyze's Pathogenics v0.3+.
    /// 
    /// Focus: Standalone respiratory disease with hidden state tracking.
    /// Features: Outsider importation, player feedback, debug readout, settings.
    /// </summary>
    public class DyzePathogenicsMod: Mod
    {
        public static DyzePathogenicsSettings Settings;

        public DyzePathogenicsMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<DyzePathogenicsSettings>();

            Harmony harmony = new Harmony("dyze.pathogenics");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            DyzeLog.Message("Mod assembly loaded. v0.3: Disease simulation active with outsider importation.");
        }

        public override string SettingsCategory()
        {
            return "Dyze_Pathogenics_SettingsCategory".Translate().ToString();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Settings.ClampValues();

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            // ===== v0.3 Core Settings =====
            
            listing.Label("Dyze_Pathogenics_Section_Simulation".Translate());
            
            listing.CheckboxLabeled(
                "Dyze_Pathogenics_Enable_Label".Translate(),
                ref Settings.Enabled,
                "Dyze_Pathogenics_Enable_Desc".Translate()
            );
            
            listing.CheckboxLabeled(
                "Dyze_Pathogenics_AffectColonistsOnly_Label".Translate(),
                ref Settings.AffectColonistsOnly,
                "Dyze_Pathogenics_AffectColonistsOnly_Desc".Translate()
            );

            listing.GapLine();
            
            // ===== v0.3: Outsider Importation Settings =====
            
            listing.Label("Dyze_Pathogenics_Section_OutsiderImportation".Translate());
            
            listing.CheckboxLabeled(
                "Dyze_Pathogenics_EnableOutsiderImportation_Label".Translate(),
                ref Settings.EnableOutsiderImportation,
                "Dyze_Pathogenics_EnableOutsiderImportation_Desc".Translate()
            );

            // Outsider import chance slider
            string importChanceLabel = $"Dyze_Pathogenics_OutsiderImportChance_Label".Translate() + $" ({Settings.OutsiderImportChance:P0})";
            listing.Label(importChanceLabel);
            Settings.OutsiderImportChance = listing.Slider(Settings.OutsiderImportChance, 0.01f, 0.5f);

            listing.GapLine();

            // ===== v0.4: Vanilla Disease Event Handling =====

            listing.Label("Dyze_Pathogenics_Section_DiseaseEvents".Translate());

            listing.CheckboxLabeled(
                "Dyze_Pathogenics_DisableAllVanillaDiseaseIncidents_Label".Translate(),
                ref Settings.DisableAllVanillaDiseaseIncidents,
                "Dyze_Pathogenics_DisableAllVanillaDiseaseIncidents_Desc".Translate()
            );

            listing.CheckboxLabeled(
                "Dyze_Pathogenics_DisableIntegratedVanillaDiseaseIncidents_Label".Translate(),
                ref Settings.DisableIntegratedVanillaDiseaseIncidents,
                "Dyze_Pathogenics_DisableIntegratedVanillaDiseaseIncidents_Desc".Translate()
            );

            listing.GapLine();
            
            // ===== v0.3: Respiratory Spread Settings =====
            
            listing.Label("Dyze_Pathogenics_Section_RespiratorySpread".Translate());
            
            listing.CheckboxLabeled(
                "Dyze_Pathogenics_EnableRespiratorySpread_Label".Translate(),
                ref Settings.EnableRespiratorySpread,
                "Dyze_Pathogenics_EnableRespiratorySpread_Desc".Translate()
            );

            // Exposure multiplier slider
            string exposureLabel = $"Dyze_Pathogenics_ExposureGainMultiplier_Label".Translate() + $" ({Settings.ExposureGainMultiplier:F1}x)";
            listing.Label(exposureLabel);
            Settings.ExposureGainMultiplier = listing.Slider(Settings.ExposureGainMultiplier, 0.1f, 5f);

            listing.GapLine();
            
            // ===== v0.3: Player Feedback Settings =====
            
            listing.Label("Dyze_Pathogenics_Section_PlayerFeedback".Translate());
            
            listing.CheckboxLabeled(
                "Dyze_Pathogenics_ShowTransmissionWarning_Label".Translate(),
                ref Settings.ShowTransmissionWarning,
                "Dyze_Pathogenics_ShowTransmissionWarning_Desc".Translate()
            );
            
            listing.CheckboxLabeled(
                "Dyze_Pathogenics_ShowDebugReadout_Label".Translate(),
                ref Settings.ShowDebugReadout,
                "Dyze_Pathogenics_ShowDebugReadout_Desc".Translate()
            );

            listing.GapLine();
            
            // ===== Debug Settings =====
            
            listing.Label("Dyze_Pathogenics_Section_Debug".Translate());
            
            listing.CheckboxLabeled(
                "Dyze_Pathogenics_DebugLogging_Label".Translate(),
                ref Settings.EnableDebugLogging,
                "Dyze_Pathogenics_DebugLogging_Desc".Translate()
            );

            listing.GapLine();
            
            // ===== Action Buttons =====
            
            if (listing.ButtonText("Dyze_Pathogenics_Reset_Label".Translate()))
            {
                Settings.ResetToDefaults();
            }

            listing.GapLine();

            listing.End();

            Settings.ClampValues();
            Settings.Write();
        }
    }
}