using System;
using System.Collections.Generic;
using AshNCircuit.Core.Entities;
using AshNCircuit.Core.Items;
using AshNCircuit.Core.Map;
using AshNCircuit.Core.Systems;

namespace AshNCircuit.Core.GameLoop
{
    /// <summary>
    /// ゲーム全体の状態管理と、ターンフェーズへの橋渡しを行うクラス。
    /// プレイヤー行動は必ず TurnManager のプレイヤーフェーズ経由で適用される。
    /// </summary>
    public class GameController
    {
        private readonly TurnManager _turnManager;
        private readonly ActionSystem _actionSystem;
        private readonly EffectSystem _effectSystem;
        private readonly MapManager _mapManager;
        private readonly LogSystem _logSystem;

        private PlayerEntity? _player;

        private PlayerActionKind _pendingActionKind = PlayerActionKind.None;
        private int _pendingDeltaX;
        private int _pendingDeltaY;
        private GridPosition _pendingTargetTile;
        private ItemDefinition? _pendingItem;
        private bool _pendingEquipAfterPickup;
        private bool _hasPendingAction;

        private readonly List<ProjectileResult> _projectilesForCurrentTurn =
            new List<ProjectileResult>();

        private ProjectileResult? _lastProjectileResult;
        private GridPosition _lastProjectileDirection;
        private bool _hasLastProjectile;

        public GameController(
            TurnManager turnManager,
            ActionSystem actionSystem,
            EffectSystem effectSystem,
            MapManager mapManager,
            LogSystem logSystem)
        {
            _turnManager = turnManager ?? throw new ArgumentNullException(nameof(turnManager));
            _actionSystem = actionSystem ?? throw new ArgumentNullException(nameof(actionSystem));
            _effectSystem = effectSystem ?? throw new ArgumentNullException(nameof(effectSystem));
            _mapManager = mapManager ?? throw new ArgumentNullException(nameof(mapManager));
            _logSystem = logSystem ?? throw new ArgumentNullException(nameof(logSystem));

            _turnManager.OnPlayerPhase += HandlePlayerPhase;
            _turnManager.OnProjectilePhase += HandleProjectilePhase;
            _turnManager.OnEnvironmentPhase += HandleEnvironmentPhase;
        }

        /// <summary>
        /// 現在のゲームで使用している TurnManager インスタンス。
        /// </summary>
        public TurnManager TurnManager => _turnManager;

        /// <summary>
        /// プレイヤーエンティティを登録する。
        /// プレイヤーフェーズでの行動適用に使用される。
        /// </summary>
        public void SetPlayerEntity(PlayerEntity player)
        {
            _player = player;
        }

        /// <summary>
        /// プレイヤーの移動アクションをキューに積む。
        /// 実際の挙動は次のプレイヤーフェーズで適用される。
        /// </summary>
        public void QueueMove(int deltaX, int deltaY)
        {
            _pendingActionKind = PlayerActionKind.MoveDirectional;
            _pendingDeltaX = deltaX;
            _pendingDeltaY = deltaY;
            _hasPendingAction = true;
        }

        /// <summary>
        /// プレイヤーの待機アクションをキューに積む。
        /// 現時点では「何もしない」ターンとして扱う。
        /// </summary>
        public void QueueWait()
        {
            _pendingActionKind = PlayerActionKind.Wait;
            _hasPendingAction = true;
        }

        /// <summary>
        /// プレイヤーの足元アイテム取得アクションをキューに積む。
        /// </summary>
        public void QueuePickup()
        {
            _pendingActionKind = PlayerActionKind.Pickup;
            _pendingEquipAfterPickup = false;
            _hasPendingAction = true;
        }

        public void QueuePickupFromItemPile(ItemDefinition item, GridPosition targetTile)
        {
            _pendingActionKind = PlayerActionKind.PickupFromItemPile;
            _pendingItem = item;
            _pendingTargetTile = targetTile;
            _pendingEquipAfterPickup = false;
            _hasPendingAction = true;
        }

