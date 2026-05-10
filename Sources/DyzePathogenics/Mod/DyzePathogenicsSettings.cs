using System;
using UnityEngine;
using Verse;

namespace Dyze.RimWorld.Pathogenics
{
    /// <summary>
    /// Mod settings for Pathogenics v0.2+.
    /// 
    /// v0.2: Standalone respiratory disease simulation with hidden state tracking.
    /// The residue system has been completely removed.
    /// </summary>
    public class DyzePathogenicsSettings : ModSettings
    {
        // ===== v0.2 Core Settings =====
        
        /// <summary>
        /// Master toggle for the mod's disease simulation.
        /// When true, the hidden disease state system is active.
        /// </summary>
        public bool Enabled = true;
        
        /// <summary>
        /// Only affect colonists (vs all humanlike pawns).
        /// </summary>
        public bool AffectColonistsOnly = false;
        
        /// <summary>
        /// Enable debug logging for development and balancing.
        /// </summary>
        public bool EnableDebugLogging = false;
        
        public override void ExposeData()
        {
            base.ExposeData();
            
            // v0.2 core
            Scribe_Values.Look(ref Enabled, "Enabled", true);
            Scribe_Values.Look(ref AffectColonistsOnly, "AffectColonistsOnly", false);
            Scribe_Values.Look(ref EnableDebugLogging, "enableDebugLogging", false);
        }

        public void ClampValues()
        {
            // No clamping needed for current settings
        }
    }
}