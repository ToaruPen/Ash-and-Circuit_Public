using NUnit.Framework;
using AshNCircuit.Core.Entities;
using AshNCircuit.Core.Items;
using AshNCircuit.Core.Map;
using AshNCircuit.Core.Systems;

namespace AshNCircuit.Tests.Core
{
    /// <summary>
    /// ActionSystem.TryPickupItem の基本挙動を検証するテスト。
    /// ground_* 系タイルでのみ土くれを拾えることや、インベントリ満杯時の挙動を確認する。
    /// </summary>
    public class ActionSystemPickupTests
    {
        [Test]
        public void TryPickupItem_Succeeds_OnGroundNormal_AndAddsDirtClod()
        {
            var map = new MapManager(8, 8);
            var log = new LogSystem();
            var actionSystem = new ActionSystem();

            const int x = 2;
            const int y = 2;
            map.SetTileType(x, y, TileType.GroundNormal);

            var player = new PlayerEntity(x, y);

            var picked = actionSystem.TryPickupItem(player, map, log);

            Assert.That(picked, Is.True, "Pickup on ground_normal should succeed.");
            Assert.That(player.Inventory.GetTotalCount(ItemDefinition.DirtClod), Is.EqualTo(1));
            Assert.That(log.LoggedMessageIds, Has.Some.EqualTo(MessageId.PickupDirtGeneric));
        }

        [Test]
        public void TryPickupItem_Succeeds_OnGroundOil_WithOilyFlavorLog()
        {
            var map = new MapManager(8, 8);
            var log = new LogSystem();
            var actionSystem = new ActionSystem();

            const int x = 3;
            const int y = 3;
            map.SetTileType(x, y, TileType.GroundOil);

            var player = new PlayerEntity(x, y);

            var picked = actionSystem.TryPickupItem(player, map, log);

            Assert.That(picked, Is.True, "Pickup on ground_oil should succeed.");
            Assert.That(player.Inventory.GetTotalCount(ItemDefinition.DirtClod), Is.EqualTo(1));
            Assert.That(log.LoggedMessageIds, Has.Some.EqualTo(MessageId.PickupDirtFromOilGround));
        }

        [Test]
        public void TryPickupItem_Fails_WhenNoPickupItemOnTile()
        {
            var map = new MapManager(8, 8);
            var log = new LogSystem();
            var actionSystem = new ActionSystem();

            // TryGetPickupItem が false を返すタイル（壁）を足元に配置する。
            const int x = 4;
            const int y = 4;
            map.SetTileType(x, y, TileType.WallStone);

            var player = new PlayerEntity(x, y);

            var picked = actionSystem.TryPickupItem(player, map, log);

            Assert.That(picked, Is.False, "Pickup where TryGetPickupItem returns false should fail.");
            Assert.That(player.Inventory.GetTotalCount(ItemDefinition.DirtClod), Is.EqualTo(0));
            Assert.That(log.LoggedMessageIds, Has.Some.EqualTo(MessageId.PickupNoItem));
        }

        [Test]
        public void TryPickupItem_Fails_WhenInventoryIsFull()
        {
            var map = new MapManager(8, 8);
            var log = new LogSystem();
            var actionSystem = new ActionSystem();

            // デフォルトの Inventory.MaxStacks / DirtClod.MaxStack を利用し、
            // すべてのスタックを DirtClod で埋めることで「新たに追加できない」状態を作る。
            var player = new PlayerEntity(1, 1);
            var maxStacks = player.Inventory.MaxStacks;
            var perStack = ItemDefinition.DirtClod.MaxStack;
            player.Inventory.TryAdd(ItemDefinition.DirtClod, maxStacks * perStack);

            const int x = 2;
            const int y = 2;
            map.SetTileType(x, y, TileType.GroundNormal);
            player.SetPosition(x, y);

            var picked = actionSystem.TryPickupItem(player, map, log);

            Assert.That(picked, Is.False, "Pickup should fail when inventory cannot accept more items.");
            Assert.That(log.LoggedMessageIds, Has.Some.EqualTo(MessageId.PickupInventoryFull));
        }
    }
}
