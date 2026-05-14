using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Dyze.RimWorld.Pathogenics;

namespace Dyze.RimWorld.Pathogenics.Simulation
{
    /// <summary>
    /// Worker for respiratory proximity transmission (v0.2 feature 6).
    /// 
    /// Run periodically from PathogenicsMapComponent to allow infectious pawns
    /// to expose nearby valid pawns through shared air.
    /// </summary>
    public static class RespiratoryTransmissionWorker
    {
        // ===== CONFIGURATION: Transmission parameters =====
        
        /// <summary>
        /// Interval between transmission checks (250 ticks ≈ 4 seconds).
        /// </summary>
        public const int TransmissionIntervalTicks = 250;
        
        /// <summary>
        /// Maximum distance for proximity transmission in tiles.
        /// v0.2.2: Increased from 5 to 15 for more colony-realistic spread.
        /// </summary>
        public const float MaxTransmissionRadius = 15f;
        
        /// <summary>
        /// Base exposure added per transmission tick when at optimal distance/same room.
        /// v0.2.2: Increased from 0.008 to 0.020 for realistic gameplay pacing.
        /// 0.020 × infectiousness × distanceFactor × roomFactor per 250-tick interval.
        /// </summary>
        public const float BaseExposurePerTick = 0.020f;
        
        /// <summary>
        /// Exposure multipliers by room condition.
        /// </summary>
        public const float SameRoomIndoorFactor = 1.0f;
        public const float OutdoorFactor = 0.25f;
        public const float DifferentRoomFactor = 0.0f;
        
        /// <summary>
        /// Debug/test multiplier for accelerated transmission testing.
        /// v0.2.2: Set to 10x for fast testing, 1x for normal gameplay.
        /// </summary>
        public static float DebugTransmissionMultiplier = 1f;
        
        /// <summary>
        /// Distance falloff parameters - exposure drops with distance squared.
        /// </summary>
        private const float DistanceFalloffPower = 2f;

        /// <summary>
        /// Debug logging interval - don't spam every tick.
        /// </summary>
        private const int DebugLogIntervalTicks = 1500; // ~25 seconds

        /// <summary>
        /// Process respiratory transmission for all pawns on the map.
        /// Should be called from MapComponentTick at regular intervals.
        /// </summary>
        public static void ProcessTransmission(Map map)
        {
            if (map == null)
                return;

            // v0.3: Check if respiratory spread is enabled
            if (DyzePathogenicsMod.Settings?.EnableRespiratorySpread != true)
                return;

            int currentTick = Find.TickManager.TicksGame;
            if (currentTick % TransmissionIntervalTicks != 0)
                return;

            PathogenicsMapComponent mapComponent = map.GetComponent<PathogenicsMapComponent>();
            if (mapComponent == null)
                return;

            // Get all spawned pawns
            var allPawns = map.mapPawns.AllPawnsSpawned;
            if (allPawns.Count == 0)
                return;

            // Find all infectious pawns
            List<Pawn> infectiousPawns = null;
            for (int i = 0; i < allPawns.Count; i++)
            {
                Pawn pawn = allPawns[i];
                if (pawn == null || !pawn.Spawned || pawn.Dead)
                    continue;

                if (!InfectiousnessUtility.IsInfectious(pawn))
                    continue;

                // Apply AffectColonistsOnly filter to source pawns too
                if (DyzePathogenicsMod.Settings?.AffectColonistsOnly == true && !pawn.IsColonist)
                    continue;

                infectiousPawns ??= new List<Pawn>();
                infectiousPawns.Add(pawn);
            }

            if (infectiousPawns == null || infectiousPawns.Count == 0)
                return;

            // Process each infectious pawn
            for (int i = 0; i < infectiousPawns.Count; i++)
            {
                Pawn sourcePawn = infectiousPawns[i];
                float sourceInfectiousness = InfectiousnessUtility.GetInfectiousness(sourcePawn);
                
                if (sourceInfectiousness <= 0f)
                    continue;

                // Find nearby valid targets
                ProcessSourcePawn(sourcePawn, sourceInfectiousness, mapComponent, currentTick);
            }
        }

        /// <summary>
        /// Process transmission from a single infectious source pawn to nearby targets.
        /// </summary>
        private static void ProcessSourcePawn(Pawn sourcePawn, float sourceInfectiousness, 
            PathogenicsMapComponent mapComponent, int currentTick)
        {
            if (sourcePawn == null || mapComponent == null)
                return;
            var allPawns = sourcePawn.Map.mapPawns.AllPawnsSpawned;
            IntVec3 sourcePos = sourcePawn.Position;

            for (int i = 0; i < allPawns.Count; i++)
            {
                Pawn targetPawn = allPawns[i];
                
                // Skip self and invalid targets
                if (!IsValidTarget(sourcePawn, targetPawn))
                    continue;

                // Check distance
                float distance = sourcePos.DistanceTo(targetPawn.Position);
                if (distance > MaxTransmissionRadius || distance < 0.1f)
                    continue;

                // Calculate exposure
                float exposure = CalculateExposure(sourcePawn, targetPawn, distance, sourceInfectiousness);
                if (exposure <= 0f)
                    continue;

                // Add exposure to target through the map component
                mapComponent.AddExposureToPawn(targetPawn, exposure);

                // Debug log occasionally
                if (DyzePathogenicsMod.Settings?.EnableDebugLogging == true && 
                    currentTick % DebugLogIntervalTicks < TransmissionIntervalTicks)
                {
                    PawnDiseaseState targetState = mapComponent.GetDiseaseState(targetPawn);
                    string roomStatus = GetRoomStatus(sourcePawn, targetPawn);
                    DyzeLog.Message($"{sourcePawn.LabelShort} -> {targetPawn.LabelShort}: " +
                        $"+{exposure:F4} exposure (dist={distance:F1}, inf={sourceInfectiousness:F2}, {roomStatus}) " +
                        $"(target exposure: {targetState?.Exposure:F2})");
                }
            }
        }

