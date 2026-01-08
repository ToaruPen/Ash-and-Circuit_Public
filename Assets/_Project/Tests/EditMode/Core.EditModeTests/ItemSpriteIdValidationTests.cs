using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace AshNCircuit.Tests.Core
{
    public sealed class ItemSpriteIdValidationTests
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
            public string sprite_id = string.Empty;
        }

        [Test]
        public void ContentItemsJson_SpriteId_IsRequired_AndResolvesInSpriteCatalog()
        {
            var assets = Resources.LoadAll<TextAsset>("Content");
            Assert.That(assets, Is.Not.Null.And.Not.Empty, "Resources/Content should contain item JSON (TextAsset).");

            var (spriteCatalog, getSpriteOrThrow) = LoadSpriteCatalogViaReflection();

            var ids = new HashSet<string>(StringComparer.Ordinal);

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
                    Assert.That(entry, Is.Not.Null, $"Item entry should not be null: {asset.name}");
                    Assert.That(entry!.id, Is.Not.Null.And.Not.Empty, $"Item id should not be empty: {asset.name}");
                    Assert.That(ids.Add(entry.id), Is.True, $"Content items JSON contains duplicated item id: {entry.id}");

                    Assert.That(entry.sprite_id, Is.Not.Null.And.Not.Empty, $"Item sprite_id is required: {entry.id}");
                    Assert.DoesNotThrow(
                        () => InvokeGetSpriteOrThrow(spriteCatalog, getSpriteOrThrow, entry.sprite_id),
                        $"SpriteCatalog should resolve item sprite_id: itemId={entry.id}, sprite_id={entry.sprite_id}");
                }
            }
        }

        private static (object SpriteCatalog, MethodInfo GetSpriteOrThrow) LoadSpriteCatalogViaReflection()
        {
            var spriteCatalogType = Type.GetType("AshNCircuit.UnityIntegration.Content.SpriteCatalog, Assembly-CSharp");
            Assert.That(spriteCatalogType, Is.Not.Null, "SpriteCatalog type should exist in Assembly-CSharp.");

            var loadFromResources = spriteCatalogType!.GetMethod("LoadFromResources", BindingFlags.Public | BindingFlags.Static);
            Assert.That(loadFromResources, Is.Not.Null, "SpriteCatalog.LoadFromResources() should exist.");

            var spriteCatalog = loadFromResources!.Invoke(null, null);
            Assert.That(spriteCatalog, Is.Not.Null, "SpriteCatalog.LoadFromResources() should return an instance.");

            var getSpriteOrThrow = spriteCatalogType.GetMethod("GetSpriteOrThrow", BindingFlags.Public | BindingFlags.Instance);
            Assert.That(getSpriteOrThrow, Is.Not.Null, "SpriteCatalog.GetSpriteOrThrow(string) should exist.");

            return (spriteCatalog!, getSpriteOrThrow!);
        }

        private static void InvokeGetSpriteOrThrow(object spriteCatalog, MethodInfo getSpriteOrThrow, string spriteId)
        {
            try
            {
                _ = getSpriteOrThrow.Invoke(spriteCatalog, new object[] { spriteId });
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException ?? ex;
            }
        }
    }
}

