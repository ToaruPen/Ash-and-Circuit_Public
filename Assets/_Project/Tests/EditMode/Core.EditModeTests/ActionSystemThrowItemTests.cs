using NUnit.Framework;
using AshNCircuit.Core.Entities;
using AshNCircuit.Core.Items;
using AshNCircuit.Core.Map;
using AshNCircuit.Core.Systems;

namespace AshNCircuit.Tests.Core
{
    /// <summary>
    /// ActionSystem.TryThrowItem の基本挙動を検証するテスト。
    /// アイテム種別ごとに、インベントリ消費・タイル変化・ログメッセージが
    /// 仕様通りであることを確認する。
    /// </summary>
    public class ActionSystemThrowItemTests
    {
        [Test]
        public void TryThrowItem_Fails_WhenItemNotOwned()
        {
            var map = new MapManager(8, 8);
            var log = new LogSystem();
            var actionSystem = new ActionSystem();
            var player = new PlayerEntity(1, 1);

            var target = new GridPosition(3, 1);

            var result = actionSystem.TryThrowItem(
                player,
                ItemDefinition.WoodenArrow,
                target,
                map,
                log);

            Assert.That(result, Is.Null, "Throwing item that player does not own should fail.");
            Assert.That(log.LoggedMessageIds, Has.Some.EqualTo(MessageId.ThrowMissingItem));
        }

        [Test]
        public void TryThrowItem_Fails_WhenTargetIsSameTile()
        {
            var map = new MapManager(8, 8);
            var log = new LogSystem();
            var actionSystem = new ActionSystem();
            var player = new PlayerEntity(2, 2);

            player.Inventory.TryAdd(ItemDefinition.WoodenArrow, 1);
            var target = new GridPosition(2, 2);

            var result = actionSystem.TryThrowItem(
                player,
                ItemDefinition.WoodenArrow,
                target,
                map,
                log);

            Assert.That(result, Is.Null, "Throwing to own tile should fail.");
            Assert.That(log.LoggedMessageIds, Has.Some.EqualTo(MessageId.ThrowTargetIsSelf));
        }

        [Test]
        public void TryThrowItem_Fails_WhenTargetBeyondRange()
        {
            var map = new MapManager(32, 32);
            var log = new LogSystem();
            var actionSystem = new ActionSystem();
            var player = new PlayerEntity(1, 1);

            player.Inventory.TryAdd(ItemDefinition.WoodenArrow, 1);

            // DefaultThrowRange は 5 なので、距離 6 で失敗を確認する。
            var target = new GridPosition(7, 1);

            var result = actionSystem.TryThrowItem(
                player,
                ItemDefinition.WoodenArrow,
                target,
                map,
                log);

            Assert.That(result, Is.Null, "Throwing beyond range should fail.");
            Assert.That(log.LoggedMessageIds, Has.Some.EqualTo(MessageId.ThrowTooFar));
        }

        [Test]
        public void TryThrowItem_WoodenArrow_SucceedsWithinRange_ConsumesItemAndCreatesProjectile()
        {
            var map = new MapManager(16, 16);
            var log = new LogSystem();
            var actionSystem = new ActionSystem();
            var player = new PlayerEntity(1, 1);

            player.Inventory.TryAdd(ItemDefinition.WoodenArrow, 2);
            var target = new GridPosition(4, 1);

            var result = actionSystem.TryThrowItem(
                player,
                ItemDefinition.WoodenArrow,
                target,
                map,
                log);

            Assert.That(result, Is.Not.Null, "Throwing wooden arrow within range should create projectile result.");
            Assert.That(player.Inventory.GetTotalCount(ItemDefinition.WoodenArrow), Is.EqualTo(1));
            Assert.That(log.LoggedMessageIds, Has.Some.EqualTo(MessageId.ThrowWoodenArrowFlavor));
        }

