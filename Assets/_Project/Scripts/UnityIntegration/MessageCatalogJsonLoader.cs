using System;
using System.Collections.Generic;
using AshNCircuit.Core.Systems;
using UnityEngine;

namespace AshNCircuit.UnityIntegration
{
    /// <summary>
    /// JSON で定義されたメッセージカタログデータを読み込み、
    /// MessageId → テンプレート文字列の辞書に変換するユーティリティ。
    /// 本チケットの範囲では、ゲーム本番フローからの呼び出しは必須とせず、
    /// 後続チケットでの差し替えを前提としたバックエンド実装の一部として扱う。
    /// </summary>
    public static class MessageCatalogJsonLoader
    {
        [Serializable]
        private class MessageCatalogEntry
        {
            public string id = string.Empty;
            public string text = string.Empty;
            public string? category = null;
            public string? notes = null;
        }

        [Serializable]
        private class MessageCatalogJson
        {
            public MessageCatalogEntry[] entries = Array.Empty<MessageCatalogEntry>();
        }

        /// <summary>
        /// 指定された TextAsset からメッセージカタログ JSON を読み込み、
        /// MessageId → テンプレート文字列のマッピングを生成する。
        /// </summary>
        public static Dictionary<MessageId, string> LoadFromTextAsset(TextAsset jsonAsset)
        {
            var result = new Dictionary<MessageId, string>();

            if (jsonAsset == null || string.IsNullOrEmpty(jsonAsset.text))
            {
                return result;
            }

            MessageCatalogJson data;
            try
            {
                data = JsonUtility.FromJson<MessageCatalogJson>(jsonAsset.text);
            }
            catch (Exception e)
            {
                Debug.LogError($"MessageCatalogJsonLoader: JSON のパースに失敗しました: {e.Message}");
                return result;
            }

            if (data?.entries == null)
            {
                return result;
            }

            foreach (var entry in data.entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.id))
                {
                    continue;
                }

                if (!Enum.TryParse<MessageId>(entry.id, out var messageId))
                {
                    Debug.LogWarning($"MessageCatalogJsonLoader: 未知の MessageId '{entry.id}' が JSON に含まれています。");
                    continue;
                }

                if (string.IsNullOrEmpty(entry.text))
                {
                    continue;
                }

                result[messageId] = entry.text;
            }

            return result;
        }

        /// <summary>
        /// 指定された複数の TextAsset からメッセージカタログ JSON を読み込み、
        /// それらを 1 つの辞書へマージする（fail-fast / strict）。
        /// - JSON が空 / パース失敗: 例外
        /// - 未知の MessageId: 例外
        /// - text が空: 例外
        /// - 重複定義: 例外（静かに上書きしない）
        /// </summary>
        public static Dictionary<MessageId, string> LoadFromTextAssetsStrict(IEnumerable<TextAsset> jsonAssets)
        {
            if (jsonAssets == null)
            {
                throw new ArgumentNullException(nameof(jsonAssets));
            }

            var result = new Dictionary<MessageId, string>();

            foreach (var jsonAsset in jsonAssets)
            {
                if (jsonAsset == null)
                {
                    throw new InvalidOperationException("MessageCatalogJsonLoader: null の TextAsset が渡されました。");
                }

                if (string.IsNullOrEmpty(jsonAsset.text))
                {
                    throw new InvalidOperationException($"MessageCatalogJsonLoader: '{jsonAsset.name}' が空です。");
                }

                MessageCatalogJson data;
                try
                {
                    data = JsonUtility.FromJson<MessageCatalogJson>(jsonAsset.text);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException($"MessageCatalogJsonLoader: '{jsonAsset.name}' の JSON パースに失敗しました: {e.Message}", e);
                }

                if (data?.entries == null || data.entries.Length == 0)
                {
                    throw new InvalidOperationException($"MessageCatalogJsonLoader: '{jsonAsset.name}' の entries が空です。");
                }

                foreach (var entry in data.entries)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.id))
                    {
                        continue;
                    }

                    if (!Enum.TryParse<MessageId>(entry.id, out var messageId))
                    {
                        throw new InvalidOperationException($"MessageCatalogJsonLoader: '{jsonAsset.name}' に未知の MessageId '{entry.id}' が含まれています。");
                    }

                    if (string.IsNullOrEmpty(entry.text))
                    {
                        throw new InvalidOperationException($"MessageCatalogJsonLoader: '{jsonAsset.name}' の '{entry.id}' が空文字です。");
                    }

                    if (result.ContainsKey(messageId))
                    {
                        throw new InvalidOperationException($"MessageCatalogJsonLoader: MessageId '{entry.id}' が複数ファイルで重複定義されています（例: '{jsonAsset.name}'）。");
                    }

                    result[messageId] = entry.text;
                }
            }

            if (result.Count == 0)
            {
                throw new InvalidOperationException("MessageCatalogJsonLoader: 有効なエントリが 0 件でした。");
            }

            return result;
        }
    }
}
