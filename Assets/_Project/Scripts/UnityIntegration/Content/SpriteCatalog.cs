using System;
using System.Collections.Generic;
using UnityEngine;

namespace AshNCircuit.UnityIntegration.Content
{
    /// <summary>
    /// sprite_id → Sprite を解決するカタログ。
    /// JSON-only / fail-fast 運用のため、欠落は例外として検出する。
    /// </summary>
    public sealed class SpriteCatalog
    {
        private static SpriteCatalog _cached = null!;
        private static bool _hasCached;

        private readonly IReadOnlyDictionary<string, Sprite> _spritesById;

        private SpriteCatalog(IReadOnlyDictionary<string, Sprite> spritesById)
        {
            _spritesById = spritesById;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache()
        {
            _cached = null!;
            _hasCached = false;
        }

        public static SpriteCatalog LoadFromResources()
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

        private static SpriteCatalog LoadFromResourcesUncached()
        {
            var assets = Resources.LoadAll<SpriteCatalogAsset>("SpriteCatalogs");
            if (assets == null || assets.Length == 0)
            {
                throw new InvalidOperationException(
                    "SpriteCatalog: Resources/SpriteCatalogs 配下に SpriteCatalogAsset が見つかりません。ScriptableObject を配置してください。");
            }

            var map = new Dictionary<string, Sprite>(StringComparer.Ordinal);

            foreach (var asset in assets)
            {
                if (asset == null)
                {
                    throw new InvalidOperationException("SpriteCatalog: SpriteCatalogAsset が null です。Resources/SpriteCatalogs 配下のアセットを確認してください。");
                }

                if (asset.Entries == null || asset.Entries.Length == 0)
                {
                    throw new InvalidOperationException($"SpriteCatalog: Entries が空です（asset={asset.name}）。");
                }

                foreach (var entry in asset.Entries)
                {
                    if (entry == null)
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(entry.SpriteId))
                    {
                        throw new InvalidOperationException($"SpriteCatalog: SpriteId が空のエントリがあります（asset={asset.name}）。");
                    }

                    if (entry.Sprite == null)
                    {
                        throw new InvalidOperationException($"SpriteCatalog: Sprite が未設定です（SpriteId={entry.SpriteId}, asset={asset.name}）。");
                    }

                    if (map.ContainsKey(entry.SpriteId))
                    {
                        throw new InvalidOperationException($"SpriteCatalog: SpriteId が重複しています: {entry.SpriteId}");
                    }

                    map.Add(entry.SpriteId, entry.Sprite);
                }
            }

            if (map.Count == 0)
            {
                throw new InvalidOperationException("SpriteCatalog: 有効な Entries が 0 件です。");
            }

            return new SpriteCatalog(map);
        }

        public Sprite GetSpriteOrThrow(string spriteId)
        {
            if (string.IsNullOrEmpty(spriteId))
            {
                throw new ArgumentException("SpriteCatalog: spriteId が空です。", nameof(spriteId));
            }

            if (!_spritesById.TryGetValue(spriteId, out var sprite) || sprite == null)
            {
                throw new InvalidOperationException($"SpriteCatalog: sprite_id が未解決です: {spriteId}");
            }

            return sprite;
        }
    }
}
