using AshNCircuit.Core.Entities;
using AshNCircuit.Core.Items;
using AshNCircuit.Core.Map;
using AshNCircuit.Core.Systems;
using AshNCircuit.Core.GameLoop;
using AshNCircuit.UnityIntegration.UI.InventoryUi;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AshNCircuit.UnityIntegration.MapView
{
    /// <summary>
    /// インベントリからの「投げる」操作中に、ターゲット指定と投擲実行を担当するコントローラ。
    /// 入力処理と状態管理のみを行い、実際のアニメーション再生は委譲先のデリゲート経由で行う。
    /// </summary>
    public sealed class ThrowTargetingController
    {
        private readonly PlayerEntity _player;
        private readonly MapManager _mapManager;
        private readonly LogSystem _logSystem;
        private readonly MapViewPresenter? _mapView;
        private readonly InventoryView? _inventoryView;
        private readonly System.Action<ProjectileResult, Vector2> _playProjectileAnimation;
        private readonly float _tileSize;
        private readonly GameController _gameController;

        private ItemDefinition? _targetingItem;

        public bool IsTargeting => _targetingItem != null;

        public ThrowTargetingController(
            PlayerEntity player,
            MapManager mapManager,
            LogSystem logSystem,
            MapViewPresenter? mapView,
            InventoryView? inventoryView,
            System.Action<ProjectileResult, Vector2> playProjectileAnimation,
            float tileSize,
            GameController gameController)
        {
            _player = player;
            _mapManager = mapManager;
            _logSystem = logSystem;
            _mapView = mapView;
            _inventoryView = inventoryView;
            _playProjectileAnimation = playProjectileAnimation;
            _tileSize = tileSize;
            _gameController = gameController;
        }

        public void BeginTargeting(ItemDefinition item)
        {
            if (item == null || _player == null)
            {
                return;
            }

            _targetingItem = item;
            _logSystem?.LogById(MessageId.ThrowModeBegin, item.DisplayName);
        }

        public void HandleInput(Keyboard? keyboard, Mouse? mouse)
        {
            if (!IsTargeting)
            {
                return;
            }

            // 右クリックまたはEscでキャンセル
            if ((mouse != null && mouse.rightButton.wasPressedThisFrame) ||
                (keyboard != null && keyboard.escapeKey.wasPressedThisFrame))
            {
                CancelTargeting();
                return;
            }

            // 左クリックでターゲット確定
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                var worldPos = GetMouseWorldPosition(mouse);
                var targetTile = WorldToGrid(worldPos);
                ExecuteThrow(targetTile);
            }
        }

        private void CancelTargeting()
        {
            _targetingItem = null;
            _logSystem?.LogById(MessageId.ThrowModeCanceled);
        }

        private void ExecuteThrow(GridPosition targetTile)
        {
            if (_targetingItem == null || _player == null || _gameController == null)
            {
                CancelTargeting();
                return;
            }

            _gameController.QueueThrowItem(_targetingItem, targetTile);
            _gameController.AdvanceTurn();
            TryPlayLastProjectileAnimation();

            _mapView?.RefreshAllTiles();
            _inventoryView?.Refresh();

            // ターゲット指定モードを終了
            _targetingItem = null;
        }

        private Vector3 GetMouseWorldPosition(Mouse? mouse)
        {
            if (mouse == null)
            {
                return Vector3.zero;
            }

            var mousePos = mouse.position.ReadValue();
            var camera = Camera.main;
            if (camera == null)
            {
                return Vector3.zero;
            }

            var worldPos = camera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, -camera.transform.position.z));
            return worldPos;
        }

        private GridPosition WorldToGrid(Vector3 worldPos)
        {
            var gridX = Mathf.RoundToInt(worldPos.x / _tileSize);
            var gridY = Mathf.RoundToInt(worldPos.y / _tileSize);
            return new GridPosition(gridX, gridY);
        }

        private void TryPlayLastProjectileAnimation()
        {
            if (_gameController == null)
            {
                return;
            }

            if (!_gameController.TryConsumeLastPlayerProjectile(out var result, out var direction))
            {
                return;
            }

            if (result == null)
            {
                return;
            }

            var dirVector = new Vector2(direction.X, direction.Y);
            _playProjectileAnimation?.Invoke(result, dirVector);
        }
    }
}
