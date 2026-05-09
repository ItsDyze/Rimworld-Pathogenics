using System;
using UnityEngine;
using Verse;

namespace Dyze.RimWorld.CoreExample
{
    public class DyzePathogenicResidueSettings : ModSettings
    {
        public bool Enabled = true;
        public bool AffectColonistsOnly = false;
        public int CheckIntervalTicks = 250;
        public float SpawnChancePerCheck = 0.08f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref Enabled, "Enabled", true);
            Scribe_Values.Look(ref AffectColonistsOnly, "AffectColonistsOnly", false);
            Scribe_Values.Look(ref CheckIntervalTicks, "CheckIntervalTicks", 250);
            Scribe_Values.Look(ref SpawnChancePerCheck, "SpawnChancePerCheck", 0.08f);

            if(Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                ClampValues();
            }
        }

        public void ClampValues()
        {
            CheckIntervalTicks = Mathf.Clamp(CheckIntervalTicks, 60, 5000);
            SpawnChancePerCheck = Mathf.Clamp(SpawnChancePerCheck, 0f, 1f);
        }
    }
}