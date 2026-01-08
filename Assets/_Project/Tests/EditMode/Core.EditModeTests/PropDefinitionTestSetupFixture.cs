using System;
using System.Collections.Generic;
using AshNCircuit.Core.Map;
using NUnit.Framework;
using UnityEngine;

namespace AshNCircuit.Tests.Core
{
    [SetUpFixture]
    public sealed class PropDefinitionTestSetupFixture
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

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var assets = Resources.LoadAll<TextAsset>("Props");
            if (assets == null || assets.Length == 0)
            {
                throw new InvalidOperationException("EditModeTests: Resources/Props の props JSON (TextAsset) が見つかりません。");
            }

            var dict = new Dictionary<string, PropDefinition>(StringComparer.Ordinal);

            foreach (var asset in assets)
            {
                if (asset == null)
                {
                    throw new InvalidOperationException("EditModeTests: props JSON (TextAsset) が null です。");
                }

                if (string.IsNullOrEmpty(asset.text))
                {
                    throw new InvalidOperationException($"EditModeTests: props JSON が空です: {asset.name}");
                }

                PropJsonRoot root;
                try
                {
                    root = JsonUtility.FromJson<PropJsonRoot>(asset.text);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"EditModeTests: props JSON の解析に失敗しました: {asset.name}\n{ex}");
                }

                if (root?.props == null)
                {
                    throw new InvalidOperationException($"EditModeTests: props が null です: {asset.name}");
                }

                foreach (var entry in root.props)
                {
                    if (entry == null)
                    {
                        throw new InvalidOperationException($"EditModeTests: props に null エントリがあります: {asset.name}");
                    }

                    if (string.IsNullOrEmpty(entry.id))
                    {
                        throw new InvalidOperationException($"EditModeTests: prop id が空です: {asset.name}");
                    }

                    if (string.IsNullOrEmpty(entry.display_name))
                    {
                        throw new InvalidOperationException($"EditModeTests: prop display_name が空です: propId={entry.id}, asset={asset.name}");
                    }

                    if (string.IsNullOrEmpty(entry.sprite_id))
                    {
                        throw new InvalidOperationException($"EditModeTests: prop sprite_id が空です: propId={entry.id}, asset={asset.name}");
                    }

                    if (dict.ContainsKey(entry.id))
                    {
                        throw new InvalidOperationException($"EditModeTests: props JSON に重複した prop id '{entry.id}' が含まれています。");
                    }

                    dict.Add(
                        entry.id,
                        PropDefinition.FromJson(
                            entry.id,
                            entry.display_name,
                            entry.sprite_id,
                            entry.blocks_movement,
                            entry.blocks_los,
                            entry.blocks_projectiles));
                }
            }

            if (dict.Count == 0)
            {
                throw new InvalidOperationException("EditModeTests: Resources/Props の props JSON の読み込み結果が空です。");
            }

            PropDefinition.ApplyDefinitions(dict);
        }
    }
}