        public void QueuePickupAndEquipFromItemPile(ItemDefinition item, GridPosition targetTile)
        {
            _pendingActionKind = PlayerActionKind.PickupFromItemPile;
            _pendingItem = item;
            _pendingTargetTile = targetTile;
            _pendingEquipAfterPickup = true;
            _hasPendingAction = true;
        }

        /// <summary>
        /// プレイヤーの 4 方向射撃アクションをキューに積む。
        /// </summary>
        public void QueueShootDirectional(int deltaX, int deltaY)
        {
            _pendingActionKind = PlayerActionKind.ShootDirectional;
            _pendingDeltaX = deltaX;
            _pendingDeltaY = deltaY;
            _hasPendingAction = true;
        }

        /// <summary>
        /// プレイヤーの任意タイル射撃アクションをキューに積む。
        /// </summary>
        public void QueueShootAtTile(GridPosition targetTile)
        {
            _pendingActionKind = PlayerActionKind.ShootTargetTile;
            _pendingTargetTile = targetTile;
            _hasPendingAction = true;
        }

        /// <summary>
        /// プレイヤーの投擲アクションをキューに積む。
        /// </summary>
        public void QueueThrowItem(ItemDefinition item, GridPosition targetTile)
        {
            _pendingActionKind = PlayerActionKind.ThrowItem;
            _pendingItem = item;
            _pendingTargetTile = targetTile;
            _hasPendingAction = true;
        }

        /// <summary>
        /// プレイヤーのインベントリから、指定アイテムを 1 個地面へドロップするアクションをキューに積む。
        /// 実際の挙動は次のプレイヤーフェーズで適用される。
        /// </summary>
        public void QueueDropItem(ItemDefinition item)
        {
            _pendingActionKind = PlayerActionKind.DropItem;
            _pendingItem = item;
            _hasPendingAction = true;
        }

        /// <summary>
        /// プレイヤー入力 1 回分に対応する 1 ターン進行を要求する。
        /// 実際のフェーズ順序は TurnManager.AdvanceTurn 内で管理される。
        /// </summary>
        public void AdvanceTurn()
        {
            var nextTurnNumber = _turnManager.CurrentTurn + 1;
            _logSystem.LogTurnStart(nextTurnNumber);

            _turnManager.AdvanceTurn();

            _logSystem.LogTurnEnd(nextTurnNumber);
        }

        /// <summary>
        /// 直近のプレイヤー射撃／投擲によって生成された ProjectileResult を取得し、1 回だけ消費する。
        /// </summary>
        public bool TryConsumeLastPlayerProjectile(out ProjectileResult? result, out GridPosition direction)
        {
            if (!_hasLastProjectile)
            {
                result = null;
                direction = default;
                return false;
            }

            result = _lastProjectileResult;
            direction = _lastProjectileDirection;

            _hasLastProjectile = false;
            _lastProjectileResult = null;
            _lastProjectileDirection = default;

            return true;
        }

        // ------------------------------------------------------------
        // Turn phases (called from TurnManager)
        // ------------------------------------------------------------

        private void HandlePlayerPhase()
        {
            if (!_hasPendingAction || _player == null)
            {
                return;
            }

            RunPendingPlayerAction(_player);

            _hasPendingAction = false;
            _pendingActionKind = PlayerActionKind.None;
        }

        private void RunPendingPlayerAction(PlayerEntity player)
        {
            switch (_pendingActionKind)
            {
                case PlayerActionKind.MoveDirectional:
                    RunPlayerMove(player);
                    break;

                case PlayerActionKind.Wait:
                    // 現時点では特別な処理は行わない。
                    break;

                case PlayerActionKind.Pickup:
                    RunPlayerPickup(player);
                    break;

                case PlayerActionKind.PickupFromItemPile:
                    RunPlayerPickupFromItemPile(player);
                    break;

                case PlayerActionKind.ShootDirectional:
                    RunPlayerShootDirectional(player);
                    break;

                case PlayerActionKind.ShootTargetTile:
                    RunPlayerShootAtTile(player);
                    break;

                case PlayerActionKind.ThrowItem:
                    RunPlayerThrowItem(player);
                    break;

                case PlayerActionKind.DropItem:
                    RunPlayerDropItem(player);
                    break;
            }
        }

