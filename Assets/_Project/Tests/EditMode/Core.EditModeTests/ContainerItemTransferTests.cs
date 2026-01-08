using System.Linq;
using AshNCircuit.Core.Items;
using AshNCircuit.Core.Map;
using AshNCircuit.Core.States;
using NUnit.Framework;

namespace AshNCircuit.Tests.Core
{
    public sealed class ContainerItemTransferTests
    {
        [Test]
        public void TryTakeOneFromContainerToInventory_Succeeds_AndUpdatesBothSides()
        {
            var prop = new PropInstance(PropDefinition.ChestPropId);
            var worldRng = new WorldRngState(runSeed: 12345);

            Assert.That(prop.TryEnsureContainerLootRolled(worldRng, x: 1, y: 2), Is.True);
            Assert.That(prop.ContainerItems, Is.Not.Null);

            var inventory = new Inventory(maxStacks: 20);
            var item = ItemDefinition.WoodenArrow;

            var beforeContainer = CountItems(prop.ContainerItems!, item);
            var beforeInventory = inventory.GetTotalCount(item);

            Assert.That(prop.TryTakeOneFromContainerToInventory(item, inventory), Is.True);

            var afterContainer = CountItems(prop.ContainerItems!, item);
            var afterInventory = inventory.GetTotalCount(item);

            Assert.That(afterContainer, Is.EqualTo(beforeContainer - 1));
            Assert.That(afterInventory, Is.EqualTo(beforeInventory + 1));
        }

        [Test]
        public void TryTakeOneFromContainerToInventory_Fails_WhenInventoryIsFull_AndDoesNotChangeContainer()
        {
            var prop = new PropInstance(PropDefinition.ChestPropId);
            var worldRng = new WorldRngState(runSeed: 12345);

            Assert.That(prop.TryEnsureContainerLootRolled(worldRng, x: 1, y: 2), Is.True);
            Assert.That(prop.ContainerItems, Is.Not.Null);

            var inventory = new Inventory(maxStacks: 1);
            Assert.That(inventory.TryAdd(ItemDefinition.Bow, amount: 1), Is.True);

            var item = ItemDefinition.WoodenArrow;
            var beforeContainer = CountItems(prop.ContainerItems!, item);
            var beforeInventory = inventory.GetTotalCount(item);

            Assert.That(prop.TryTakeOneFromContainerToInventory(item, inventory), Is.False);

            var afterContainer = CountItems(prop.ContainerItems!, item);
            var afterInventory = inventory.GetTotalCount(item);

            Assert.That(afterContainer, Is.EqualTo(beforeContainer));
            Assert.That(afterInventory, Is.EqualTo(beforeInventory));
        }

        [Test]
        public void TryStoreOneFromInventoryToContainer_CreatesContainerStorage_AndMovesItem()
        {
            var prop = new PropInstance(PropDefinition.ChestPropId);
            Assert.That(prop.ContainerItems, Is.Null);

            var inventory = new Inventory(maxStacks: 20);
            var item = ItemDefinition.WoodenArrow;
            Assert.That(inventory.TryAdd(item, amount: 2), Is.True);

            var beforeInventory = inventory.GetTotalCount(item);

            Assert.That(prop.TryStoreOneFromInventoryToContainer(item, inventory), Is.True);
            Assert.That(prop.ContainerItems, Is.Not.Null);

            var afterInventory = inventory.GetTotalCount(item);
            var containerCount = CountItems(prop.ContainerItems!, item);

            Assert.That(afterInventory, Is.EqualTo(beforeInventory - 1));
            Assert.That(containerCount, Is.EqualTo(1));
        }

        private static int CountItems(ItemPile pile, ItemDefinition item)
        {
            return pile.Entries.Where(e => e.Item == item).Sum(e => e.Amount);
        }
    }
}
