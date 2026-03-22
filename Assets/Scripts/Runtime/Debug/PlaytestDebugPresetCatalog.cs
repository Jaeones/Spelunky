using System;
using UnityEngine;

namespace Spelunky {

    [Serializable]
    public struct PlaytestDebugPresetData {

        public string label;
        public int stageIndex;
        public int health;
        public int bombs;
        public int ropes;
        [TextArea(2, 4)] public string goal;
    }

    [CreateAssetMenu(fileName = "PlaytestDebugPresetCatalog", menuName = "Spelunky/Debug/Playtest Preset Catalog")]
    public class PlaytestDebugPresetCatalog : ScriptableObject {

        public const string ResourcesPath = "PlaytestDebugPresetCatalog";

        [Tooltip("Quick playtest presets used by the runtime debug overlay.")]
        public PlaytestDebugPresetData[] presets;
    }

}