        /// <summary>
        /// Check if a target pawn is valid for transmission.
        /// </summary>
        private static bool IsValidTarget(Pawn source, Pawn target)
        {
            // Skip self
            if (source == target)
                return false;

            // Skip null or not spawned
            if (target == null || !target.Spawned)
                return false;

            // Skip dead pawns
            if (target.Dead)
                return false;

            // Only humanlike pawns
            if (!target.RaceProps.Humanlike)
                return false;

            // Check AffectColonistsOnly setting
            if (DyzePathogenicsMod.Settings?.AffectColonistsOnly == true && !target.IsColonist)
                return false;

            // Skip if target is already fully progressed in disease (symptomatic onwards)
            // They could still get re-infected but for simplicity we skip
            PathogenicsMapComponent mapComponent = target.Map?.GetComponent<PathogenicsMapComponent>();
            if (mapComponent != null)
            {
                PawnDiseaseState targetState = mapComponent.GetDiseaseState(target);
                if (targetState != null && targetState.Stage >= SimulatedDiseaseStage.Symptomatic)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Calculate the exposure amount for a target based on proximity and room factors.
        /// </summary>
        private static float CalculateExposure(Pawn source, Pawn target, float distance, float sourceInfectiousness)
        {
            // Get room factor based on room conditions
            float roomFactor = GetRoomFactor(source, target);
            if (roomFactor <= 0f)
                return 0f;

            // Calculate distance factor (inverse square falloff)
            float distanceFactor = 1f - (distance / MaxTransmissionRadius);
            distanceFactor = (float)Math.Pow(Math.Max(0f, distanceFactor), DistanceFalloffPower);

            // v0.3: Get exposure multiplier from settings
            float exposureMultiplier = DyzePathogenicsMod.Settings?.ExposureGainMultiplier ?? 1.0f;

            // Final exposure formula:
            // exposure = baseExposure × sourceInfectiousness × distanceFactor × roomFactor × debugMultiplier × exposureMultiplier
            float exposure = BaseExposurePerTick * sourceInfectiousness * distanceFactor * roomFactor * DebugTransmissionMultiplier * exposureMultiplier;

            return exposure;
        }

        /// <summary>
        /// Get the room factor based on whether source and target share the same room.
        /// Returns 0 if different rooms (no transmission through walls).
        /// </summary>
        private static float GetRoomFactor(Pawn source, Pawn target)
        {
            // Check if they're outdoors
            bool sourceOutdoor = IsOutdoors(source);
            bool targetOutdoor = IsOutdoors(target);

            // If either is outdoors, use outdoor factor
            if (sourceOutdoor || targetOutdoor)
            {
                // If both outdoors, still allow reduced transmission
                if (sourceOutdoor && targetOutdoor)
                    return OutdoorFactor;
                
                // One outdoor, one indoor - no transmission through building walls
                // But if they're on the same outdoor tile/zone, allow outdoor factor
                if (!sourceOutdoor || !targetOutdoor)
                {
                    // Different zones - one indoor, one outdoor = no transmission
                    return 0f;
                }
                
                return OutdoorFactor;
            }

            // Both indoor - check same room
            Room sourceRoom = source.GetRoom();
            Room targetRoom = target.GetRoom();

            // If either has no room (shouldn't happen for indoor), skip
            if (sourceRoom == null || targetRoom == null)
                return 0f;

            // Same room = full transmission
            if (sourceRoom == targetRoom)
                return SameRoomIndoorFactor;

            // Different rooms = no transmission (walls block respiratory transmission)
            return DifferentRoomFactor;
        }

        /// <summary>
        /// Get a string describing the room status for debug logging.
        /// </summary>
        private static string GetRoomStatus(Pawn source, Pawn target)
        {
            bool sourceOutdoor = IsOutdoors(source);
            bool targetOutdoor = IsOutdoors(target);

            if (sourceOutdoor || targetOutdoor)
            {
                if (sourceOutdoor && targetOutdoor)
                    return "outdoor";
                return "indoor-outdoor-blocked";
            }

            Room sourceRoom = source.GetRoom();
            Room targetRoom = target.GetRoom();
            
            if (sourceRoom == null || targetRoom == null)
                return "no-room";
            
            if (sourceRoom == targetRoom)
                return "same-room";
            
            return "different-rooms-blocked";
        }

        private static bool IsOutdoors(Pawn pawn)
        {
            if (pawn == null || pawn.Map == null)
                return false;

            return !pawn.Position.Roofed(pawn.Map);
        }
    }
}