using System;
using System.Collections.Generic;
using AshNCircuit.Core.Entities;
using AshNCircuit.Core.Items;
using AshNCircuit.Core.Map;
using AshNCircuit.Core.States;

namespace AshNCircuit.Core.Systems
{
    /// <summary>
    /// 移動・近接攻撃・射撃・アイテム使用など各種アクションを扱うシステムクラス。
    /// MVP初期段階では、プレイヤーの移動のみを実装する。
    /// </summary>
    public class ActionSystem
    {
        private readonly MovementActionSystem _movementActionSystem = new MovementActionSystem();
        private readonly MeleeCombatActionSystem _meleeCombatActionSystem = new MeleeCombatActionSystem();
        private readonly ItemInteractionActionSystem _itemInteractionActionSystem = new ItemInteractionActionSystem();
        private readonly RangedCombatActionSystem _rangedCombatActionSystem = new RangedCombatActionSystem();

        // ------------------------------------------------------------
        // Enemy locator / common guards
        // ------------------------------------------------------------
        /// <summary>
        /// 指定座標に生存中の敵エンティティが存在するかを解決するためのコールバック。
        /// 敵AIシステム（EnemyAISystem）から注入される。
        /// </summary>
        private System.Func<int, int, EnemyEntity?>? _findEnemyAt;

        /// <summary>
        /// マップ上の敵位置を解決するためのロケータを設定する。
        /// </summary>
        public void SetEnemyLocator(System.Func<int, int, EnemyEntity?>? findEnemyAt)
        {
            _findEnemyAt = findEnemyAt;
            _rangedCombatActionSystem.SetEnemyLocator(findEnemyAt);
        }

        private static bool IsInvalidPlayer(PlayerEntity? player)
        {
            return player == null || player.IsDead;
        }

        private static bool IsInvalidEntity(Entity? entity)
        {
            return entity == null || entity.IsDead;
        }

        // ------------------------------------------------------------
        // Movement
        // ------------------------------------------------------------

        /// <summary>
        /// プレイヤーを指定方向に1マス移動させる。
        /// </summary>
        /// <param name="player">移動対象のプレイヤーエンティティ。</param>
        /// <param name="deltaX">X方向の変化量（-1, 0, 1）。</param>
        /// <param name="deltaY">Y方向の変化量（-1, 0, 1）。</param>
        /// <param name="mapManager">通行可否判定に使用するマップマネージャ。</param>
        /// <param name="logSystem">移動結果を記録するログシステム。</param>
        /// <returns>実際に位置が変化した場合は true。</returns>
        public bool TryMovePlayer(PlayerEntity player, int deltaX, int deltaY, MapManager mapManager, LogSystem logSystem)
        {
            if (IsInvalidPlayer(player) || mapManager == null || logSystem == null)
            {
                return false;
            }

            var targetX = player.X + deltaX;
            var targetY = player.Y + deltaY;

            // まず対象マスに敵がいれば、「体当たり移動」ではなく近接攻撃として扱う。
            if (_findEnemyAt != null)
            {
                var enemy = _findEnemyAt(targetX, targetY);
                if (enemy != null && !enemy.IsDead)
                {
                    TryMeleeAttack(player, enemy, logSystem);
                    // 位置は変化しないため false を返す。
                    return false;
                }
            }

            return _movementActionSystem.TryMovePlayerTo(player, targetX, targetY, mapManager, logSystem);
        }

        // ------------------------------------------------------------
        // Melee combat
        // ------------------------------------------------------------

        /// <summary>
        /// 近接攻撃（殴り）のロジックを実行する。
        /// docs/03_combat_and_turns.md のダメージ式
        /// damage = max(1, attacker.atk - target.def)
        /// に従って HP を減少させる。
        /// </summary>
        /// <param name="attacker">攻撃側エンティティ。</param>
        /// <param name="target">防御側エンティティ。</param>
        /// <param name="logSystem">ログ出力先。</param>
        /// <returns>攻撃が実行された場合 true。</returns>
        public bool TryMeleeAttack(Entity attacker, Entity target, LogSystem logSystem)
        {
            return _meleeCombatActionSystem.TryMeleeAttack(attacker, target, logSystem);
        }

