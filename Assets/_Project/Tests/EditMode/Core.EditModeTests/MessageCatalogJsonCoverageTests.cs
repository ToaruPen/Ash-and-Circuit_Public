using System;
using System.Collections.Generic;
using AshNCircuit.Core.Systems;
using NUnit.Framework;
using UnityEngine;

namespace AshNCircuit.Tests.Core
{
    /// <summary>
    /// MessageCatalog.json が MessageId enum を全件カバーしていることを検証するテスト。
    /// - 欠落 MessageId があれば FAIL
    /// - 未知の MessageId が含まれていれば FAIL
    /// - 重複定義があれば FAIL
    /// </summary>
    public class MessageCatalogJsonCoverageTests
    {
        private static readonly string[] RequiredMessageCatalogAssetNames =
        {
            "MessageCatalog_ui",
            "MessageCatalog_gameplay",
            "MessageCatalog_guide",
        };

        [Serializable]
        private class MessageCatalogEntry
        {
            public string id = string.Empty;
            public string text = string.Empty;
        }

        [Serializable]
        private class MessageCatalogJson
        {
            public MessageCatalogEntry[] entries = Array.Empty<MessageCatalogEntry>();
        }

        [Test]
        public void MessageCatalogJson_CoversAllMessageIds_AndHasNoUnknownIds()
        {
            var assets = Resources.LoadAll<TextAsset>("MessageCatalog");
            Assert.That(assets, Is.Not.Null.And.Not.Empty, "Resources/MessageCatalog/*.json (TextAsset) should exist.");

            var assetMap = new Dictionary<string, TextAsset>(StringComparer.Ordinal);
            foreach (var a in assets)
            {
                if (a != null && !string.IsNullOrEmpty(a.name))
                {
                    assetMap[a.name] = a;
                }
            }

            foreach (var requiredName in RequiredMessageCatalogAssetNames)
            {
                Assert.That(assetMap.ContainsKey(requiredName), $"Resources/MessageCatalog/{requiredName}.json should exist.");
                Assert.That(assetMap[requiredName].text, Is.Not.Null.And.Not.Empty, $"{requiredName}.json should not be empty.");
            }

            var jsonIds = new HashSet<MessageId>();
            var duplicatedIds = new List<string>();
            var unknownIds = new List<string>();
            var emptyTextIds = new List<string>();
            var parseFailures = new List<string>();

            foreach (var asset in assets)
            {
                if (asset == null)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(asset.text))
                {
                    parseFailures.Add($"{asset.name}: empty");
                    continue;
                }

                MessageCatalogJson? data;
                try
                {
                    data = JsonUtility.FromJson<MessageCatalogJson>(asset.text);
                }
                catch (Exception e)
                {
                    parseFailures.Add($"{asset.name}: {e.Message}");
                    continue;
                }

                if (data?.entries == null)
                {
                    parseFailures.Add($"{asset.name}: entries is null");
                    continue;
                }

                foreach (var entry in data.entries)
                {
                    if (entry == null)
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(entry.id))
                    {
                        continue;
                    }

                    if (!Enum.TryParse<MessageId>(entry.id, out var messageId))
                    {
                        unknownIds.Add($"{asset.name}:{entry.id}");
                        continue;
                    }

                    if (!jsonIds.Add(messageId))
                    {
                        duplicatedIds.Add($"{asset.name}:{entry.id}");
                    }

                    if (string.IsNullOrEmpty(entry.text))
                    {
                        emptyTextIds.Add($"{asset.name}:{entry.id}");
                    }
                }
            }

            Assert.That(parseFailures, Is.Empty, $"MessageCatalog JSON parse failures: {string.Join(", ", parseFailures)}");
            Assert.That(unknownIds, Is.Empty, $"MessageCatalog JSON contains unknown MessageId(s): {string.Join(", ", unknownIds)}");
            Assert.That(duplicatedIds, Is.Empty, $"MessageCatalog JSON contains duplicated MessageId(s): {string.Join(", ", duplicatedIds)}");
            Assert.That(emptyTextIds, Is.Empty, $"MessageCatalog JSON contains empty text for MessageId(s): {string.Join(", ", emptyTextIds)}");

            var missingIds = new List<string>();
            foreach (MessageId id in Enum.GetValues(typeof(MessageId)))
            {
                if (!jsonIds.Contains(id))
                {
                    missingIds.Add(id.ToString());
                }
            }

            Assert.That(missingIds, Is.Empty, $"MessageCatalog JSON is missing MessageId(s): {string.Join(", ", missingIds)}");
        }
    }
}