        private void RunPlayerMove(PlayerEntity player)
        {
            _actionSystem.TryMovePlayer(player, _pendingDeltaX, _pendingDeltaY, _mapManager, _logSystem);
        }

        private void RunPlayerPickup(PlayerEntity player)
        {
            _actionSystem.TryPickupItem(player, _mapManager, _logSystem);
        }

        private void RunPlayerPickupFromItemPile(PlayerEntity player)
        {
            if (_pendingItem == null)
            {
                return;
            }

            _actionSystem.TryPickupItemFromItemPile(
                player,
                _pendingTargetTile,
                _pendingItem,
                _pendingEquipAfterPickup,
                _mapManager,
                _logSystem);
        }

        private void RunPlayerShootDirectional(PlayerEntity player)
        {
            var result = _actionSystem.TryShootProjectile(player, _pendingDeltaX, _pendingDeltaY, _mapManager, _logSystem);
            SaveProjectileResult(result, new GridPosition(_pendingDeltaX, _pendingDeltaY));
        }

        private void RunPlayerShootAtTile(PlayerEntity player)
        {
            var range = Math.Max(_mapManager.Width, _mapManager.Height);
            var parameters = ProjectileParams.CreateBasicArrow(range);
            var result = _actionSystem.PerformShootProjectile(player, _pendingTargetTile, parameters, _mapManager, _logSystem);

            var dx = _pendingTargetTile.X - player.X;
            var dy = _pendingTargetTile.Y - player.Y;
            SaveProjectileResult(result, new GridPosition(dx, dy));
        }

        private void RunPlayerThrowItem(PlayerEntity player)
        {
            var result = _actionSystem.TryThrowItem(player, _pendingItem, _pendingTargetTile, _mapManager, _logSystem);

            if (result != null)
            {
                var dx = _pendingTargetTile.X - player.X;
                var dy = _pendingTargetTile.Y - player.Y;
                SaveProjectileResult(result, new GridPosition(dx, dy));
            }
        }

        private void RunPlayerDropItem(PlayerEntity player)
        {
            if (_pendingItem == null)
            {
                return;
            }

            _actionSystem.TryDropItem(
                player,
                _pendingItem,
                amount: 1,
                dropTurn: _turnManager.CurrentTurn,
                mapManager: _mapManager,
                logSystem: _logSystem);
        }

        private void SaveProjectileResult(ProjectileResult? result, GridPosition direction)
        {
            if (result == null)
            {
                _hasLastProjectile = false;
                _lastProjectileResult = null;
                _lastProjectileDirection = default;
                return;
            }

            _lastProjectileResult = result;
            _lastProjectileDirection = direction;
            _hasLastProjectile = true;
            _projectilesForCurrentTurn.Add(result);
        }

        private void HandleProjectilePhase()
        {
            if (_projectilesForCurrentTurn.Count == 0)
            {
                return;
            }

            foreach (var projectile in _projectilesForCurrentTurn)
            {
                _effectSystem.ApplyProjectileEnvironmentRules(projectile, _mapManager, _logSystem);
            }

            _projectilesForCurrentTurn.Clear();
        }

        private void HandleEnvironmentPhase()
        {
            var endOfTurn = _turnManager.CurrentTurn + 1;
            _mapManager.ExpireGroundItemPiles(endOfTurn);
        }

        private enum PlayerActionKind
        {
            None,
            MoveDirectional,
            Wait,
            Pickup,
            PickupFromItemPile,
            ShootDirectional,
            ShootTargetTile,
            ThrowItem,
            DropItem
        }
    }
}