        // ------------------------------------------------------------
        // Prop interactions (containers)
        // ------------------------------------------------------------

        public bool TryOpenContainer(
            PlayerEntity player,
            GridPosition targetTile,
            WorldRngState worldRng,
            MapManager mapManager,
            LogSystem logSystem)
        {
            if (IsInvalidPlayer(player) || mapManager == null || logSystem == null || worldRng == null)
            {
                return false;
            }

            var dx = Math.Abs(player.X - targetTile.X);
            var dy = Math.Abs(player.Y - targetTile.Y);
            if (dx > 1 || dy > 1)
            {
                return false;
            }

            if (!mapManager.TryGetProp(targetTile.X, targetTile.Y, out var prop) || prop == null)
            {
                return false;
            }

            return prop.TryEnsureContainerLootRolled(worldRng, targetTile.X, targetTile.Y);
        }

        // ------------------------------------------------------------
        // Inventory interactions
        // ------------------------------------------------------------

        /// <summary>
        /// プレイヤーの足元タイルからアイテムを 1 つ拾う。
        /// ground_* 系タイルからの土くれ取得など、TICKET-0027/0028 で定義された最低限の挙動のみを扱う。
        /// </summary>
        public bool TryPickupItem(PlayerEntity player, MapManager mapManager, LogSystem logSystem)
        {
            return _itemInteractionActionSystem.TryPickupItem(player, mapManager, logSystem);
        }

        /// <summary>
        /// 指定タイルの ItemPile から、指定アイテムを 1 個拾う。
        /// - 距離制約は「足元＋8近傍（自マス＋周囲8マス）」のみ許可する。
        /// - ItemPile の消費順は決定的（最古優先）。
        /// </summary>
        public bool TryPickupItemFromItemPile(
            PlayerEntity player,
            GridPosition targetTile,
            ItemDefinition item,
            bool equipAfterPickup,
            MapManager mapManager,
            LogSystem logSystem)
        {
            return _itemInteractionActionSystem.TryPickupItemFromItemPile(
                player,
                targetTile,
                item,
                equipAfterPickup,
                mapManager,
                logSystem);
        }

        public bool TryDropItem(
            PlayerEntity player,
            ItemDefinition item,
            int amount,
            int dropTurn,
            MapManager mapManager,
            LogSystem logSystem)
        {
            return _itemInteractionActionSystem.TryDropItem(
                player,
                item,
                amount,
                dropTurn,
                mapManager,
                logSystem);
        }

        // ------------------------------------------------------------
        // Projectile actions (shoot / throw)
        // ------------------------------------------------------------

        /// <summary>
        /// プレイヤーから指定方向へ矢を放つ（4方向用のラッパー）。
        /// 内部ではターゲットタイルを推定し、PerformShootProjectile に委譲する。
        /// </summary>
        /// <param name="player">射撃を行うプレイヤー。</param>
        /// <param name="deltaX">X方向の変化量（-1, 0, 1）。</param>
        /// <param name="deltaY">Y方向の変化量（-1, 0, 1）。</param>
        /// <param name="mapManager">マップ情報を提供するマネージャ。</param>
        /// <param name="logSystem">射撃ログを出力するログシステム。</param>
        /// <returns>弾道候補列と命中情報を含む ProjectileResult。射撃できなかった場合は null。</returns>
        public ProjectileResult? TryShootProjectile(PlayerEntity player, int deltaX, int deltaY, MapManager mapManager, LogSystem logSystem)
        {
            if (IsInvalidPlayer(player) || mapManager == null || logSystem == null)
            {
                return null;
            }

            if (deltaX == 0 && deltaY == 0)
            {
                return null;
            }

            return _rangedCombatActionSystem.TryShootProjectile(player, deltaX, deltaY, mapManager, logSystem);
        }

