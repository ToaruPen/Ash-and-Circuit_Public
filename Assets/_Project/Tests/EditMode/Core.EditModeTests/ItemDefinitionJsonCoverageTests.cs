using System;
using System.Collections.Generic;
using AshNCircuit.Core.Items;
using NUnit.Framework;
using UnityEngine;

namespace AshNCircuit.Tests.Core
{
    public class ItemDefinitionJsonCoverageTests
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
        }

        [Test]
        public void ContentItemsJson_Exists_AndCoversRequiredItemIds()
        {
            var assets = Resources.LoadAll<TextAsset>("Content");
            Assert.That(assets, Is.Not.Null.And.Not.Empty, "Resources/Content should contain item JSON (TextAsset).");

            var ids = new HashSet<string>();
            foreach (var asset in assets!)
            {
                Assert.That(asset, Is.Not.Null, "TextAsset in Resources/Content should not be null.");
                Assert.That(asset!.text, Is.Not.Null.And.Not.Empty, $"Item JSON should not be empty: {asset.name}");

                ItemJsonRoot data;
                Assert.DoesNotThrow(
                    () => data = JsonUtility.FromJson<ItemJsonRoot>(asset.text),
                    $"Item JSON should be valid JSON: {asset.name}");

                data = JsonUtility.FromJson<ItemJsonRoot>(asset.text);
                Assert.That(data, Is.Not.Null, $"Parsed item JSON root should not be null: {asset.name}");
                Assert.That(data.items, Is.Not.Null, $"`items` should not be null: {asset.name}");

                foreach (var entry in data.items)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.id))
                    {
                        continue;
                    }

                    Assert.That(ids.Add(entry.id), Is.True, $"Content items JSON contains duplicated item id: {entry.id}");
                }
            }

            Assert.That(ids.Count, Is.GreaterThan(0), "Content items JSON should contain at least 1 item.");

            var missing = new List<string>();
            if (!ids.Contains(ItemDefinition.ShortSwordId))
            {
                missing.Add(ItemDefinition.ShortSwordId);
            }
            if (!ids.Contains(ItemDefinition.BowId))
            {
                missing.Add(ItemDefinition.BowId);
            }
            if (!ids.Contains(ItemDefinition.WoodenArrowId))
            {
                missing.Add(ItemDefinition.WoodenArrowId);
            }
            if (!ids.Contains(ItemDefinition.OilBottleId))
            {
                missing.Add(ItemDefinition.OilBottleId);
            }
            if (!ids.Contains(ItemDefinition.DirtClodId))
            {
                missing.Add(ItemDefinition.DirtClodId);
            }

            Assert.That(missing, Is.Empty, $"Content items JSON is missing required item id(s): {string.Join(", ", missing)}");
        }

        [Test]
        public void ApplyDefinitions_FailsFast_WhenEmptyOrMissingRequired()
        {
            var empty = new Dictionary<string, ItemDefinition>();
            Assert.Throws<InvalidOperationException>(() => ItemDefinition.ApplyDefinitions(empty));

            var missingSome = new Dictionary<string, ItemDefinition>
            {
                {
                    ItemDefinition.ShortSwordId,
                    ItemDefinition.FromJson(ItemDefinition.ShortSwordId, "短剣", "desc", Array.Empty<string>(), "icon_item_short_sword", false, 1)
                }
            };
            Assert.Throws<InvalidOperationException>(() => ItemDefinition.ApplyDefinitions(missingSome));
        }
    }
}
