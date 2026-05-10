using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Dyze.RimWorld.Pathogenics
{
    /// <summary>
    /// Main mod class for Dyze's Pathogenics v0.2+.
    /// 
    /// Focus: Standalone respiratory disease with hidden state tracking.
    /// The residue system has been completely removed.
    /// </summary>
    public class DyzePathogenicsMod: Mod
    {
        public static DyzePathogenicsSettings Settings;

        public DyzePathogenicsMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<DyzePathogenicsSettings>();

            Harmony harmony = new Harmony("dyze.pathogenics");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            DyzeLog.Message("Mod assembly loaded. v0.2: Disease simulation active.");
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

            // ===== v0.2 Core Settings =====
            
            listing.Label("Disease Simulation");
            
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
            
            // ===== Debug Settings =====
            
            listing.Label("Debug");
            
            listing.CheckboxLabeled(
                "Dyze_Pathogenics_DebugLogging_Label".Translate(),
                ref Settings.EnableDebugLogging,
                "Dyze_Pathogenics_DebugLogging_Desc".Translate()
            );

            listing.GapLine();
            
            // ===== Action Buttons =====
            
            if (listing.ButtonText("Dyze_Pathogenics_Reset_Label".Translate()))
            {
                Settings.Enabled = true;
                Settings.AffectColonistsOnly = false;
                Settings.EnableDebugLogging = false;
            }

            listing.GapLine();

            listing.End();

            Settings.ClampValues();
            Settings.Write();
        }
    }
}