        /// <summary>
        /// インベントリ内のアイテムを投擲する。
        /// 木製の矢は既存の Projectile ロジックを再利用して短射程の弾道を描き、
        /// 土くれ・油瓶は簡易的な範囲チェックと環境変化のみを行う。
        /// </summary>
        /// <param name="player">投擲を行うプレイヤー。</param>
        /// <param name="item">投げるアイテム定義。</param>
        /// <param name="targetTile">狙うタイル座標。</param>
        /// <param name="mapManager">マップ情報。</param>
        /// <param name="logSystem">ログ出力先。</param>
        /// <returns>投擲が Projectile を伴う場合はその結果。環境のみ変化する場合や失敗時は null。</returns>
        public ProjectileResult? TryThrowItem(
            PlayerEntity player,
            ItemDefinition? item,
            GridPosition targetTile,
            MapManager mapManager,
            LogSystem logSystem)
        {
            if (IsInvalidPlayer(player) || item == null || mapManager == null || logSystem == null)
            {
                return null;
            }
            return _rangedCombatActionSystem.TryThrowItem(player, item, targetTile, mapManager, logSystem);
        }

        /// <summary>
        /// プレイヤーから指定ターゲットタイルへ矢を放つ。
        /// MapManager から Bresenham ラインに基づく弾道候補列を取得し、最初に blocking タイルに当たった地点で停止する。
        /// </summary>
        public ProjectileResult? PerformShootProjectile(PlayerEntity player, GridPosition targetTile, ProjectileParams parameters, MapManager mapManager, LogSystem logSystem)
        {
            if (IsInvalidPlayer(player) || mapManager == null || logSystem == null || parameters == null)
            {
                return null;
            }
            return _rangedCombatActionSystem.PerformShootProjectile(player, targetTile, parameters, mapManager, logSystem);
        }
    }

    public enum ProjectileImpactKind
    {
        None,
        Enemy,
        BlockingTile,
        Ground,
    }

    /// <summary>
    /// 投射物の結果を表すクラス。
    /// - 弾道候補タイル列
    /// - 実際の命中タイル（または外れ）
    /// - ProjectileEntity 参照
    /// を含む。
    /// </summary>
    public class ProjectileResult
    {
        public ProjectileEntity Projectile { get; }

        /// <summary>
        /// 射手の位置の次のマスから始まる弾道候補タイル列。
        /// </summary>
        public IReadOnlyList<GridPosition> TrajectoryTiles { get; }

        /// <summary>
        /// TrajectoryTiles 内での命中インデックス。外れた場合は null。
        /// </summary>
        public int? ImpactIndex { get; }

        public bool HasImpact => ImpactIndex.HasValue;

        public GridPosition? ImpactTile => ImpactIndex is int index ? TrajectoryTiles[index] : null;

        public ProjectileImpactKind ImpactKind { get; }

        public EnemyEntity? ImpactEnemy { get; }

        public GridPosition FinalTile => new GridPosition(Projectile.X, Projectile.Y);

        public ProjectileResult(ProjectileEntity projectile, IReadOnlyList<GridPosition> trajectoryTiles, int? impactIndex)
            : this(projectile, trajectoryTiles, impactIndex, ProjectileImpactKind.None, null)
        {
        }

        public ProjectileResult(
            ProjectileEntity projectile,
            IReadOnlyList<GridPosition> trajectoryTiles,
            int? impactIndex,
            ProjectileImpactKind impactKind,
            EnemyEntity? impactEnemy)
        {
            Projectile = projectile;
            TrajectoryTiles = trajectoryTiles ?? Array.Empty<GridPosition>();
            ImpactIndex = impactIndex;
            ImpactKind = impactKind;
            ImpactEnemy = impactEnemy;
        }
    }
}
