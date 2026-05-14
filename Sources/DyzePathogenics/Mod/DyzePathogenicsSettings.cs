using System;
using UnityEngine;
using Verse;

namespace Dyze.RimWorld.Pathogenics
{
    /// <summary>
    /// Mod settings for Pathogenics v0.3+.
    /// 
    /// v0.3: Standalone respiratory disease simulation with:
    /// - Hidden state tracking
    /// - Outsider importation
    /// - Player feedback and debugging
    /// - Expanded settings for balancing
    /// </summary>
    public class DyzePathogenicsSettings : ModSettings
    {
        // ===== v0.3 Core Settings =====
        
        /// <summary>
        /// Master toggle for the mod's disease simulation.
        /// When true, the hidden disease state system is active.
        /// </summary>
        public bool Enabled = true;
        
        /// <summary>
        /// Only affect colonists (vs all humanlike pawns).
        /// </summary>
        public bool AffectColonistsOnly = false;

        // ===== v0.3: Feature 7 - Outsider Importation =====

        /// <summary>
        /// Enable outsider importation (disease enters via visitors, traders, raiders, etc.).
        /// </summary>
        public bool EnableOutsiderImportation = true;

        /// <summary>
        /// Chance that an outsider imports the disease (0.0 to 1.0).
        /// </summary>
        public float OutsiderImportChance = 0.15f;

        // ===== v0.3: Feature 6 - Respiratory Spread =====

        /// <summary>
        /// Enable respiratory proximity transmission.
        /// </summary>
        public bool EnableRespiratorySpread = true;

        /// <summary>
        /// Multiplier for exposure gain from transmission events.
        /// Higher values = faster disease spread.
        /// </summary>
        public float ExposureGainMultiplier = 1.0f;

        // ===== v0.3: Feature 8 - Player Feedback =====

        /// <summary>
        /// Show transmission warning when symptoms are detected.
        /// </summary>
        public bool ShowTransmissionWarning = true;

        /// <summary>
        /// Show debug readout overlay on the screen.
        /// </summary>
        public bool ShowDebugReadout = false;

        // ===== Debug Settings =====

        /// <summary>
        /// Enable debug logging for development and balancing.
        /// </summary>
        public bool EnableDebugLogging = false;

        // ===== Default Values for Reset =====

        private const bool DefaultEnabled = true;
        private const bool DefaultAffectColonistsOnly = false;
        private const bool DefaultEnableOutsiderImportation = true;
        private const float DefaultOutsiderImportChance = 0.15f;
        private const bool DefaultEnableRespiratorySpread = true;
        private const float DefaultExposureGainMultiplier = 1.0f;
        private const bool DefaultShowTransmissionWarning = true;
        private const bool DefaultShowDebugReadout = false;
        private const bool DefaultEnableDebugLogging = false;
        
        public override void ExposeData()
        {
            base.ExposeData();
            
            // v0.3 core
            Scribe_Values.Look(ref Enabled, "Enabled", DefaultEnabled);
            Scribe_Values.Look(ref AffectColonistsOnly, "AffectColonistsOnly", DefaultAffectColonistsOnly);
            
            // v0.3: Outsider importation
            Scribe_Values.Look(ref EnableOutsiderImportation, "EnableOutsiderImportation", DefaultEnableOutsiderImportation);
            Scribe_Values.Look(ref OutsiderImportChance, "OutsiderImportChance", DefaultOutsiderImportChance);
            
            // v0.3: Respiratory spread
            Scribe_Values.Look(ref EnableRespiratorySpread, "EnableRespiratorySpread", DefaultEnableRespiratorySpread);
            Scribe_Values.Look(ref ExposureGainMultiplier, "ExposureGainMultiplier", DefaultExposureGainMultiplier);
            
            // v0.3: Player feedback
            Scribe_Values.Look(ref ShowTransmissionWarning, "ShowTransmissionWarning", DefaultShowTransmissionWarning);
            Scribe_Values.Look(ref ShowDebugReadout, "ShowDebugReadout", DefaultShowDebugReadout);
            
            // Debug
            Scribe_Values.Look(ref EnableDebugLogging, "EnableDebugLogging", DefaultEnableDebugLogging);
        }

        public void ClampValues()
        {
            // Clamp import chance between 0 and 1
            if (OutsiderImportChance < 0f) OutsiderImportChance = 0f;
            if (OutsiderImportChance > 1f) OutsiderImportChance = 1f;

            // Clamp exposure multiplier (allow 0.1x to 10x for extreme balancing)
            if (ExposureGainMultiplier < 0.1f) ExposureGainMultiplier = 0.1f;
            if (ExposureGainMultiplier > 10f) ExposureGainMultiplier = 10f;
        }

        /// <summary>
        /// Reset settings to defaults.
        /// </summary>
        public void ResetToDefaults()
        {
            Enabled = DefaultEnabled;
            AffectColonistsOnly = DefaultAffectColonistsOnly;
            EnableOutsiderImportation = DefaultEnableOutsiderImportation;
            OutsiderImportChance = DefaultOutsiderImportChance;
            EnableRespiratorySpread = DefaultEnableRespiratorySpread;
            ExposureGainMultiplier = DefaultExposureGainMultiplier;
            ShowTransmissionWarning = DefaultShowTransmissionWarning;
            ShowDebugReadout = DefaultShowDebugReadout;
            EnableDebugLogging = DefaultEnableDebugLogging;
        }
    }
}