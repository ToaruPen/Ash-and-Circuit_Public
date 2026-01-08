using System;
using System.Collections.Generic;
using AshNCircuit.Core.Entities;
using UnityEngine;

namespace AshNCircuit.UnityIntegration.Actors
{
    /// <summary>
    /// actors_*.json から ActorDefinition を読み込むためのローダ。
    /// docs/06_content_schema.md の Actor スキーマに対応する。
    /// </summary>
    public static class ActorJsonLoader
    {
        [Serializable]
        private sealed class ActorJsonRoot
        {
            public ActorJsonEntry[] actors = Array.Empty<ActorJsonEntry>();
        }

        [Serializable]
        private sealed class ActorJsonEntry
        {
            public string id = string.Empty;
            public string display_name = string.Empty;
            public string sprite_id = string.Empty;
            public string kind = string.Empty;
            public string faction_id = string.Empty;
            public string[] tags = Array.Empty<string>();
            public ActorStatsJson base_stats = new ActorStatsJson();
            public string ai_profile_id = string.Empty;
            public InventoryEntryJson[] initial_inventory = Array.Empty<InventoryEntryJson>();
            public string notes = string.Empty;
        }

        [Serializable]
        private sealed class ActorStatsJson
        {
            public int hp = default;
            public int attack = default;
            public int defense = default;
            public int speed = default;
        }

        [Serializable]
        private sealed class InventoryEntryJson
        {
            public string item_id = string.Empty;
            public int count = default;
        }

        /// <summary>
        /// 指定した TextAsset 群から ActorDefinition テーブルを構築する。
        /// JSON-only 運用のため、不正なエントリは fail-fast（例外）で検出する。
        /// </summary>
        public static IReadOnlyDictionary<string, ActorDefinition> LoadFromTextAssets(
            IEnumerable<TextAsset> textAssets)
        {
            if (textAssets == null)
            {
                throw new ArgumentNullException(nameof(textAssets));
            }

            var result = new Dictionary<string, ActorDefinition>(StringComparer.Ordinal);

            foreach (var asset in textAssets)
            {
                if (asset == null)
                {
                    throw new InvalidOperationException("ActorJsonLoader: TextAsset が null です。Resources/Actors 配下の JSON を確認してください。");
                }

                if (string.IsNullOrEmpty(asset.text))
                {
                    throw new InvalidOperationException($"ActorJsonLoader: JSON が空です: {asset.name}");
                }

                ActorJsonRoot root;
                try
                {
                    root = JsonUtility.FromJson<ActorJsonRoot>(asset.text);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"ActorJsonLoader: JSON 解析に失敗しました: {asset.name}", ex);
                }

                if (root?.actors == null)
                {
                    throw new InvalidOperationException($"ActorJsonLoader: actors 配列がありません: {asset.name}");
                }

                foreach (var entry in root.actors)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.id))
                    {
                        throw new InvalidOperationException($"ActorJsonLoader: actor.id が空のエントリがあります: {asset.name}");
                    }

                    if (string.IsNullOrEmpty(entry.sprite_id))
                    {
                        throw new InvalidOperationException($"ActorJsonLoader: actor.sprite_id が空です: id={entry.id}");
                    }

                    if (result.ContainsKey(entry.id))
                    {
                        throw new InvalidOperationException($"ActorJsonLoader: 重複した Actor ID です: {entry.id}");
                    }

                    if (entry.base_stats == null)
                    {
                        throw new InvalidOperationException($"ActorJsonLoader: base_stats がありません: id={entry.id}");
                    }

                    var definition = ActorDefinition.FromJson(
                        entry.id,
                        entry.display_name,
                        entry.sprite_id,
                        entry.kind,
                        entry.faction_id,
                        entry.tags,
                        entry.base_stats.hp,
                        entry.base_stats.attack,
                        entry.base_stats.defense,
                        entry.base_stats.speed,
                        entry.ai_profile_id,
                        ToInventoryEntries(entry.initial_inventory),
                        entry.notes);

                    result.Add(entry.id, definition);
                }
            }

            return result;
        }

        private static IReadOnlyList<ActorInventoryEntry> ToInventoryEntries(InventoryEntryJson[] entries)
        {
            if (entries == null || entries.Length == 0)
            {
                return Array.Empty<ActorInventoryEntry>();
            }

            var list = new List<ActorInventoryEntry>(entries.Length);
            foreach (var e in entries)
            {
                if (e == null || string.IsNullOrEmpty(e.item_id) || e.count <= 0)
                {
                    continue;
                }

                list.Add(new ActorInventoryEntry(e.item_id, e.count));
            }

            return list;
        }
    }
}
