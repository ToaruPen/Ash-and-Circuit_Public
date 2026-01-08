using AshNCircuit.Core.Map;
using AshNCircuit.Core.Systems;
using UnityEngine;

namespace AshNCircuit.UnityIntegration.MapView
{
    public sealed partial class PlayerInputController
    {
        private void Execute(Intent intent)
        {
            switch (intent.Kind)
            {
                case IntentKind.ToggleInventory:
                    ToggleInventory();
                    return;
                case IntentKind.CloseContextMenu:
                    _contextMenuView?.Hide();
                    return;
                case IntentKind.ShowTileContextMenu:
                    ShowTileContextMenu(intent.TargetTile, intent.ScreenPosition);
                    return;
                case IntentKind.PickupAtFeet:
                    ExecutePickupAtFeet();
                    return;
                case IntentKind.ShootAtTile:
                    ExecuteShootAtTile(intent.TargetTile);
                    return;
                case IntentKind.ShootDirectional:
                    ExecuteShootDirectional(intent.DeltaX, intent.DeltaY);
                    return;
                case IntentKind.MoveDirectional:
                    ExecuteMoveDirectional(intent.DeltaX, intent.DeltaY);
                    return;
                case IntentKind.ThrowTargetingTick:
                    if (_throwTargetingController == null)
                    {
                        return;
                    }

                    _throwTargetingController.HandleInput(intent.Keyboard, intent.Mouse);
                    return;
                default:
                    return;
            }
        }

        private void ExecutePickupAtFeet()
        {
            _gameController.QueuePickup();
            _gameController.AdvanceTurn();
            _inventoryView?.Refresh();
        }

        private void ExecuteShootAtTile(GridPosition targetTile)
        {
            _gameController.QueueShootAtTile(targetTile);
            _gameController.AdvanceTurn();
            TryPlayLastProjectileAnimation();
        }

        private void ExecuteShootDirectional(int deltaX, int deltaY)
        {
            _gameController.QueueShootDirectional(deltaX, deltaY);
            _gameController.AdvanceTurn();
            TryPlayLastProjectileAnimation();
        }

        private void ExecuteMoveDirectional(int deltaX, int deltaY)
        {
            _gameController.QueueMove(deltaX, deltaY);
            _gameController.AdvanceTurn();
            SyncPlayerTransform();
            ShowPickupGuideIfAvailable();
        }

        /// <summary>
        /// タイル右クリック時にコンテキストメニューを表示する。
        /// </summary>
        private void ShowTileContextMenu(GridPosition targetTile, Vector2 screenPos)
        {
            if (_contextMenuView == null || _tileContextMenuController == null)
            {
                return;
            }

            var playerPos = new GridPosition(_player.X, _player.Y);
            var items = _tileContextMenuController.BuildContextMenuItems(targetTile, playerPos);

            if (items.Count > 0)
            {
                _contextMenuView.Show(screenPos, items);
            }
        }

        private void ToggleInventory()
        {
            if (_inventoryView == null)
            {
                return;
            }

            var nextVisible = !_inventoryView.IsVisible;
            _inventoryView.SetVisible(nextVisible);
            if (nextVisible)
            {
                _inventoryView.Refresh();
            }
        }

        private void SyncPlayerTransform()
        {
            var x = _player.X * _tileSize;
            var y = _player.Y * _tileSize;
            _playerTransform.position = new Vector3(x, y, _playerTransform.position.z);
        }

        /// <summary>
        /// 足元に拾えるアイテムがある場合、1回だけガイドログを表示する。
        /// </summary>
        private void ShowPickupGuideIfAvailable()
        {
            var currentPos = new GridPosition(_player.X, _player.Y);

            // 同じ位置で既にガイドを出した場合はスキップ。
            if (_lastPickupGuidePosition.HasValue && _lastPickupGuidePosition.Value.Equals(currentPos))
            {
                return;
            }

            if (_mapManager.TryGetPickupItem(_player.X, _player.Y, out var itemDef) && itemDef != null)
            {
                _logSystem.LogById(MessageId.PickupGuideAtFeet, itemDef.DisplayName);
                _lastPickupGuidePosition = currentPos;
            }
            else
            {
                _lastPickupGuidePosition = null;
            }
        }

        private void TryPlayLastProjectileAnimation()
        {
            if (!_gameController.TryConsumeLastPlayerProjectile(out var result, out var direction))
            {
                return;
            }

            if (result == null)
            {
                return;
            }

            var dirVector = new Vector2(direction.X, direction.Y);
            _playProjectileAnimation(result, dirVector);
        }
    }
}
