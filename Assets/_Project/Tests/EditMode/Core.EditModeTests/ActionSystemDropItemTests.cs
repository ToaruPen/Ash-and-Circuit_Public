using NUnit.Framework;
using AshNCircuit.Core.Entities;
using AshNCircuit.Core.GameLoop;
using AshNCircuit.Core.Items;
using AshNCircuit.Core.Map;
using AshNCircuit.Core.Systems;

namespace AshNCircuit.Tests.Core
{
    public class ActionSystemDropItemTests
    {
        private static readonly GridPosition[] DropOffsets = new[]
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

        [Test]
        public void TryDropItem_Succeeds_AndCreatesItemPileAtFeet()
        {
            var map = new MapManager(10, 10);
            var log = new LogSystem();
            var actionSystem = new ActionSystem();

            var player = new PlayerEntity(5, 5);
            Assert.That(player.Inventory.TryAdd(ItemDefinition.WoodenArrow, 1), Is.True);

            var dropped = actionSystem.TryDropItem(
                player,
                ItemDefinition.WoodenArrow,
                amount: 1,
                dropTurn: 0,
                mapManager: map,
                logSystem: log);

            Assert.That(dropped, Is.True);
            Assert.That(player.Inventory.GetTotalCount(ItemDefinition.WoodenArrow), Is.EqualTo(0));
            Assert.That(map.TryGetItemPile(5, 5, out var pile), Is.True);
            Assert.That(pile, Is.Not.Null);
            Assert.That(pile!.RepresentativeItem, Is.EqualTo(ItemDefinition.WoodenArrow));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        public void TryDropItem_SlidesToFirstAvailableTile_InFixedOrder(int expectedIndex)
        {
            var map = new MapManager(10, 10);
            var log = new LogSystem();
            var actionSystem = new ActionSystem();

            var player = new PlayerEntity(5, 5);
            Assert.That(player.Inventory.TryAdd(ItemDefinition.WoodenArrow, 1), Is.True);

            for (var i = 0; i < expectedIndex; i++)
            {
                var offset = DropOffsets[i];
                map.SetTileType(player.X + offset.X, player.Y + offset.Y, TileType.WallStone);
            }

            var dropped = actionSystem.TryDropItem(
                player,
                ItemDefinition.WoodenArrow,
                amount: 1,
                dropTurn: 0,
                mapManager: map,
                logSystem: log);

            Assert.That(dropped, Is.True);

            var expected = DropOffsets[expectedIndex];
            var expectedX = player.X + expected.X;
            var expectedY = player.Y + expected.Y;

            Assert.That(player.Inventory.GetTotalCount(ItemDefinition.WoodenArrow), Is.EqualTo(0));
            Assert.That(map.TryGetItemPile(expectedX, expectedY, out _), Is.True);
        }

        [Test]
        public void TryDropItem_AddsToExistingPile_AndKeepsOldestRepresentative()
        {
            var map = new MapManager(10, 10);
            var log = new LogSystem();
            var actionSystem = new ActionSystem();

            var player = new PlayerEntity(3, 3);
            Assert.That(player.Inventory.TryAdd(ItemDefinition.WoodenArrow, 1), Is.True);

            var pile = new ItemPile();
            pile.Add(ItemDefinition.ShortSword, amount: 1, dropTurn: 0);
            Assert.That(map.TryAddItemPile(3, 3, pile), Is.True);
            Assert.That(pile.RepresentativeItem, Is.EqualTo(ItemDefinition.ShortSword));

            var dropped = actionSystem.TryDropItem(
                player,
                ItemDefinition.WoodenArrow,
                amount: 1,
                dropTurn: 5,
                mapManager: map,
                logSystem: log);

            Assert.That(dropped, Is.True);
            Assert.That(pile.RepresentativeItem, Is.EqualTo(ItemDefinition.ShortSword));
            Assert.That(pile.Entries.Count, Is.EqualTo(2));
        }

        [Test]
        public void GameController_DropItem_ConsumesTurn_AndPlacesItemPile()
        {
            var map = new MapManager(10, 10);
            var log = new LogSystem();
            var actionSystem = new ActionSystem();
            var effectSystem = new EffectSystem();
            var turnManager = new TurnManager();
            var gameController = new GameController(turnManager, actionSystem, effectSystem, map, log);

            var player = new PlayerEntity(5, 5);
            gameController.SetPlayerEntity(player);
            Assert.That(player.Inventory.TryAdd(ItemDefinition.WoodenArrow, 1), Is.True);

            gameController.QueueDropItem(ItemDefinition.WoodenArrow);
            gameController.AdvanceTurn();

            Assert.That(turnManager.CurrentTurn, Is.EqualTo(1));
            Assert.That(map.TryGetItemPile(5, 5, out var pile), Is.True);
            Assert.That(pile, Is.Not.Null);
            Assert.That(pile!.RepresentativeItem, Is.EqualTo(ItemDefinition.WoodenArrow));
        }
    }
}

