using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AshNCircuit.Core.Entities;
using AshNCircuit.Core.Items;
using AshNCircuit.Core.Map;
using AshNCircuit.Core.Systems;
using AshNCircuit.UnityIntegration;
using AshNCircuit.UnityIntegration.Items;
using AshNCircuit.UnityIntegration.Actors;
using AshNCircuit.UnityIntegration.Content;
using AshNCircuit.UnityIntegration.Props;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AshNCircuit.UnityIntegration.Boot
{
    /// <summary>
    /// Boot シーンから Game シーンへ遷移させるためのシンプルなローダー。
    /// MVP初期段階では、Awake 時に即座に Game シーンを読み込むだけとする。
    /// </summary>
    public class BootLoader : MonoBehaviour
    {
        private static readonly string[] RequiredMessageCatalogAssetNames =
        {
            "MessageCatalog_ui",
            "MessageCatalog_gameplay",
            "MessageCatalog_guide",
        };

        [SerializeField]
        private string gameSceneName = "Game";

        private void Awake()
        {
            InitializeMessageCatalogFromJson();
            InitializeItemsFromJson();
            InitializePropsFromJson();
            InitializeActorsFromJson();
            InitializeSpriteCatalogFromResources();
            InitializeAudioCatalogFromResources();

            if (!string.IsNullOrEmpty(gameSceneName))
            {
                SceneManager.LoadScene(gameSceneName);
            }
        }

        /// <summary>
        /// Resources に配置された JSON からメッセージカタログを読み込み、
        /// MessageCatalog のバックエンドとして差し替える。
        /// JSON が存在しない／空／パース失敗の場合は fail-fast（例外）で検出する。
        /// </summary>
        private void InitializeMessageCatalogFromJson()
        {
            // Resources/MessageCatalog 配下からまとめて取得する（3ファイル名をコードにベタ書きしない運用に寄せる）。
            var assets = Resources.LoadAll<TextAsset>("MessageCatalog");
            if (assets == null || assets.Length == 0)
            {
                FailMessageCatalogInitialization("Resources/MessageCatalog に MessageCatalog JSON (TextAsset) が見つかりません。");
            }

            var assetMap = new System.Collections.Generic.Dictionary<string, TextAsset>(StringComparer.Ordinal);
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
                    FailMessageCatalogInitialization($"Resources/MessageCatalog/{requiredName}.json が見つかりません。");
                }

                if (string.IsNullOrEmpty(requiredAsset.text))
                {
                    FailMessageCatalogInitialization($"Resources/MessageCatalog/{requiredName}.json が空です。");
                }
            }

            System.Collections.Generic.Dictionary<MessageId, string> dict;
            try
            {
                // 必須 3 ファイルの fail-fast チェックを行いつつ、実際の辞書構築は Resources/MessageCatalog 配下の全ファイルを strict 合算する。
                var allAssets = assetMap.Values.OrderBy(a => a.name, StringComparer.Ordinal);
                dict = MessageCatalogJsonLoader.LoadFromTextAssetsStrict(allAssets);
            }
            catch (Exception e)
            {
                FailMessageCatalogInitialization($"MessageCatalog JSON の読み込みに失敗しました: {e.Message}");
                return; // FailMessageCatalogInitialization が例外を投げるが、解析上 return を残す
            }

            var missing = GetMissingMessageIds(dict);
            if (missing.Count > 0)
            {
                FailMessageCatalogInitialization($"MessageCatalog に MessageId の欠落があります: {string.Join(", ", missing)}");
            }

            MessageCatalog.Initialize(new DictionaryMessageCatalog(dict));
        }

        private static System.Collections.Generic.List<string> GetMissingMessageIds(System.Collections.Generic.IDictionary<MessageId, string> dict)
        {
            var missing = new System.Collections.Generic.List<string>();
            foreach (MessageId id in Enum.GetValues(typeof(MessageId)))
            {
                if (!dict.ContainsKey(id))
                {
                    missing.Add(id.ToString());
                }
            }
            return missing;
        }

        [DoesNotReturn]
        private static void FailMessageCatalogInitialization(string message)
        {
            Debug.LogError(message);
            throw new InvalidOperationException(message);
        }

        /// <summary>
        /// Resources に配置された JSON からプロップ定義を読み込み、
        /// PropDefinition の静的テーブルへ反映する。
        /// JSON-only / fail-fast 運用のため、JSON 不足/空/必須フィールド欠落/`sprite_id` 未解決は例外で停止する。
        /// </summary>
        private void InitializePropsFromJson()
        {
            var assets = Resources.LoadAll<TextAsset>("Props");
            if (assets == null || assets.Length == 0)
            {
                FailPropInitialization("Resources/Props に props JSON (TextAsset) が見つかりません。期待: props_*.json");
            }

            IReadOnlyDictionary<string, PropDefinition> dict;
            try
            {
                dict = PropJsonLoader.LoadFromTextAssets(assets);
            }
            catch (Exception e)
            {
                FailPropInitialization($"Resources/Props の props JSON の読み込みに失敗しました: {e.Message}");
                return;
            }

            if (dict == null || dict.Count == 0)
            {
                FailPropInitialization("Resources/Props の props JSON の読み込み結果が空です。JSON の内容・スキーマ・パースエラーを確認してください。");
            }

            ValidatePropSpriteIds(dict);
            PropDefinition.ApplyDefinitions(dict);
        }

        private static void ValidatePropSpriteIds(IReadOnlyDictionary<string, PropDefinition> dict)
        {
            if (dict == null)
            {
                throw new ArgumentNullException(nameof(dict));
            }

            var spriteCatalog = SpriteCatalog.LoadFromResources();

            foreach (var pair in dict)
            {
                var propId = pair.Key;
                var definition = pair.Value;
                if (definition == null)
                {
                    FailPropInitialization($"PropDefinition が null です: {propId}");
                }

                try
                {
                    _ = spriteCatalog.GetSpriteOrThrow(definition.SpriteId);
                }
                catch (Exception e)
                {
                    FailPropInitialization($"Prop sprite_id が未解決です: propId={propId}, sprite_id={definition.SpriteId}\n{e}");
                }
            }
        }

        [DoesNotReturn]
        private static void FailPropInitialization(string message)
        {
            Debug.LogError(message);
            throw new InvalidOperationException(message);
        }

        /// <summary>
         /// Resources に配置された JSON からコアアイテム定義を読み込み、
         /// ItemDefinition の静的プロパティへ反映する。
        /// JSON-only / fail-fast 運用のため、JSON 不足/空/必須ID欠落/`sprite_id` 未解決は例外で停止する。
        /// </summary>
        private void InitializeItemsFromJson()
        {
            // 将来的に content_items*.json を複数読み込むことを想定し、
            // Resources/Content 配下からまとめて取得する。
            var assets = Resources.LoadAll<TextAsset>("Content");
            if (assets == null || assets.Length == 0)
            {
                FailItemInitialization("Resources/Content に items JSON (TextAsset) が見つかりません。期待: content_items_*.json");
            }

            var dict = ItemJsonLoader.LoadFromTextAssets(assets);
            if (dict == null || dict.Count == 0)
            {
                FailItemInitialization("Resources/Content の items JSON の読み込み結果が空です。JSON の内容・スキーマ・パースエラーを確認してください。");
            }

            ValidateItemSpriteIds(dict);

            // Core 側のヘルパーを通じて、静的プロパティへ反映する。
            ItemDefinition.ApplyDefinitions(dict);
        }

        private static void ValidateItemSpriteIds(IReadOnlyDictionary<string, ItemDefinition> dict)
        {
            if (dict == null)
            {
                throw new ArgumentNullException(nameof(dict));
            }

            var spriteCatalog = SpriteCatalog.LoadFromResources();

            foreach (var pair in dict)
            {
                var itemId = pair.Key;
                var definition = pair.Value;
                if (definition == null)
                {
                    FailItemInitialization($"ItemDefinition が null です: {itemId}");
                }

                try
                {
                    _ = spriteCatalog.GetSpriteOrThrow(definition.SpriteId);
                }
                catch (Exception e)
                {
                    FailItemInitialization($"Item sprite_id が未解決です: itemId={itemId}, sprite_id={definition.SpriteId}\n{e}");
                }
            }
        }

        [DoesNotReturn]
        private static void FailItemInitialization(string message)
        {
            Debug.LogError(message);
            throw new InvalidOperationException(message);
        }

        /// <summary>
        /// Resources に配置された JSON から Actor 定義を読み込み、
        /// ActorDefinition のテーブルを構築する。
        /// 本チケット時点では、呼び出してテーブルを構築するのみで、
        /// 具体的なスポーンや戦闘ロジックへの接続は行わない。
        /// </summary>
        private void InitializeActorsFromJson()
        {
            var assets = Resources.LoadAll<TextAsset>("Actors");
            if (assets == null || assets.Length == 0)
            {
                FailActorInitialization("Resources/Actors に Actor JSON (TextAsset) が見つかりません。期待: actors_*.json");
            }

            IReadOnlyDictionary<string, ActorDefinition> dict;
            try
            {
                dict = ActorJsonLoader.LoadFromTextAssets(assets);
            }
            catch (Exception e)
            {
                FailActorInitialization($"Actor JSON の読み込みに失敗しました: {e.Message}");
                return;
            }

            // Core へ反映（fail-fast: 空や未初期化のまま進めない）。
            ActorDefinition.ApplyDefinitions(dict);
        }

        [DoesNotReturn]
        private static void FailActorInitialization(string message)
        {
            Debug.LogError(message);
            throw new InvalidOperationException(message);
        }

        private static void InitializeSpriteCatalogFromResources()
        {
            try
            {
                _ = SpriteCatalog.LoadFromResources();
            }
            catch (Exception e)
            {
                Debug.LogError($"SpriteCatalog の初期化に失敗しました: {e}");
                throw;
            }
        }

        private static void InitializeAudioCatalogFromResources()
        {
            try
            {
                _ = AudioCatalog.LoadFromResources();
            }
            catch (Exception e)
            {
                Debug.LogError($"AudioCatalog の初期化に失敗しました: {e}");
                throw;
            }
        }
    }
}
