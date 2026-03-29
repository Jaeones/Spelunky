using UnityEngine;
using System;

namespace Spelunky {

    public enum StageMainPathStyle {
        Classic,
        VerticalDescent
    }

    public enum SpecialRoomType {
        Trap,
        Sacrifice,
        FinalAltar
    }

    [System.Serializable]
    public class StageSpecialRoomPool {

        public SpecialRoomType roomType = SpecialRoomType.Trap;
        public Room[] rooms;

    }

    /// <summary>
    /// Minimal per-stage configuration asset.
    /// Temporary generation knobs live here until the full 4-stage flow data is ready.
    /// </summary>
    [CreateAssetMenu(fileName = "StageDefinition", menuName = "Spelunky/Stage Definition")]
    public class StageDefinition : ScriptableObject {

        [Header("Identity")]
        [Min(1)] public int stageNumber = 1;
        public string stageId = "stage-01";
        public string displayName = "Stage 1";

        [Header("Pacing")]
        public int targetMinutesMin = 3;
        public int targetMinutesMax = 5;
        [Min(1)] public int roomGridWidth = 4;
        [Min(1)] public int roomGridHeight = 4;

        [Header("Generation Rules")]
        public StageMainPathStyle mainPathStyle = StageMainPathStyle.Classic;
        [Range(0f, 1f)] public float mainPathDownChance = 0.2f;
        [Range(0f, 1f)] public float trapRoomChance = 0.1f;
        [Range(0f, 1f)] public float sacrificeRoomChance = 0.1f;
        [Min(0)] public int maxTrapRooms = 1;
        [Min(0)] public int maxSacrificeRooms = 1;
        public string[] mainPathPreferredRoomNameHints;
        public string[] entranceExitTileNameHints = { "Dirt" };
        public bool clearEnemiesFromStartRoom = true;
        public bool clearImmediateHazardsNearEntrance = true;
        [Min(0)] public int entranceSafetyRadiusTiles = 4;

        [Header("Background")]
        public string backgroundPrefabName = "Background";
        public string[] backgroundDecalPrefabNames = { "BackgroundDecal", "BackgroundDecal_2" };
        [Range(0f, 1f)] public float backgroundDecalChance = 0.1f;

        [Header("Rooms")]
        public Room[] normalRooms;
        public StageSpecialRoomPool[] specialRoomPools;
        public bool allowLegacyNormalRoomFallback = true;
        public bool allowLegacySpecialRoomFallback = true;

        [Header("Presentation")]
        public AudioClip musicTrack;
        public AudioClip ambientLoop;

        [Header("Flags")]
        public bool isFinalStage;

        public bool MatchesStageNumber(int requestedStageNumber) {
            return stageNumber == requestedStageNumber;
        }

        public bool MatchesStageId(string requestedStageId) {
            return !string.IsNullOrWhiteSpace(requestedStageId) &&
                string.Equals(stageId, requestedStageId, StringComparison.OrdinalIgnoreCase);
        }

        public bool MatchesStageRequest(int requestedStageNumber, string requestedStageId) {
            bool hasStageNumber = requestedStageNumber > 0;
            bool hasStageId = !string.IsNullOrWhiteSpace(requestedStageId);

            if (hasStageNumber && !MatchesStageNumber(requestedStageNumber)) {
                return false;
            }

            if (hasStageId && !MatchesStageId(requestedStageId)) {
                return false;
            }

            return hasStageNumber || hasStageId;
        }

        public bool HasNormalRooms() {
            return normalRooms != null && normalRooms.Length > 0;
        }

        public bool HasSpecialRoomPool(SpecialRoomType roomType) {
            Room[] roomPool = GetSpecialRoomPool(roomType);
            return roomPool != null && roomPool.Length > 0;
        }

        public string GetDebugSummary() {
            int trapRoomCount = GetSpecialRoomPool(SpecialRoomType.Trap)?.Length ?? 0;
            int sacrificeRoomCount = GetSpecialRoomPool(SpecialRoomType.Sacrifice)?.Length ?? 0;
            int finalAltarRoomCount = GetSpecialRoomPool(SpecialRoomType.FinalAltar)?.Length ?? 0;
            return $"Stage {stageNumber}: {displayName} | Grid {roomGridWidth}x{roomGridHeight} | Path {mainPathStyle}/{mainPathDownChance:0.00} | RouteHints {GetHintCount(mainPathPreferredRoomNameHints)} | Safe {entranceSafetyRadiusTiles}t | Normal {GetRoomCount(normalRooms)} | Trap {trapRoomCount}x{maxTrapRooms} | Sacrifice {sacrificeRoomCount}x{maxSacrificeRooms} | FinalAltar {finalAltarRoomCount}";
        }

        public Room[] GetSpecialRoomPool(SpecialRoomType roomType) {
            if (specialRoomPools == null) {
                return null;
            }

            foreach (StageSpecialRoomPool specialRoomPool in specialRoomPools) {
                if (specialRoomPool == null || specialRoomPool.roomType != roomType) {
                    continue;
                }

                return specialRoomPool.rooms;
            }

            return null;
        }

        private static int GetRoomCount(Room[] roomPool) {
            return roomPool != null ? roomPool.Length : 0;
        }

        private static int GetHintCount(string[] hints) {
            return hints != null ? hints.Length : 0;
        }
    }

}
