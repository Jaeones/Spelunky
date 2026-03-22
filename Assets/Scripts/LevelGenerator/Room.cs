using System.Collections.Generic;
using UnityEngine;
using Gizmos = Popcron.Gizmos;

namespace Spelunky {

    /// <summary>
    /// </summary>
    public class Room : MonoBehaviour {

        public Vector2 index;

        [HideInInspector] public bool debug;

        public bool top, right, down, left;

        /// <summary>
        /// </summary>
        private void Update() {
            if (!debug) {
                return;
            }

            for (int x = 0; x <= LevelGenerator.RoomWidth; x++) {
                Gizmos.Line(
                    new Vector3(transform.position.x + x * Tile.Width, transform.position.y, 0),
                    new Vector3(transform.position.x + x * Tile.Width, transform.position.y, 0) + Vector3.up * LevelGenerator.RoomHeight * Tile.Height,
                    new Color(1, 1, 1, 0.3f)
                );
            }

            for (int y = 0; y <= LevelGenerator.RoomHeight; y++) {
                Gizmos.Line(
                    new Vector3(transform.position.x, transform.position.y + y * Tile.Height, 0),
                    new Vector3(transform.position.x, transform.position.y + y * Tile.Height, 0) + Vector3.right * LevelGenerator.RoomWidth * Tile.Width,
                    new Color(1, 1, 1, 0.3f)
                );
            }
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        private Tile[] GetRoomTiles() {
            List<Tile> roomTiles = new List<Tile>();

            for (int x = (int)index.x * LevelGenerator.RoomWidth; x < (int)(index.x + 1) * LevelGenerator.RoomWidth; x++) {
                for (int y = (int)index.y * LevelGenerator.RoomHeight; y < (int)(index.y + 1) * LevelGenerator.RoomHeight; y++) {
                    // No tile.
                    if (LevelGenerator.instance.Tiles[x, y] == null) {
                        continue;
                    }

                    roomTiles.Add(LevelGenerator.instance.Tiles[x, y]);
                }
            }

            return roomTiles.ToArray();
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public Tile GetSuitableEntranceOrExitTile() {
            Tile[] roomTiles = GetRoomTiles();
            List<Tile> preferredTiles = new List<Tile>();
            List<Tile> fallbackTiles = new List<Tile>();

            foreach (Tile tile in roomTiles) {
                if (!CanSpawnEntranceOrExitOnTile(tile)) {
                    continue;
                }

                if (LevelGenerator.instance.IsValidEntranceExitTile(tile)) {
                    preferredTiles.Add(tile);
                }
                else if (IsFallbackEntranceExitFloor(tile)) {
                    // Stage-specific naming can drift before all door hints are curated.
                    // Fall back to any solid floor tile in the room so in-place stage transitions stay playable.
                    fallbackTiles.Add(tile);
                }
            }

            if (preferredTiles.Count > 0) {
                return preferredTiles[Random.Range(0, preferredTiles.Count)];
            }

            return fallbackTiles.Count > 0 ? fallbackTiles[Random.Range(0, fallbackTiles.Count)] : null;
        }

        public string GetEntranceExitDebugSummary() {
            Tile[] roomTiles = GetRoomTiles();
            int spawnableTileCount = 0;
            int preferredTileCount = 0;
            int fallbackTileCount = 0;
            HashSet<string> sampleTileNames = new HashSet<string>();

            foreach (Tile tile in roomTiles) {
                if (tile == null) {
                    continue;
                }

                if (sampleTileNames.Count < 6) {
                    sampleTileNames.Add(tile.name);
                }

                if (!CanSpawnEntranceOrExitOnTile(tile)) {
                    continue;
                }

                spawnableTileCount++;
                if (LevelGenerator.instance.IsValidEntranceExitTile(tile)) {
                    preferredTileCount++;
                }
                else if (IsFallbackEntranceExitFloor(tile)) {
                    fallbackTileCount++;
                }
            }

            string sampleSummary = sampleTileNames.Count > 0 ? string.Join(", ", sampleTileNames) : "none";
            return $"room={name} index={index} totalTiles={roomTiles.Length} spawnable={spawnableTileCount} preferred={preferredTileCount} fallback={fallbackTileCount} samples=[{sampleSummary}]";
        }

        private bool CanSpawnEntranceOrExitOnTile(Tile tile) {
            if (tile == null) {
                return false;
            }

            // If there is an empty space above the tile we can spawn a door here, but make sure we don't try to
            // spawn a door out of bounds or so far up it's on the bottom of the room above us.
            int yPositionToCheck = tile.y + 1;
            int roomMaxYPosition = (int)(index.y + 1) * LevelGenerator.RoomHeight - 1;
            return yPositionToCheck < roomMaxYPosition &&
                yPositionToCheck < LevelGenerator.instance.Tiles.GetLength(1) - 1 &&
                LevelGenerator.instance.Tiles[tile.x, yPositionToCheck] == null;
        }

        private static bool IsFallbackEntranceExitFloor(Tile tile) {
            if (tile == null) {
                return false;
            }

            if (tile.CompareTag("Ladder") || tile.CompareTag("OneWayPlatform") || tile.CompareTag("Block")) {
                return false;
            }

            string tileName = tile.name.ToLowerInvariant();
            if (tileName.Contains("arrowtrap") ||
                tileName.Contains("spike") ||
                tileName.Contains("altar") ||
                tileName.Contains("entrance") ||
                tileName.Contains("exit")) {
                return false;
            }

            return tile.hasDecorations ||
                tileName.Contains("dirt") ||
                tileName.Contains("floor") ||
                tileName.Contains("temple") ||
                tileName.Contains("metal") ||
                tileName.Contains("dais");
        }

    }

}
