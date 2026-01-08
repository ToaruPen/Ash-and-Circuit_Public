using System;
using System.Collections.Generic;
using AshNCircuit.Core.Items;
using NUnit.Framework;
using UnityEngine;

namespace AshNCircuit.Tests.Core
{
    [SetUpFixture]
    public sealed class ItemDefinitionTestSetupFixture
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
            public string[] tags = Array.Empty<string>();
            public string sprite_id = string.Empty;
            public bool stackable = default;
            public int max_stack = default;
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var assets = Resources.LoadAll<TextAsset>("Content");
            if (assets == null || assets.Length == 0)
            {
                throw new InvalidOperationException("EditModeTests: Resources/Content の items JSON (TextAsset) が見つかりません。");
            }

            var dict = new Dictionary<string, ItemDefinition>();

            foreach (var asset in assets)
            {
                if (asset == null || string.IsNullOrEmpty(asset.text))
                {
                    continue;
                }

                ItemJsonRoot root;
                try
                {
                    root = JsonUtility.FromJson<ItemJsonRoot>(asset.text);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"EditModeTests: items JSON の解析に失敗しました: {asset.name}\n{ex}");
                }

                if (root?.items == null || root.items.Length == 0)
                {
                    continue;
                }

                foreach (var entry in root.items)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.id))
                    {
                        continue;
                    }

                    if (dict.ContainsKey(entry.id))
                    {
                        throw new InvalidOperationException($"EditModeTests: items JSON に重複した item id '{entry.id}' が含まれています。");
                    }

                    dict.Add(
                        entry.id,
                        ItemDefinition.FromJson(
                            entry.id,
                            entry.name,
                            entry.description,
                            entry.tags ?? Array.Empty<string>(),
                            entry.sprite_id,
                            entry.stackable,
                            entry.max_stack));
                }
            }

            if (dict.Count == 0)
            {
                throw new InvalidOperationException("EditModeTests: Resources/Content の items JSON の読み込み結果が空です。");
            }

            ItemDefinition.ApplyDefinitions(dict);
        }
    }
}
