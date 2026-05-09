using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Dyze.RimWorld.PathogenicResidue
{
    public class DyzePathogenicResidueMod: Mod
    {
        public static DyzePathogenicResidueSettings Settings;


        public DyzePathogenicResidueMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<DyzePathogenicResidueSettings>();


            Harmony harmony = new Harmony("dyze.pathogenicresidue");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            DyzeLog.Message("Mod assembly loaded successfully.");
        }

        public override string SettingsCategory()
        {
            return "Dyze_PathogenicResidue_SettingsCategory".Translate().ToString();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Settings.ClampValues();

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.CheckboxLabeled(
                "Dyze_PathogenicResidue_Enable_Label".Translate(),
                ref Settings.Enabled,
                "Dyze_PathogenicResidue_Enable_Desc".Translate()
            );
            
            listing.GapLine();

            listing.CheckboxLabeled(
                "Dyze_PathogenicResidue_AffectColonistsOnly_Label".Translate(),
                ref Settings.AffectColonistsOnly,
                "Dyze_PathogenicResidue_AffectColonistsOnly_Desc".Translate()
            );

            listing.CheckboxLabeled(
                "Dyze_PathogenicResidue_RequireMovement_Label".Translate(),
                ref Settings.RequireMovement,
                "Dyze_PathogenicResidue_RequireMovement_Desc".Translate()
            );

            listing.GapLine();

            listing.Label(
                "Dyze_PathogenicResidue_CheckInterval_Label"
                    .Translate(Settings.CheckIntervalTicks)
            );

            float interval = listing.Slider(Settings.CheckIntervalTicks, 60f, 5000f);
            Settings.CheckIntervalTicks = Mathf.RoundToInt(interval);

            listing.Label(
                "Dyze_PathogenicResidue_Cooldown_Label"
                    .Translate(Settings.MinTicksBetweenResiduePerPawn)
            );

            float cooldown = listing.Slider(Settings.MinTicksBetweenResiduePerPawn, 0f, 10000f);
            Settings.MinTicksBetweenResiduePerPawn = Mathf.RoundToInt(cooldown);


            listing.Label(
                "Dyze_PathogenicResidue_SpawnChance_Label"
                    .Translate(Math.Round(Settings.SpawnChancePerCheck * 100f, 1))
            );

            Settings.SpawnChancePerCheck = listing.Slider(
                Settings.SpawnChancePerCheck,
                0f,
                1f
            );

            listing.GapLine();

            listing.CheckboxLabeled(
                "Dyze_PathogenicResidue_DebugLogging_Label".Translate(),
                ref Settings.EnableDebugLogging,
                "Dyze_PathogenicResidue_DebugLogging_Desc".Translate()
            );

            listing.CheckboxLabeled(
                "Dyze_PathogenicResidue_UseMovementHook_Label".Translate(),
                ref Settings.UseMovementHook,
                "Dyze_PathogenicResidue_UseMovementHook_Desc".Translate()
            );

            listing.GapLine();

            if (listing.ButtonText("Dyze_PathogenicResidue_Reset_Label".Translate()))
            {
                Settings.Enabled = true;
                Settings.AffectColonistsOnly = false;
                Settings.RequireMovement = true;
                Settings.CheckIntervalTicks = 250;
                Settings.MinTicksBetweenResiduePerPawn = 1000;
                Settings.SpawnChancePerCheck = 0.08f;
                Settings.EnableDebugLogging = false;
                Settings.UseMovementHook = true;
            }

            listing.GapLine();

            if (listing.ButtonText("Dyze_PathogenicResidue_ClearAll_Label".Translate()))
            {
                Find.WindowStack.Add(new Dialog_MessageBox(
                    "Dyze_PathogenicResidue_ClearAll_Confirm".Translate(),
                    "Confirm".Translate(),
                    delegate
                    {
                        int removed = DyzePathogenicResidueUtility.ClearAllResidueFromLoadedMaps();

                        Messages.Message(
                            "Dyze_PathogenicResidue_ClearAll_Done".Translate(removed),
                            MessageTypeDefOf.PositiveEvent,
                            false
                        );
                    },
                    "Cancel".Translate(),
                    null
                ));
            }

            listing.GapLine();

            listing.End();

            Settings.ClampValues();
            Settings.Write();
        }
    }

}

