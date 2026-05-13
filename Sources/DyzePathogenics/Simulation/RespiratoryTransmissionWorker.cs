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
        /// </summary>
        public const float MaxTransmissionRadius = 5f;
        
        /// <summary>
        /// Base exposure added per transmission tick when at optimal distance/same room.
        /// 0.01 means ~1% exposure per tick, so ~100 ticks (about 17 seconds) to full exposure.
        /// </summary>
        public const float BaseExposurePerTick = 0.008f;
        
        /// <summary>
        /// Exposure multipliers by room condition.
        /// </summary>
        public const float SameRoomIndoorFactor = 1.0f;
        public const float OutdoorFactor = 0.25f;
        public const float DifferentRoomFactor = 0.0f;
        
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

            int currentTick = Find.TickManager.TicksGame;
            if (currentTick % TransmissionIntervalTicks != 0)
                return;

            PathogenicsMapComponent mapComponent = map.GetComponent<PathogenicsMapComponent>();
            if (mapComponent == null)
                return;

            // Get all spawned pawns
            List<Pawn> allPawns = map.mapPawns.AllPawnsSpawned;
            if (allPawns.Count == 0)
                return;

            // Find all infectious pawns
            List<Pawn> infectiousPawns = null;
            for (int i = 0; i < allPawns.Count; i++)
            {
                Pawn pawn = allPawns[i];
                if (pawn == null || !pawn.Spawned || pawn.Dead)
                    continue;

                if (InfectiousnessUtility.IsInfectious(pawn))
                {
                    infectiousPawns ??= new List<Pawn>();
                    infectiousPawns.Add(pawn);
                }
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
            List<Pawn> allPawns = sourcePawn.Map.mapPawns.AllPawnsSpawned;
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
            distanceFactor = MathF.Pow(Math.Max(0f, distanceFactor), DistanceFalloffPower);

            // Final exposure formula:
            // exposure = baseExposure × sourceInfectiousness × distanceFactor × roomFactor
            float exposure = BaseExposurePerTick * sourceInfectiousness * distanceFactor * roomFactor;

            return exposure;
        }

        /// <summary>
        /// Get the room factor based on whether source and target share the same room.
        /// Returns 0 if different rooms (no transmission through walls).
        /// </summary>
        private static float GetRoomFactor(Pawn source, Pawn target)
        {
            // Check if they're outdoors
            bool sourceOutdoor = source.Position.GetZone(source.Map) == ZoneManager.ZoneType.Outdoor;
            bool targetOutdoor = target.Position.GetZone(target.Map) == ZoneManager.ZoneType.Outdoor;

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
            bool sourceOutdoor = source.Position.GetZone(source.Map) == ZoneManager.ZoneType.Outdoor;
            bool targetOutdoor = target.Position.GetZone(target.Map) == ZoneManager.ZoneType.Outdoor;

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
    }
}