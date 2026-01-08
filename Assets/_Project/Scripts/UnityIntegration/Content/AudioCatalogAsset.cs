using System;
using UnityEngine;

namespace AshNCircuit.UnityIntegration.Content
{
    [CreateAssetMenu(menuName = "AshNCircuit/Content/AudioCatalog", fileName = "AudioCatalog")]
    public sealed class AudioCatalogAsset : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public string AudioId = "";
            public AudioClip AudioClip = null!;
        }

        public Entry[] Entries = Array.Empty<Entry>();
    }
}
