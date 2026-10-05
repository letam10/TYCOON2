using UnityEngine;

namespace Tycoon
{
    [CreateAssetMenu(menuName = "TYCOON/Game Catalog")]
    public sealed class GameCatalog : ScriptableObject
    {
        public ModelEntry[] models;
        public string[] libraryKeys;
        public Material[] palette;
        public Font font;
        public Texture2D brandTexture;
        public UnityEngine.Rendering.VolumeProfile lightingProfile;
        public GameObject Model(string key)
        {
            if (models == null) return null;
            foreach (var model in models) if (model.key == key) return model.prefab;
            return null;
        }
    }
}
