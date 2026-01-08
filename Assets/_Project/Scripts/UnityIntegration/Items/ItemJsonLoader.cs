using System;
using System.Collections.Generic;
using AshNCircuit.Core.Items;
using UnityEngine;

namespace AshNCircuit.UnityIntegration.Items
{
    /// <summary>
    /// content_items*.json からコアアイテム定義を読み込み、
    /// Core 層の ItemDefinition に変換するためのローダ。
    /// </summary>
    public static class ItemJsonLoader
    {
        [Serializable]
        private sealed class ItemJsonRoot
        {
            public ItemJsonEntry[] items = Array.Empty<ItemJsonEntry>();
        }

        [Serializable]
        private sealed class ItemJsonEntry
        {
            public string id = string.Empty;
            public string name = string.Empty;
            public string description = string.Empty;
            public string category = string.Empty;
            public string[] tags = Array.Empty<string>();
            public string sprite_id = string.Empty;
            public bool stackable = default;
            public int max_stack = default;
        }

        /// <summary>
        /// 指定した TextAsset 群からアイテム定義を読み込む。
        /// JSON-only / fail-fast 運用のため、不正なエントリは例外として検出する。
        /// </summary>
        public static IReadOnlyDictionary<string, ItemDefinition> LoadFromTextAssets(
            IEnumerable<TextAsset> textAssets)
        {
            if (textAssets == null)
            {
                throw new ArgumentNullException(nameof(textAssets));
            }

            var result = new Dictionary<string, ItemDefinition>(StringComparer.Ordinal);

            foreach (var asset in textAssets)
            {
                if (asset == null)
                {
                    throw new InvalidOperationException("[ItemJsonLoader] TextAsset が null です。");
                }

                if (string.IsNullOrEmpty(asset.text))
                {
                    throw new InvalidOperationException($"[ItemJsonLoader] Item JSON が空です: {asset.name}");
                }

                ItemJsonRoot root;
                try
                {
                    root = JsonUtility.FromJson<ItemJsonRoot>(asset.text);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"[ItemJsonLoader] JSON 解析に失敗しました: {asset.name}\n{ex}");
                }

                if (root?.items == null)
                {
                    throw new InvalidOperationException($"[ItemJsonLoader] items が null です: {asset.name}");
                }

                foreach (var entry in root.items)
                {
                    if (entry == null)
                    {
                        throw new InvalidOperationException($"[ItemJsonLoader] items に null エントリがあります: {asset.name}");
                    }

                    if (string.IsNullOrEmpty(entry.id))
                    {
                        throw new InvalidOperationException($"[ItemJsonLoader] item id が空です: {asset.name}");
                    }

                    if (string.IsNullOrEmpty(entry.name))
                    {
                        throw new InvalidOperationException($"[ItemJsonLoader] item name が空です: itemId={entry.id}, asset={asset.name}");
                    }

                    if (string.IsNullOrEmpty(entry.sprite_id))
                    {
                        throw new InvalidOperationException($"[ItemJsonLoader] item sprite_id が空です: itemId={entry.id}, asset={asset.name}");
                    }

                    if (result.ContainsKey(entry.id))
                    {
                        throw new InvalidOperationException($"[ItemJsonLoader] 重複したアイテムIDがあります: {entry.id}");
                    }

                    var tags = entry.tags ?? Array.Empty<string>();

                    var definition = ItemDefinition.FromJson(
                        entry.id,
                        entry.name,
                        entry.description,
                        tags,
                        entry.sprite_id,
                        entry.stackable,
                        entry.max_stack);

                    result.Add(entry.id, definition);
                }
            }

            return result;
        }
    }
}
