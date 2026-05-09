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
        public bool RequireMovement = true;
        public int MinTicksBetweenResiduePerPawn = 1000;
        public bool EnableDebugLogging = false;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref Enabled, "Enabled", true);
            Scribe_Values.Look(ref AffectColonistsOnly, "AffectColonistsOnly", false);
            Scribe_Values.Look(ref CheckIntervalTicks, "CheckIntervalTicks", 250);
            Scribe_Values.Look(ref SpawnChancePerCheck, "SpawnChancePerCheck", 0.08f);
            Scribe_Values.Look(ref RequireMovement, "RequireMovement", true);
            Scribe_Values.Look(ref MinTicksBetweenResiduePerPawn, "MinTicksBetweenResiduePerPawn", 1000);
            Scribe_Values.Look(ref EnableDebugLogging, "enableDebugLogging", false);
            if(Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                ClampValues();
            }
        }

        public void ClampValues()
        {
            CheckIntervalTicks = Mathf.Clamp(CheckIntervalTicks, 60, 5000);
            SpawnChancePerCheck = Mathf.Clamp(SpawnChancePerCheck, 0f, 1f);
            MinTicksBetweenResiduePerPawn = Mathf.Clamp(MinTicksBetweenResiduePerPawn, 0, 10000);
        }
    }
}