        [Test]
        public void TryThrowItem_OilBottle_OnGroundTile_CreatesOilPuddle()
        {
            var map = new MapManager(16, 16);
            var log = new LogSystem();
            var actionSystem = new ActionSystem();
            var player = new PlayerEntity(5, 5);

            player.Inventory.TryAdd(ItemDefinition.OilBottle, 1);

            var target = new GridPosition(6, 5);
            map.SetTileType(target.X, target.Y, TileType.GroundNormal);

            var result = actionSystem.TryThrowItem(
                player,
                ItemDefinition.OilBottle,
                target,
                map,
                log);

            Assert.That(result, Is.Null, "Oil bottle throw is environment-only; no projectile expected.");
            Assert.That(map.GetTileType(target.X, target.Y), Is.EqualTo(TileType.GroundOil));
            Assert.That(player.Inventory.GetTotalCount(ItemDefinition.OilBottle), Is.EqualTo(0));
            Assert.That(log.LoggedMessageIds, Has.Some.EqualTo(MessageId.ThrowOilBottleCreatePuddle));
            // CoversRules: [RULE E-02]
        }

        [Test]
        public void TryThrowItem_OilBottle_OnNonGroundTile_DoesNotCreateOilPuddle()
        {
            var map = new MapManager(16, 16);
            var log = new LogSystem();
            var actionSystem = new ActionSystem();
            var player = new PlayerEntity(5, 5);

            player.Inventory.TryAdd(ItemDefinition.OilBottle, 1);

            var target = new GridPosition(6, 5);
            map.SetTileType(target.X, target.Y, TileType.WallStone);

            var result = actionSystem.TryThrowItem(
                player,
                ItemDefinition.OilBottle,
                target,
                map,
                log);

            Assert.That(result, Is.Null);
            Assert.That(map.GetTileType(target.X, target.Y), Is.EqualTo(TileType.WallStone));
            Assert.That(player.Inventory.GetTotalCount(ItemDefinition.OilBottle), Is.EqualTo(0));
            Assert.That(log.LoggedMessageIds, Has.Some.EqualTo(MessageId.ThrowOilBottleNoSpread));
        }

        [Test]
        public void TryThrowItem_OilBottle_OutOfBounds_ConsumesItemButDoesNotChangeMap()
        {
            var map = new MapManager(8, 8);
            var log = new LogSystem();
            var actionSystem = new ActionSystem();
            var player = new PlayerEntity(1, 1);

            player.Inventory.TryAdd(ItemDefinition.OilBottle, 1);

            var target = new GridPosition(-1, -1);

            var result = actionSystem.TryThrowItem(
                player,
                ItemDefinition.OilBottle,
                target,
                map,
                log);

            Assert.That(result, Is.Null);
            Assert.That(player.Inventory.GetTotalCount(ItemDefinition.OilBottle), Is.EqualTo(0));
            Assert.That(log.LoggedMessageIds, Has.Some.EqualTo(MessageId.ThrowOilBottleLost));
        }

        [Test]
        public void TryThrowItem_DirtClod_ConsumesItemAndOnlyLogsFlavor()
        {
            var map = new MapManager(16, 16);
            var log = new LogSystem();
            var actionSystem = new ActionSystem();
            var player = new PlayerEntity(5, 5);

            player.Inventory.TryAdd(ItemDefinition.DirtClod, 1);

            var target = new GridPosition(6, 5);
            var originalTile = map.GetTileType(target.X, target.Y);

            var result = actionSystem.TryThrowItem(
                player,
                ItemDefinition.DirtClod,
                target,
                map,
                log);

            Assert.That(result, Is.Null);
            Assert.That(player.Inventory.GetTotalCount(ItemDefinition.DirtClod), Is.EqualTo(0));
            Assert.That(map.GetTileType(target.X, target.Y), Is.EqualTo(originalTile));
            Assert.That(log.LoggedMessageIds, Has.Some.EqualTo(MessageId.ThrowDirtClodFlavor));
        }

        [Test]
        public void TryThrowItem_OtherItem_DoesNotConsumeItemAndLogsNoSpecialEffect()
        {
            var map = new MapManager(16, 16);
            var log = new LogSystem();
            var actionSystem = new ActionSystem();
            var player = new PlayerEntity(5, 5);

            player.Inventory.TryAdd(ItemDefinition.ShortSword, 1);

            var target = new GridPosition(6, 5);

            var result = actionSystem.TryThrowItem(
                player,
                ItemDefinition.ShortSword,
                target,
                map,
                log);

            Assert.That(result, Is.Null);
            Assert.That(player.Inventory.GetTotalCount(ItemDefinition.ShortSword), Is.EqualTo(1));
            Assert.That(log.LoggedMessageIds, Has.Some.EqualTo(MessageId.ThrowGenericNoEffect));
        }
    }
}
