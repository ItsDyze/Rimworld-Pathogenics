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
        public const int TransmissionIntervalTicks = 250;
        public const float MaxTransmissionRadius = 15f;
        public const float BaseExposurePerTick = 0.020f;
        public const float SameRoomIndoorFactor = 1.0f;
        public const float OutdoorFactor = 0.25f;
        public const float DifferentRoomFactor = 0.0f;

        public static float DebugTransmissionMultiplier = 1f;

        private const float DistanceFalloffPower = 2f;
        private const int DebugLogIntervalTicks = 1500;

        public static void ProcessTransmission(Map map)
        {
            if (map == null)
            {
                return;
            }

            if (DyzePathogenicsMod.Settings?.EnableRespiratorySpread != true)
            {
                return;
            }

            int currentTick = Find.TickManager.TicksGame;
            if (currentTick % TransmissionIntervalTicks != 0)
            {
                return;
            }

            PathogenicsMapComponent mapComponent = map.GetComponent<PathogenicsMapComponent>();
            if (mapComponent == null)
            {
                return;
            }

            var allPawns = map.mapPawns.AllPawnsSpawned;
            if (allPawns.Count == 0)
            {
                return;
            }

            List<Pawn> infectiousPawns = null;
            for (int i = 0; i < allPawns.Count; i++)
            {
                Pawn pawn = allPawns[i];
                if (pawn == null || !pawn.Spawned || pawn.Dead)
                {
                    continue;
                }

                if (!InfectiousnessUtility.IsInfectious(pawn))
                {
                    continue;
                }

                if (DyzePathogenicsMod.Settings?.AffectColonistsOnly == true && !pawn.IsColonist)
                {
                    continue;
                }

                infectiousPawns ??= new List<Pawn>();
                infectiousPawns.Add(pawn);
            }

            if (infectiousPawns == null || infectiousPawns.Count == 0)
            {
                return;
            }

            for (int i = 0; i < infectiousPawns.Count; i++)
            {
                Pawn sourcePawn = infectiousPawns[i];
                float sourceInfectiousness = InfectiousnessUtility.GetInfectiousness(sourcePawn);
                if (sourceInfectiousness <= 0f)
                {
                    continue;
                }

                ProcessSourcePawn(sourcePawn, sourceInfectiousness, mapComponent, currentTick);
            }
        }

        private static void ProcessSourcePawn(Pawn sourcePawn, float sourceInfectiousness,
            PathogenicsMapComponent mapComponent, int currentTick)
        {
            if (sourcePawn == null || mapComponent == null)
            {
                return;
            }

            var allPawns = sourcePawn.Map.mapPawns.AllPawnsSpawned;
            IntVec3 sourcePos = sourcePawn.Position;

            for (int i = 0; i < allPawns.Count; i++)
            {
                Pawn targetPawn = allPawns[i];
                if (!IsValidTarget(sourcePawn, targetPawn))
                {
                    continue;
                }

                float distance = sourcePos.DistanceTo(targetPawn.Position);
                if (distance > MaxTransmissionRadius || distance < 0.1f)
                {
                    continue;
                }

                float exposure = CalculateExposure(sourcePawn, targetPawn, distance, sourceInfectiousness);
                if (exposure > 0f)
                {
                    mapComponent.AddExposureToPawn(targetPawn, exposure);
                }

                if (DyzePathogenicsMod.Settings?.EnableDebugLogging == true &&
                    currentTick % DebugLogIntervalTicks < TransmissionIntervalTicks)
                {
                    PawnDiseaseState targetState = PathogenicsGameComponent.Instance?.TryGetDiseaseState(targetPawn);
                    string roomStatus = GetRoomStatus(sourcePawn, targetPawn);
                    bool sourceMasked = MaskUtility.IsWearingMask(sourcePawn);
                    bool targetMasked = MaskUtility.IsWearingMask(targetPawn);
                    string maskStatus = GetMaskDebugStatus(sourceMasked, targetMasked);

                    if (exposure <= 0f)
                    {
                        DyzeLog.Message(sourcePawn.LabelShort + " -> " + targetPawn.LabelShort + ": BLOCKED " +
                            "(dist=" + distance.ToString("F1") + ", " + roomStatus + ", " + maskStatus + ")");
                    }
                    else
                    {
                        DyzeLog.Message(sourcePawn.LabelShort + " -> " + targetPawn.LabelShort + ": " +
                            "+" + exposure.ToString("F4") + " exposure (dist=" + distance.ToString("F1") + ", inf=" + sourceInfectiousness.ToString("F2") + ", " + roomStatus + ", " + maskStatus + ") " +
                            "(target exposure: " + targetState?.Exposure.ToString("F2") + ")");
                    }
                }
            }
        }

        private static string GetMaskDebugStatus(bool sourceMasked, bool targetMasked)
        {
            if (sourceMasked && targetMasked)
            {
                return "both-masked";
            }

            if (sourceMasked)
            {
                return "source-masked";
            }

            if (targetMasked)
            {
                return "target-masked";
            }

            return "no-masks";
        }

        private static bool IsValidTarget(Pawn source, Pawn target)
        {
            if (source == target)
            {
                return false;
            }

            if (target == null || !target.Spawned)
            {
                return false;
            }

            if (target.Dead)
            {
                return false;
            }

            if (!target.RaceProps.Humanlike)
            {
                return false;
            }

            if (DyzePathogenicsMod.Settings?.AffectColonistsOnly == true && !target.IsColonist)
            {
                return false;
            }

            PawnDiseaseState targetState = PathogenicsGameComponent.Instance?.TryGetDiseaseState(target);
            if (targetState != null && targetState.Stage >= SimulatedDiseaseStage.Symptomatic)
            {
                return false;
            }

            return true;
        }

        private static float CalculateExposure(Pawn source, Pawn target, float distance, float sourceInfectiousness)
        {
            float roomFactor = GetRoomFactor(source, target);
            if (roomFactor <= 0f)
            {
                return 0f;
            }

            float distanceFactor = 1f - (distance / MaxTransmissionRadius);
            distanceFactor = (float)Math.Pow(Math.Max(0f, distanceFactor), DistanceFalloffPower);

            float exposureMultiplier = DyzePathogenicsMod.Settings?.ExposureGainMultiplier ?? 1.0f;
            float maskMultiplier = MaskUtility.GetExposureMultiplier(source, target);
            if (maskMultiplier <= 0f)
            {
                return 0f;
            }

            return BaseExposurePerTick * sourceInfectiousness * distanceFactor * roomFactor * DebugTransmissionMultiplier * exposureMultiplier * maskMultiplier;
        }

        private static float GetRoomFactor(Pawn source, Pawn target)
        {
            bool sourceOutdoor = IsOutdoors(source);
            bool targetOutdoor = IsOutdoors(target);

            if (sourceOutdoor || targetOutdoor)
            {
                if (sourceOutdoor && targetOutdoor)
                {
                    return OutdoorFactor;
                }

                if (!sourceOutdoor || !targetOutdoor)
                {
                    return 0f;
                }

                return OutdoorFactor;
            }

            Room sourceRoom = source.GetRoom();
            Room targetRoom = target.GetRoom();
            if (sourceRoom == null || targetRoom == null)
            {
                return 0f;
            }

            if (sourceRoom == targetRoom)
            {
                return SameRoomIndoorFactor;
            }

            return DifferentRoomFactor;
        }

        private static string GetRoomStatus(Pawn source, Pawn target)
        {
            bool sourceOutdoor = IsOutdoors(source);
            bool targetOutdoor = IsOutdoors(target);

            if (sourceOutdoor || targetOutdoor)
            {
                if (sourceOutdoor && targetOutdoor)
                {
                    return "outdoor";
                }

                return "indoor-outdoor-blocked";
            }

            Room sourceRoom = source.GetRoom();
            Room targetRoom = target.GetRoom();
            if (sourceRoom == null || targetRoom == null)
            {
                return "no-room";
            }

            if (sourceRoom == targetRoom)
            {
                return "same-room";
            }

            return "different-rooms-blocked";
        }

        private static bool IsOutdoors(Pawn pawn)
        {
            if (pawn == null || pawn.Map == null)
            {
                return false;
            }

            Room room = pawn.GetRoom();
            return room != null && room.PsychologicallyOutdoors;
        }
    }
}
