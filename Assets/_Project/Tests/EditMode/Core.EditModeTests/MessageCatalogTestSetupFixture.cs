using System;
using System.Collections.Generic;
using System.Linq;
using AshNCircuit.Core.Systems;
using NUnit.Framework;
using UnityEngine;

namespace AshNCircuit.Tests.Core
{
    [SetUpFixture]
    public sealed class MessageCatalogTestSetupFixture
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

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var assets = Resources.LoadAll<TextAsset>("MessageCatalog");
            if (assets == null || assets.Length == 0)
            {
                throw new InvalidOperationException("EditModeTests: Resources/MessageCatalog/*.json が見つかりません。");
            }

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
                if (!assetMap.TryGetValue(requiredName, out var requiredAsset) || requiredAsset == null)
                {
                    throw new InvalidOperationException($"EditModeTests: Resources/MessageCatalog/{requiredName}.json が見つかりません。");
                }

                if (string.IsNullOrEmpty(requiredAsset.text))
                {
                    throw new InvalidOperationException($"EditModeTests: Resources/MessageCatalog/{requiredName}.json が空です。");
                }
            }

            var dict = new Dictionary<MessageId, string>();
            foreach (var asset in assetMap.Values.OrderBy(a => a.name, StringComparer.Ordinal))
            {
                if (asset == null)
                {
                    throw new InvalidOperationException("EditModeTests: MessageCatalog に null の TextAsset が含まれています。");
                }

                if (string.IsNullOrEmpty(asset.text))
                {
                    throw new InvalidOperationException($"EditModeTests: {asset.name}.json が空です。");
                }

                MessageCatalogJson? data;
                try
                {
                    data = JsonUtility.FromJson<MessageCatalogJson>(asset.text);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException($"EditModeTests: {asset.name}.json の JSON パースに失敗しました: {e.Message}", e);
                }

                if (data?.entries == null || data.entries.Length == 0)
                {
                    throw new InvalidOperationException($"EditModeTests: {asset.name}.json の entries が空です。");
                }

                foreach (var entry in data.entries)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.id))
                    {
                        continue;
                    }

                    if (!Enum.TryParse<MessageId>(entry.id, out var messageId))
                    {
                        throw new InvalidOperationException($"EditModeTests: {asset.name}.json に未知の MessageId '{entry.id}' が含まれています。");
                    }

                    if (string.IsNullOrEmpty(entry.text))
                    {
                        throw new InvalidOperationException($"EditModeTests: {asset.name}.json の '{entry.id}' が空文字です。");
                    }

                    if (dict.ContainsKey(messageId))
                    {
                        throw new InvalidOperationException($"EditModeTests: MessageId '{entry.id}' が複数ファイルで重複定義されています（例: {asset.name}.json）。");
                    }

                    dict[messageId] = entry.text;
                }
            }

            if (dict.Count == 0)
            {
                throw new InvalidOperationException("EditModeTests: MessageCatalog から有効なエントリが構築できませんでした。");
            }

            foreach (MessageId id in Enum.GetValues(typeof(MessageId)))
            {
                if (!dict.ContainsKey(id))
                {
                    throw new InvalidOperationException($"EditModeTests: MessageCatalog に '{id}' が存在しません。");
                }
            }

            MessageCatalog.Initialize(new DictionaryMessageCatalog(dict));
        }
    }
}
