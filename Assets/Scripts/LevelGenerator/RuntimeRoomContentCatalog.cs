using UnityEngine;

namespace Spelunky {

    [CreateAssetMenu(fileName = "RuntimeRoomContentCatalog", menuName = "Spelunky/Runtime Room Content Catalog")]
    public class RuntimeRoomContentCatalog : ScriptableObject {

        [Header("Prefab Pools")]
        public GameObject keyPrefab;
        public GameObject chestPrefab;
        public GameObject cratePrefab;
        public GameObject jarPrefab;
        public GameObject[] accessoryPrefabs;
        public GameObject[] equipmentPrefabs;
        public GameObject[] throwablePrefabs;
        public GameObject[] treasurePrefabs;

        [Header("Spawn Counts")]
        [Min(0)] public int minKeyChestSetCount = 1;
        [Min(0)] public int maxKeyChestSetCount = 3;
        [Min(0)] public int minAccessorySpawnCount = 0;
        [Min(0)] public int maxAccessorySpawnCount = 1;
        [Min(0)] public int minEquipmentSpawnCount = 0;
        [Min(0)] public int maxEquipmentSpawnCount = 1;
        [Min(0)] public int minCrateSpawnCount = 2;
        [Min(0)] public int maxCrateSpawnCount = 3;
        [Min(0)] public int minJarSpawnCount = 2;
        [Min(0)] public int maxJarSpawnCount = 3;
        [Min(0)] public int minThrowableSpawnCount = 2;
        [Min(0)] public int maxThrowableSpawnCount = 4;
        [Min(0)] public int minTreasureSpawnCount = 2;
        [Min(0)] public int maxTreasureSpawnCount = 4;
        [Min(0)] public float minimumKeyChestPairDistance = 96f;

    }

}
