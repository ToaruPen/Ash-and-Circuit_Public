using System.Collections.Generic;

namespace AshNCircuit.Core.Systems
{
    /// <summary>
    /// 単一ダイアログ（会話）を表す定義。
    /// docs/06_content_schema.md のダイアログスキーマに対応する。
    /// </summary>
    public sealed class DialogDefinition
    {
        public string Id { get; }
        public IReadOnlyDictionary<string, DialogNode> Nodes { get; }
        public string? StartNodeId { get; }

        private DialogDefinition(string id, Dictionary<string, DialogNode> nodes, string? startNodeId)
        {
            Id = id;
            Nodes = nodes;
            StartNodeId = startNodeId;
        }

        /// <summary>
        /// JSON ローダから渡されたノード情報をもとに DialogDefinition を構築する。
        /// </summary>
        public static DialogDefinition FromJson(
            string dialogId,
            object nodesJson)
        {
            // nodesJson は UnityIntegration 側の DialogNodeJson[] を想定しつつ、
            // Core 側では具体型に依存しないため dynamic に変換する。
            var nodesArray = nodesJson as System.Array;
            var nodes = new Dictionary<string, DialogNode>();
            string? startNodeId = null;

            if (nodesArray != null)
            {
                foreach (var rawNode in nodesArray)
                {
                    if (rawNode == null)
                    {
                        continue;
                    }

                    var id = GetStringField(rawNode, "id");
                    if (string.IsNullOrEmpty(id))
                    {
                        continue;
                    }

                    if (startNodeId == null)
                    {
                        startNodeId = id;
                    }

                    var speakerId = GetStringField(rawNode, "speaker_id");
                    var text = GetStringField(rawNode, "text");
                    var textId = GetStringField(rawNode, "textId");
                    var choices = BuildChoices(rawNode);

                    nodes[id] = new DialogNode(id, speakerId, text, textId, choices);
                }
            }

            if (startNodeId == null && nodes.Count > 0)
            {
                foreach (var key in nodes.Keys)
                {
                    startNodeId = key;
                    break;
                }
            }

            return new DialogDefinition(dialogId, nodes, startNodeId);
        }

        private static IReadOnlyList<DialogChoice> BuildChoices(object rawNode)
        {
            var result = new List<DialogChoice>();

            var choicesObj = GetField(rawNode, "choices") as System.Array;
            if (choicesObj == null)
            {
                return result;
            }

            foreach (var rawChoice in choicesObj)
            {
                if (rawChoice == null)
                {
                    continue;
                }

                var id = GetStringField(rawChoice, "id");
                var text = GetStringField(rawChoice, "text");
                var nextId = GetStringField(rawChoice, "next_node_id");

                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                result.Add(new DialogChoice(id, text, nextId));
            }

            return result;
        }

        private static object? GetField(object? obj, string fieldName)
        {
            if (obj == null)
            {
                return null;
            }

            var type = obj.GetType();
            var field = type.GetField(fieldName);
            return field != null ? field.GetValue(obj) : null;
        }

        private static string? GetStringField(object obj, string fieldName)
        {
            return GetField(obj, fieldName) as string;
        }
    }

    /// <summary>
    /// ダイアログ内の1ノード（発話と選択肢）を表す。
    /// </summary>
    public sealed class DialogNode
    {
        public string Id { get; }
        public string? SpeakerId { get; }
        public string? Text { get; }
        public string? TextId { get; }
        public IReadOnlyList<DialogChoice> Choices { get; }

        public DialogNode(string id, string? speakerId, string? text, string? textId, IReadOnlyList<DialogChoice> choices)
        {
            Id = id;
            SpeakerId = speakerId;
            Text = text;
            TextId = textId;
            Choices = choices ?? new List<DialogChoice>();
        }
    }

    /// <summary>
    /// ダイアログ選択肢。
    /// </summary>
    public readonly struct DialogChoice
    {
        public string Id { get; }
        public string? Text { get; }
        public string? NextNodeId { get; }

        public DialogChoice(string id, string? text, string? nextNodeId)
        {
            Id = id;
            Text = text;
            NextNodeId = nextNodeId;
        }
    }

    /// <summary>
    /// ダイアログの現在ノードを管理し、選択肢に応じて遷移する簡易ランナー。
    /// UI や入力制御は別レイヤで扱う。
    /// </summary>
    public sealed class DialogRunner
    {
        private readonly DialogDefinition _dialog;
        private DialogNode? _currentNode;

        public DialogRunner(DialogDefinition dialog)
        {
            _dialog = dialog;
            _currentNode = GetNode(dialog.StartNodeId);
        }

        public DialogNode? CurrentNode => _currentNode;

        public bool TryChoose(string choiceId)
        {
            if (_currentNode == null || _currentNode.Choices == null)
            {
                return false;
            }

            foreach (var choice in _currentNode.Choices)
            {
                if (choice.Id == choiceId)
                {
                    if (string.IsNullOrEmpty(choice.NextNodeId))
                    {
                        _currentNode = null;
                        return true;
                    }

                    _currentNode = GetNode(choice.NextNodeId);
                    return true;
                }
            }

            return false;
        }

        private DialogNode? GetNode(string? nodeId)
        {
            if (string.IsNullOrEmpty(nodeId) || _dialog.Nodes == null)
            {
                return null;
            }

            return _dialog.Nodes.TryGetValue(nodeId, out var node) ? node : null;
        }
    }
}
