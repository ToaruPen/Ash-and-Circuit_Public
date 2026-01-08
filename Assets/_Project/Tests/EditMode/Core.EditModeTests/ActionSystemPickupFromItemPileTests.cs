using NUnit.Framework;
using AshNCircuit.Core.Entities;
using AshNCircuit.Core.Items;
using AshNCircuit.Core.Map;
using AshNCircuit.Core.Systems;

namespace AshNCircuit.Tests.Core
{
    public class ActionSystemPickupFromItemPileTests
    {
        [Test]
        public void TryPickupItemFromItemPile_Succeeds_WhenAdjacent_AndRemovesPileWhenEmpty()
        {
            var map = new MapManager(8, 8);
            var log = new LogSystem();
            var actionSystem = new ActionSystem();

            var player = new PlayerEntity(2, 2);

            var pile = new ItemPile();
            pile.Add(ItemDefinition.WoodenArrow, amount: 1, dropTurn: 0);
            Assert.That(map.TryAddItemPile(3, 2, pile), Is.True);

            var picked = actionSystem.TryPickupItemFromItemPile(
                player,
                targetTile: new GridPosition(3, 2),
                item: ItemDefinition.WoodenArrow,
                equipAfterPickup: false,
                mapManager: map,
                logSystem: log);

            Assert.That(picked, Is.True);
            Assert.That(player.Inventory.GetTotalCount(ItemDefinition.WoodenArrow), Is.EqualTo(1));
            Assert.That(map.TryGetItemPile(3, 2, out _), Is.False, "Pile should be removed when empty.");
            Assert.That(log.LoggedMessageIds, Has.Some.EqualTo(MessageId.PickupGenericItem));
        }

        [Test]
        public void TryPickupItemFromItemPile_Fails_WhenOutOfRange_AndDoesNotChangePile()
        {
            var map = new MapManager(8, 8);
            var log = new LogSystem();
            var actionSystem = new ActionSystem();

            var player = new PlayerEntity(2, 2);

            var pile = new ItemPile();
            pile.Add(ItemDefinition.WoodenArrow, amount: 1, dropTurn: 0);
            Assert.That(map.TryAddItemPile(4, 2, pile), Is.True, "Place the pile two tiles away.");

            var picked = actionSystem.TryPickupItemFromItemPile(
                player,
                targetTile: new GridPosition(4, 2),
                item: ItemDefinition.WoodenArrow,
                equipAfterPickup: false,
                mapManager: map,
                logSystem: log);

            Assert.That(picked, Is.False);
            Assert.That(player.Inventory.GetTotalCount(ItemDefinition.WoodenArrow), Is.EqualTo(0));
            Assert.That(map.TryGetItemPile(4, 2, out var remaining), Is.True);
            Assert.That(remaining, Is.Not.Null);
            Assert.That(remaining!.Entries.Count, Is.EqualTo(1));
            Assert.That(remaining.Entries[0].Amount, Is.EqualTo(1));
            Assert.That(log.LoggedMessageIds, Has.Some.EqualTo(MessageId.ContextPickupNotFromThere));
        }

        [Test]
        public void TryPickupItemFromItemPile_Fails_WhenInventoryIsFull_AndDoesNotChangePile()
        {
            var map = new MapManager(8, 8);
            var log = new LogSystem();
            var actionSystem = new ActionSystem();

            var player = new PlayerEntity(2, 2);
            var maxStacks = player.Inventory.MaxStacks;
            var perStack = ItemDefinition.DirtClod.MaxStack;
            player.Inventory.TryAdd(ItemDefinition.DirtClod, maxStacks * perStack);

            var pile = new ItemPile();
            pile.Add(ItemDefinition.WoodenArrow, amount: 1, dropTurn: 0);
            Assert.That(map.TryAddItemPile(3, 2, pile), Is.True);

            var picked = actionSystem.TryPickupItemFromItemPile(
                player,
                targetTile: new GridPosition(3, 2),
                item: ItemDefinition.WoodenArrow,
                equipAfterPickup: false,
                mapManager: map,
                logSystem: log);

            Assert.That(picked, Is.False);
            Assert.That(player.Inventory.GetTotalCount(ItemDefinition.WoodenArrow), Is.EqualTo(0));
            Assert.That(map.TryGetItemPile(3, 2, out var remaining), Is.True, "Pile should remain when pickup fails.");
            Assert.That(remaining, Is.Not.Null);
            Assert.That(remaining!.Entries.Count, Is.EqualTo(1));
            Assert.That(remaining.Entries[0].Amount, Is.EqualTo(1));
            Assert.That(log.LoggedMessageIds, Has.Some.EqualTo(MessageId.PickupInventoryFull));
        }

        [Test]
        public void TryPickupItemFromItemPile_ConsumesOldestEntry_First()
        {
            var map = new MapManager(8, 8);
            var log = new LogSystem();
            var actionSystem = new ActionSystem();

            var player = new PlayerEntity(2, 2);

            var pile = new ItemPile();
            pile.Add(ItemDefinition.WoodenArrow, amount: 1, dropTurn: 0);
            pile.Add(ItemDefinition.WoodenArrow, amount: 1, dropTurn: 1);
            Assert.That(pile.Entries.Count, Is.EqualTo(2));
            Assert.That(pile.Entries[0].DropTurn, Is.EqualTo(0));
            Assert.That(pile.Entries[1].DropTurn, Is.EqualTo(1));

            Assert.That(map.TryAddItemPile(3, 2, pile), Is.True);

            var picked = actionSystem.TryPickupItemFromItemPile(
                player,
                targetTile: new GridPosition(3, 2),
                item: ItemDefinition.WoodenArrow,
                equipAfterPickup: false,
                mapManager: map,
                logSystem: log);

            Assert.That(picked, Is.True);
            Assert.That(map.TryGetItemPile(3, 2, out var remaining), Is.True);
            Assert.That(remaining, Is.Not.Null);
            Assert.That(remaining!.Entries.Count, Is.EqualTo(1));
            Assert.That(remaining.Entries[0].DropTurn, Is.EqualTo(1), "Oldest entry (dropTurn=0) should be consumed first.");
        }
    }
}

