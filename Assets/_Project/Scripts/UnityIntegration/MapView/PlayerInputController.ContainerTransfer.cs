using System;
using AshNCircuit.Core.Items;
using AshNCircuit.Core.Map;

namespace AshNCircuit.UnityIntegration.MapView
{
    public sealed partial class PlayerInputController
    {
        private enum ContainerTransferKind
        {
            None,
            TakeFromContainer,
            StoreToContainer,
        }

        private ContainerTransferKind _pendingContainerTransferKind = ContainerTransferKind.None;
        private ItemDefinition? _pendingContainerTransferItem;
        private GridPosition _pendingContainerTransferTile;

        private void InitializeContainerTransferIfAvailable()
        {
            if (_inventoryView == null)
            {
                return;
            }

            _inventoryView.OnTakeFromContainerRequested += HandleTakeFromContainerRequested;
            _inventoryView.OnStoreToContainerRequested += HandleStoreToContainerRequested;

            // 取り出し/しまいは「プレイヤーフェーズ内で 1 回だけ実行→クリア」する。
            // GameController / TurnManager は LOCKED のため編集せず、購読のみで整合を担保する。
            _gameController.TurnManager.OnPlayerPhase += HandleContainerTransferPlayerPhase;
        }

        private void HandleTakeFromContainerRequested(ItemDefinition item)
        {
            if (item == null || _inventoryView == null)
            {
                return;
            }

            if (!_inventoryView.TryGetOpenContainer(out var tile))
            {
                return;
            }

            _pendingContainerTransferKind = ContainerTransferKind.TakeFromContainer;
            _pendingContainerTransferItem = item;
            _pendingContainerTransferTile = tile;

            _gameController.AdvanceTurn();
            _inventoryView.Refresh();
        }

        private void HandleStoreToContainerRequested(ItemDefinition item)
        {
            if (item == null || _inventoryView == null)
            {
                return;
            }

            if (!_inventoryView.TryGetOpenContainer(out var tile))
            {
                return;
            }

            _pendingContainerTransferKind = ContainerTransferKind.StoreToContainer;
            _pendingContainerTransferItem = item;
            _pendingContainerTransferTile = tile;

            _gameController.AdvanceTurn();
            _inventoryView.Refresh();
        }

        private void HandleContainerTransferPlayerPhase()
        {
            if (_pendingContainerTransferKind == ContainerTransferKind.None || _pendingContainerTransferItem == null)
            {
                return;
            }

            var kind = _pendingContainerTransferKind;
            var item = _pendingContainerTransferItem;
            var tile = _pendingContainerTransferTile;

            _pendingContainerTransferKind = ContainerTransferKind.None;
            _pendingContainerTransferItem = null;
            _pendingContainerTransferTile = default;

            var dx = Math.Abs(tile.X - _player.X);
            var dy = Math.Abs(tile.Y - _player.Y);
            if (dx > 1 || dy > 1)
            {
                return;
            }

            if (!_mapManager.TryGetProp(tile.X, tile.Y, out var prop) || prop == null || !prop.IsContainer)
            {
                return;
            }

            var inventory = _player.Inventory;

            switch (kind)
            {
                case ContainerTransferKind.TakeFromContainer:
                    prop.TryTakeOneFromContainerToInventory(item, inventory);
                    return;
                case ContainerTransferKind.StoreToContainer:
                    prop.TryStoreOneFromInventoryToContainer(item, inventory);
                    return;
                default:
                    return;
            }
        }
    }
}

