namespace AshNCircuit.Core.Systems
{
    /// <summary>
    /// メッセージカタログの抽象インターフェース。
    /// MessageId に対応するテンプレート文字列を返す。
    /// </summary>
    public interface IMessageCatalog
    {
        string GetTemplate(MessageId id);
    }

    /// <summary>
    /// コアメッセージ用のID一覧。
    /// RULE / 挙動との対応が読み取りやすい名前を付ける。
    /// </summary>
    public enum MessageId
    {
        // ターン進行
        TurnStart,
        TurnEnd,

        // RULE P-01 / RULE E-01 関連
        RuleP01ArrowIgnited,
        RuleE01TreeIgnited,
        RuleE01TreeBurnedOut,

        // 移動関連
        MoveOutOfBounds,
        MoveBlockedByWall,
        MoveSucceeded,

        // 足元の拾う関連
        PickupNoItem,
        PickupInventoryFull,
        PickupDirtFromOilGround,
        PickupDirtGeneric,
        PickupGenericItem,

        // 投擲関連
        ThrowMissingItem,
        ThrowTargetIsSelf,
        ThrowTooFar,
        ThrowWoodenArrowFlavor,
        ThrowOilBottleLost,
        ThrowOilBottleCreatePuddle,
        ThrowOilBottleNoSpread,
        ThrowDirtClodFlavor,
        ThrowGenericNoEffect,

        // 射撃関連
        ShootDirectional,
        ShootGeneric,
        ShootBlockedImmediately,
        ShootHitSurface,
        ShootFellToGround,

        // 投擲Projectile共通
        ThrowProjectileDroppedAtFeet,

        // 投擲モードガイド
        ThrowModeBegin,
        ThrowModeCanceled,

        // コンテキストメニュー関連
        ContextPickupNotFromThere,
        ContextExamineTile,

        // UI（インベントリ等）
        UiItemExamined,
        UiTileMenuPickupWithName,
        UiTileMenuExamine,
        UiTileDescGroundNormal,
        UiTileDescGroundBurnt,
        UiTileDescGroundOil,
        UiTileDescGroundWater,
        UiTileDescTreeNormal,
        UiTileDescTreeBurning,
        UiTileDescTreeBurnt,
        UiTileDescWallStone,
        UiTileDescWallMetal,
        UiTileDescFire,
        UiTileDescUnknown,
        UiInventoryMenuThrow,
        UiInventoryMenuEquipWithSlot,
        UiInventoryMenuDrop,
        UiInventoryMenuExamine,
        UiInventoryMenuUnequip,
        UiHudHpValue,
        UiHudXpValue,
        UiHudStatusPlaceholder,
        UiHudHungerPlaceholder,
        UiHudThirstPlaceholder,
        UiHudLevelValue,
        UiHudDefenseValue,
        UiHudEvasionValue,
        UiHudTimeTurn,
        UiHudTimeTurnUnknown,
        UiHudLocationDefault,

        // 足元ガイド
        PickupGuideAtFeet,

        // 投射物命中
        ProjectileHitEnemy,

        // 状態異常関連
        BurningDamagePlayer,
        BurningDamageEnemy,

        // 近接攻撃関連
        MeleePlayerHitEnemyDamage,
        MeleeEnemyDefeated,
        MeleeEnemyHitPlayerDamage,
        MeleePlayerDefeated,
        MeleeHitGenericDamage
    }

    /// <summary>
    /// Dictionary ベースでメッセージテンプレートを解決する IMessageCatalog 実装。
    /// 外部データ（JSON など）から読み込んだマップを渡す用途を想定する。
    /// </summary>
    public sealed class DictionaryMessageCatalog : IMessageCatalog
    {
        private readonly System.Collections.Generic.IDictionary<MessageId, string> _templates;

        public DictionaryMessageCatalog(System.Collections.Generic.IDictionary<MessageId, string> templates)
        {
            _templates = templates ?? new System.Collections.Generic.Dictionary<MessageId, string>();
        }

        public string GetTemplate(MessageId id)
        {
            if (_templates.TryGetValue(id, out var template) && !string.IsNullOrEmpty(template))
            {
                return template;
            }

            throw new System.Collections.Generic.KeyNotFoundException($"MessageCatalog: MessageId '{id}' のテンプレートが見つかりません。");
        }
    }

    /// <summary>
    /// MessageId に対応する日本語テンプレートを管理するカタログ。
    /// 当面はコード内に定義しつつ、将来的に外部テーブルへ差し替えやすい形を保つ。
    /// </summary>
    public static class MessageCatalog
    {
        private static IMessageCatalog? _backend;

        /// <summary>
        /// バックエンドとなる IMessageCatalog 実装を差し替える。
        /// </summary>
        public static void Initialize(IMessageCatalog backend)
        {
            _backend = backend ?? throw new System.ArgumentNullException(nameof(backend));
        }

        public static string Format(MessageId id, params object[] args)
        {
            var template = GetTemplate(id);
            if (string.IsNullOrEmpty(template))
            {
                return string.Empty;
            }

            if (args == null || args.Length == 0)
            {
                return template;
            }

            return string.Format(template, args);
        }

        public static string GetTemplate(MessageId id)
        {
            if (_backend == null)
            {
                throw new System.InvalidOperationException("MessageCatalog: 初期化されていません（BootLoader またはテストセットアップで Initialize が必要です）。");
            }

            return _backend.GetTemplate(id);
        }
    }
}
