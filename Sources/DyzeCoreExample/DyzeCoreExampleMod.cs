using System;
using UnityEngine;
using Verse;

namespace Dyze.RimWorld.CoreExample
{
    public class DyzeCoreExampleMod: Mod
    {
        public static DyzePathogenicResidueSettings Settings;


        public DyzeCoreExampleMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<DyzePathogenicResidueSettings>();
            Log.Message("[DyzeCoreExample] Mod assembly loaded successfully.");
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

            listing.CheckboxLabeled(
                "Dyze_PathogenicResidue_AffectColonistsOnly_Label".Translate(),
                ref Settings.AffectColonistsOnly,
                "Dyze_PathogenicResidue_AffectColonistsOnly_Desc".Translate()
            );

            listing.GapLine();

            listing.Label(
                "Dyze_PathogenicResidue_CheckInterval_Label"
                    .Translate(Settings.CheckIntervalTicks)
            );

            float interval = listing.Slider(Settings.CheckIntervalTicks, 60f, 5000f);
            Settings.CheckIntervalTicks = Mathf.RoundToInt(interval);

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

            if (listing.ButtonText("Dyze_PathogenicResidue_Reset_Label".Translate()))
            {
                Settings.Enabled = true;
                Settings.AffectColonistsOnly = false;
                Settings.CheckIntervalTicks = 250;
                Settings.SpawnChancePerCheck = 0.08f;
            }

            listing.End();

            Settings.ClampValues();
            Settings.Write();
        }
    }

}

