using System;
using System.Collections.Generic;
using AshNCircuit.Core.Systems;
using UnityEngine;

namespace AshNCircuit.UnityIntegration.Dialogs
{
    /// <summary>
    /// dialogs_*.json から DialogDefinition を読み込むためのローダ。
    /// docs/06_content_schema.md のダイアログスキーマに対応する。
    /// </summary>
    public static class DialogJsonLoader
    {
        [Serializable]
        private sealed class DialogsJsonRoot
        {
            public DialogJsonEntry[] dialogs = Array.Empty<DialogJsonEntry>();
        }

        [Serializable]
        private sealed class DialogJsonEntry
        {
            public string dialog_id = string.Empty;
            public DialogNodeJson[] nodes = Array.Empty<DialogNodeJson>();
        }

        [Serializable]
        private sealed class DialogNodeJson
        {
            public string id = string.Empty;
            public string speaker_id = string.Empty;
            public string text = string.Empty;
            public string textId = string.Empty;
            public DialogChoiceJson[] choices = Array.Empty<DialogChoiceJson>();
        }

        [Serializable]
        private sealed class DialogChoiceJson
        {
            public string id = string.Empty;
            public string text = string.Empty;
            public string next_node_id = string.Empty;
        }

        /// <summary>
        /// 指定した TextAsset 群からダイアログ定義テーブルを構築する。
        /// 不正なエントリは警告を出してスキップする。
        /// </summary>
        public static IReadOnlyDictionary<string, DialogDefinition> LoadFromTextAssets(
            IEnumerable<TextAsset> textAssets)
        {
            var result = new Dictionary<string, DialogDefinition>();

            if (textAssets == null)
            {
                return result;
            }

            foreach (var asset in textAssets)
            {
                if (asset == null || string.IsNullOrEmpty(asset.text))
                {
                    continue;
                }

                DialogsJsonRoot root;
                try
                {
                    root = JsonUtility.FromJson<DialogsJsonRoot>(asset.text);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[DialogJsonLoader] JSON 解析に失敗しました: {asset.name}\n{ex}");
                    continue;
                }

                if (root?.dialogs == null)
                {
                    continue;
                }

                foreach (var dialog in root.dialogs)
                {
                    if (dialog == null || string.IsNullOrEmpty(dialog.dialog_id))
                    {
                        continue;
                    }

                    if (result.ContainsKey(dialog.dialog_id))
                    {
                        Debug.LogWarning($"[DialogJsonLoader] 重複した dialog_id をスキップします: {dialog.dialog_id}");
                        continue;
                    }

                    var definition = DialogDefinition.FromJson(
                        dialog.dialog_id,
                        dialog.nodes);

                    result.Add(dialog.dialog_id, definition);
                }
            }

            return result;
        }
    }
}
