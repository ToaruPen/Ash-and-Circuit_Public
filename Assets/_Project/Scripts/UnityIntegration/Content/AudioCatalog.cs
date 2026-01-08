using System;
using System.Collections.Generic;
using UnityEngine;

namespace AshNCircuit.UnityIntegration.Content
{
    /// <summary>
    /// audio_id → AudioClip を解決するカタログ。
    /// JSON-only / fail-fast 運用のため、欠落は例外として検出する。
    /// </summary>
    public sealed class AudioCatalog
    {
        private static AudioCatalog _cached = null!;
        private static bool _hasCached;

        private readonly IReadOnlyDictionary<string, AudioClip> _clipsById;

        private AudioCatalog(IReadOnlyDictionary<string, AudioClip> clipsById)
        {
            _clipsById = clipsById;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache()
        {
            _cached = null!;
            _hasCached = false;
        }

        public static AudioCatalog LoadFromResources()
        {
            if (_hasCached)
            {
                return _cached;
            }

            var loaded = LoadFromResourcesUncached();
            _cached = loaded;
            _hasCached = true;
            return loaded;
        }

        private static AudioCatalog LoadFromResourcesUncached()
        {
            var assets = Resources.LoadAll<AudioCatalogAsset>("AudioCatalogs");
            if (assets == null || assets.Length == 0)
            {
                throw new InvalidOperationException(
                    "AudioCatalog: Resources/AudioCatalogs 配下に AudioCatalogAsset が見つかりません。ScriptableObject を配置してください。");
            }

            var map = new Dictionary<string, AudioClip>(StringComparer.Ordinal);

            foreach (var asset in assets)
            {
                if (asset == null)
                {
                    throw new InvalidOperationException("AudioCatalog: AudioCatalogAsset が null です。Resources/AudioCatalogs 配下のアセットを確認してください。");
                }

                if (asset.Entries == null || asset.Entries.Length == 0)
                {
                    throw new InvalidOperationException($"AudioCatalog: Entries が空です（asset={asset.name}）。");
                }

                foreach (var entry in asset.Entries)
                {
                    if (entry == null)
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(entry.AudioId))
                    {
                        throw new InvalidOperationException($"AudioCatalog: AudioId が空のエントリがあります（asset={asset.name}）。");
                    }

                    if (entry.AudioClip == null)
                    {
                        throw new InvalidOperationException($"AudioCatalog: AudioClip が未設定です（AudioId={entry.AudioId}, asset={asset.name}）。");
                    }

                    if (map.ContainsKey(entry.AudioId))
                    {
                        throw new InvalidOperationException($"AudioCatalog: AudioId が重複しています: {entry.AudioId}");
                    }

                    map.Add(entry.AudioId, entry.AudioClip);
                }
            }

            if (map.Count == 0)
            {
                throw new InvalidOperationException("AudioCatalog: 有効な Entries が 0 件です。");
            }

            return new AudioCatalog(map);
        }

        public AudioClip GetClipOrThrow(string audioId)
        {
            if (string.IsNullOrEmpty(audioId))
            {
                throw new ArgumentException("AudioCatalog: audioId が空です。", nameof(audioId));
            }

            if (!_clipsById.TryGetValue(audioId, out var clip) || clip == null)
            {
                throw new InvalidOperationException($"AudioCatalog: audio_id が未解決です: {audioId}");
            }

            return clip;
        }
    }
}
