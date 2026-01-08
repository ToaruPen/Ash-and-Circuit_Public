using System;
using UnityEngine;

namespace AshNCircuit.UnityIntegration.Content
{
    [CreateAssetMenu(menuName = "AshNCircuit/Content/SpriteCatalog", fileName = "SpriteCatalog")]
    public sealed class SpriteCatalogAsset : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public string SpriteId = "";
            public Sprite Sprite = null!;
        }

        public Entry[] Entries = Array.Empty<Entry>();
    }
}

