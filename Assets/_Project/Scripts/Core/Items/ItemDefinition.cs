using System;
using System.Collections.Generic;

namespace AshNCircuit.Core.Items
{
    /// <summary>
    /// コアアイテムの定義クラス。
    /// MVP では短剣・弓・木製の矢・油の入った瓶・土くれのみを扱う。
    /// 将来的には ScriptableObject や JSON 定義へ置き換える前提の軽量なスタブ。
    /// </summary>
    public sealed class ItemDefinition
    {
        /// <summary>
        /// アイテム ID（例: item_short_sword）。
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// 表示名（例: 「短剣」）。
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// 説明文。
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// 表示用スプライトID（例: icon_item_short_sword）。
        /// SpriteCatalog で解決されることを前提とする。
        /// </summary>
        public string SpriteId { get; }

        /// <summary>
        /// タグ一覧（weapon, ranged, ammo, oil, earth など）。
        /// docs/06_content_schema.md および TICKET-0027 の方針に対応。
        /// </summary>
        public IReadOnlyList<string> Tags { get; }

        /// <summary>
        /// スタック可能かどうか。
        /// </summary>
        public bool IsStackable { get; }

        /// <summary>
        /// 1 スタックあたりの最大数（スタック不可のときは常に 1）。
        /// </summary>
        public int MaxStack { get; }

        private ItemDefinition(
            string id,
            string displayName,
            string description,
            string spriteId,
            IReadOnlyList<string> tags,
            bool isStackable,
            int maxStack)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            SpriteId = spriteId;
            Tags = tags;
            IsStackable = isStackable;
            MaxStack = isStackable ? (maxStack > 0 ? maxStack : 1) : 1;
        }

        /// <summary>
        /// JSON 由来のアイテム定義を生成するヘルパー。
        /// docs/06_content_schema.md のスキーマに対応する。
        /// </summary>
        public static ItemDefinition FromJson(
            string id,
            string displayName,
            string description,
            IReadOnlyList<string> tags,
            string spriteId,
            bool isStackable,
            int maxStack)
        {
            if (string.IsNullOrEmpty(spriteId))
            {
                throw new InvalidOperationException($"ItemDefinition: sprite_id が空です（id={id}）。");
            }

            return new ItemDefinition(
                id,
                displayName,
                description,
                spriteId,
                tags ?? new List<string>(),
                isStackable,
                maxStack);
        }

        // --- コアアイテム ID 定数 ---

        public const string ShortSwordId = "item_short_sword";
        public const string BowId = "item_bow_basic";
        public const string WoodenArrowId = "item_arrow_wooden";
        public const string OilBottleId = "item_oil_bottle";
        public const string DirtClodId = "item_dirt_clod";

        // --- コアアイテム定義（JSON からの読み込み結果を格納） ---

        private static ItemDefinition? _shortSword;
        private static ItemDefinition? _bow;
        private static ItemDefinition? _woodenArrow;
        private static ItemDefinition? _oilBottle;
        private static ItemDefinition? _dirtClod;

        /// <summary>
        /// JSON ローダから設定される、短剣の定義。
        /// JSON-only 運用のため、未初期化の参照は例外とする。
        /// </summary>
        public static ItemDefinition ShortSword => _shortSword
            ?? throw new InvalidOperationException("ItemDefinition: 未初期化です（ShortSword）。BootLoader.InitializeItemsFromJson または EditMode SetUpFixture による初期化が必要です。");

        /// <summary>
        /// JSON ローダから設定される、弓の定義。
        /// </summary>
        public static ItemDefinition Bow => _bow
            ?? throw new InvalidOperationException("ItemDefinition: 未初期化です（Bow）。BootLoader.InitializeItemsFromJson または EditMode SetUpFixture による初期化が必要です。");

        /// <summary>
        /// JSON ローダから設定される、木製の矢の定義。
        /// </summary>
        public static ItemDefinition WoodenArrow => _woodenArrow
            ?? throw new InvalidOperationException("ItemDefinition: 未初期化です（WoodenArrow）。BootLoader.InitializeItemsFromJson または EditMode SetUpFixture による初期化が必要です。");

        /// <summary>
        /// JSON ローダから設定される、油の入った瓶の定義。
        /// </summary>
        public static ItemDefinition OilBottle => _oilBottle
            ?? throw new InvalidOperationException("ItemDefinition: 未初期化です（OilBottle）。BootLoader.InitializeItemsFromJson または EditMode SetUpFixture による初期化が必要です。");

        /// <summary>
        /// JSON ローダから設定される、土くれの定義。
        /// </summary>
        public static ItemDefinition DirtClod => _dirtClod
            ?? throw new InvalidOperationException("ItemDefinition: 未初期化です（DirtClod）。BootLoader.InitializeItemsFromJson または EditMode SetUpFixture による初期化が必要です。");

        /// <summary>
        /// JSON から読み込んだアイテム定義テーブルを静的プロパティへ反映する。
        /// JSON-only 運用のため、空・必須ID欠落は例外として検出する。
        /// </summary>
        public static void ApplyDefinitions(IReadOnlyDictionary<string, ItemDefinition> definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            if (definitions.Count == 0)
            {
                throw new InvalidOperationException("ItemDefinition: 定義テーブルが空です。Resources/Content の items JSON を確認してください。");
            }

            var missing = new List<string>();

            if (!definitions.TryGetValue(ShortSwordId, out var shortSword) || shortSword == null)
            {
                missing.Add(ShortSwordId);
            }

            if (!definitions.TryGetValue(BowId, out var bow) || bow == null)
            {
                missing.Add(BowId);
            }

            if (!definitions.TryGetValue(WoodenArrowId, out var woodenArrow) || woodenArrow == null)
            {
                missing.Add(WoodenArrowId);
            }

            if (!definitions.TryGetValue(OilBottleId, out var oilBottle) || oilBottle == null)
            {
                missing.Add(OilBottleId);
            }

            if (!definitions.TryGetValue(DirtClodId, out var dirtClod) || dirtClod == null)
            {
                missing.Add(DirtClodId);
            }

            if (missing.Count > 0)
            {
                throw new InvalidOperationException($"ItemDefinition: 必須アイテムIDが不足しています: {string.Join(", ", missing)}");
            }

            _shortSword = shortSword;
            _bow = bow;
            _woodenArrow = woodenArrow;
            _oilBottle = oilBottle;
            _dirtClod = dirtClod;
        }
    }
}
