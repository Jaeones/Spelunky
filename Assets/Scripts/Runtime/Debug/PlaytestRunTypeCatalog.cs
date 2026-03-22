using UnityEngine;

namespace Spelunky {

    [CreateAssetMenu(fileName = "PlaytestRunTypeCatalog", menuName = "Spelunky/Debug/Playtest Run Type Catalog")]
    public class PlaytestRunTypeCatalog : ScriptableObject {

        public const string ResourcesPath = "PlaytestRunTypeCatalog";

        [Tooltip("Selectable QA run types for the current session.")]
        public string[] runTypes;
    }

}
