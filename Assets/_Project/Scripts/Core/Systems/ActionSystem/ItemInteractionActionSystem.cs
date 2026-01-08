using AshNCircuit.Core.Entities;
using AshNCircuit.Core.Items;
using AshNCircuit.Core.Map;
using System;

namespace AshNCircuit.Core.Systems
{
    /// <summary>
    /// インベントリ操作（拾う等）に関わるアクションを扱うサブシステム。
    /// ActionSystem から委譲され、public API/挙動を維持するための分割ステップとして導入する。
    /// </summary>
    internal sealed class ItemInteractionActionSystem
    {
        private static readonly GridPosition[] DropSearchOffsets = new[]
        {
            new GridPosition(0, 0),
            new GridPosition(0, 1),
            new GridPosition(1, 1),
            new GridPosition(1, 0),
            new GridPosition(1, -1),
            new GridPosition(0, -1),
            new GridPosition(-1, -1),
            new GridPosition(-1, 0),
            new GridPosition(-1, 1),
        };

        public bool TryPickupItem(PlayerEntity player, MapManager mapManager, LogSystem logSystem)
        {
            if (player == null || player.IsDead || mapManager == null || logSystem == null)
            {
                return false;
            }

            var inventory = player.Inventory;
            if (inventory == null)
            {
                return false;
            }

            if (!mapManager.TryGetPickupItem(player.X, player.Y, out var itemDefinition) || itemDefinition == null)
            {
                logSystem.LogById(MessageId.PickupNoItem);
                return false;
            }

            var added = inventory.TryAdd(itemDefinition, 1);
            if (!added)
            {
                logSystem.LogById(MessageId.PickupInventoryFull);
                return false;
            }

            // 足元タイルの種類に応じて簡単なフレーバーログを出す。
            var tileType = mapManager.GetGroundType(player.X, player.Y);
            if (itemDefinition == ItemDefinition.DirtClod)
            {
                if (tileType == TileType.GroundOil)
                {
                    logSystem.LogById(MessageId.PickupDirtFromOilGround);
                }
                else
                {
                    logSystem.LogById(MessageId.PickupDirtGeneric);
                }
            }
            else
            {
                logSystem.LogById(MessageId.PickupGenericItem, itemDefinition.DisplayName);
            }

            return true;
        }

        public bool TryDropItem(
            PlayerEntity player,
            ItemDefinition item,
            int amount,
            int dropTurn,
            MapManager mapManager,
            LogSystem logSystem)
        {
            if (player == null || player.IsDead || item == null || mapManager == null || logSystem == null)
            {
                return false;
            }

            if (amount <= 0 || dropTurn < 0)
            {
                return false;
            }

            var inventory = player.Inventory;
            if (inventory == null)
            {
                return false;
            }

            if (!inventory.TryRemove(item, amount))
            {
                return false;
            }

            var dropPos = FindDropPositionOrDefault(player.X, player.Y, mapManager);

            if (mapManager.TryGetItemPile(dropPos.X, dropPos.Y, out var existing) && existing != null)
            {
                existing.Add(item, amount, dropTurn);
                return true;
            }

            var newPile = new ItemPile();
            newPile.Add(item, amount, dropTurn);

            if (!mapManager.TryAddItemPile(dropPos.X, dropPos.Y, newPile))
            {
                inventory.TryAdd(item, amount);
                return false;
            }

            return true;
        }

        private static GridPosition FindDropPositionOrDefault(int startX, int startY, MapManager mapManager)
        {
            foreach (var offset in DropSearchOffsets)
            {
                var x = startX + offset.X;
                var y = startY + offset.Y;

                if (!mapManager.IsWalkable(x, y))
                {
                    continue;
                }

                if (mapManager.TryGetProp(x, y, out var prop) && prop != null)
                {
                    continue;
                }

                return new GridPosition(x, y);
            }

            return new GridPosition(startX, startY);
        }

        public bool TryPickupItemFromItemPile(
            PlayerEntity player,
            GridPosition targetTile,
            ItemDefinition item,
            bool equipAfterPickup,
            MapManager mapManager,
            LogSystem logSystem)
        {
            if (player == null || player.IsDead || item == null || mapManager == null || logSystem == null)
            {
                return false;
            }

            var inventory = player.Inventory;
            if (inventory == null)
            {
                return false;
            }

            var dx = Math.Abs(targetTile.X - player.X);
            var dy = Math.Abs(targetTile.Y - player.Y);
            if (dx > 1 || dy > 1)
            {
                logSystem.LogById(MessageId.ContextPickupNotFromThere);
                return false;
            }

            if (!mapManager.TryGetItemPile(targetTile.X, targetTile.Y, out var pile) || pile == null)
            {
                logSystem.LogById(MessageId.PickupNoItem);
                return false;
            }

            // 先にインベントリへ追加できるかを確認する（失敗時に ItemPile を変化させないため）。
            if (!inventory.TryAdd(item, 1))
            {
                logSystem.LogById(MessageId.PickupInventoryFull);
                return false;
            }

            // 最古エントリ優先で 1 個消費する。
            if (!pile.TryTakeOne(item))
            {
                // 想定外（直前の UI/状態からズレた等）。追加した分はロールバックする。
                inventory.TryRemove(item, 1);
                logSystem.LogById(MessageId.PickupNoItem);
                return false;
            }

            if (pile.IsEmpty)
            {
                mapManager.TryRemoveItemPile(targetTile.X, targetTile.Y);
            }

            if (equipAfterPickup)
            {
                // 装備は「拾う」と同様に、ターン経由の 1 アクションで完結させる。
                // 仕様: 成功時は ItemPile から 1 個減り、装備状態が更新されるまでを同ターン内で完結させる。
                inventory.TryEquip(item);
            }

            logSystem.LogById(MessageId.PickupGenericItem, item.DisplayName);
            return true;
        }
    }
}
