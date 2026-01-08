using System;
using System.Collections.Generic;
using AshNCircuit.Core.Map;
using UnityEngine;

namespace AshNCircuit.UnityIntegration.Props
{
    /// <summary>
    /// props_*.json からプロップ定義を読み込み、Core 層の PropDefinition に変換するためのローダ。
    /// </summary>
    public static class PropJsonLoader
    {
        [Serializable]
        private sealed class PropJsonRoot
        {
            public PropJsonEntry[] props = Array.Empty<PropJsonEntry>();
        }

        [Serializable]
        private sealed class PropJsonEntry
        {
            public string id = string.Empty;
            public string display_name = string.Empty;
            public string sprite_id = string.Empty;
            public bool blocks_movement = default;
            public bool blocks_los = default;
            public bool blocks_projectiles = default;
        }

        /// <summary>
        /// 指定した TextAsset 群からプロップ定義を読み込む。
        /// JSON-only / fail-fast 運用のため、不正なエントリは例外として検出する。
        /// </summary>
        public static IReadOnlyDictionary<string, PropDefinition> LoadFromTextAssets(
            IEnumerable<TextAsset> textAssets)
        {
            if (textAssets == null)
            {
                throw new ArgumentNullException(nameof(textAssets));
            }

            var result = new Dictionary<string, PropDefinition>(StringComparer.Ordinal);

            foreach (var asset in textAssets)
            {
                if (asset == null)
                {
                    throw new InvalidOperationException("[PropJsonLoader] TextAsset が null です。");
                }

                if (string.IsNullOrEmpty(asset.text))
                {
                    throw new InvalidOperationException($"[PropJsonLoader] Props JSON が空です: {asset.name}");
                }

                PropJsonRoot root;
                try
                {
                    root = JsonUtility.FromJson<PropJsonRoot>(asset.text);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"[PropJsonLoader] JSON 解析に失敗しました: {asset.name}\n{ex}");
                }

                if (root?.props == null)
                {
                    throw new InvalidOperationException($"[PropJsonLoader] props が null です: {asset.name}");
                }

                foreach (var entry in root.props)
                {
                    if (entry == null)
                    {
                        throw new InvalidOperationException($"[PropJsonLoader] props に null エントリがあります: {asset.name}");
                    }

                    if (string.IsNullOrEmpty(entry.id))
                    {
                        throw new InvalidOperationException($"[PropJsonLoader] prop id が空です: {asset.name}");
                    }

                    if (string.IsNullOrEmpty(entry.display_name))
                    {
                        throw new InvalidOperationException($"[PropJsonLoader] prop display_name が空です: propId={entry.id}, asset={asset.name}");
                    }

                    if (string.IsNullOrEmpty(entry.sprite_id))
                    {
                        throw new InvalidOperationException($"[PropJsonLoader] prop sprite_id が空です: propId={entry.id}, asset={asset.name}");
                    }

                    if (result.ContainsKey(entry.id))
                    {
                        throw new InvalidOperationException($"[PropJsonLoader] 重複したプロップIDがあります: {entry.id}");
                    }

                    var definition = PropDefinition.FromJson(
                        entry.id,
                        entry.display_name,
                        entry.sprite_id,
                        entry.blocks_movement,
                        entry.blocks_los,
                        entry.blocks_projectiles);

                    result.Add(entry.id, definition);
                }
            }

            return result;
        }
    }
}
