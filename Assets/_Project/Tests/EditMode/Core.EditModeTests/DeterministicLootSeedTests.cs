using System.Linq;
using AshNCircuit.Core.Entities;
using AshNCircuit.Core.Map;
using AshNCircuit.Core.Systems;
using AshNCircuit.Core.States;
using NUnit.Framework;

namespace AshNCircuit.Tests.Core
{
    public class DeterministicLootSeedTests
    {
        [Test]
        public void TryOpenContainer_IsOrderIndependent_AndDoesNotConsumeLootRng()
        {
            const int runSeed = 12345;

            var first = OpenTwoChestsAndSnapshot(runSeed, openLeftFirst: true);
            var second = OpenTwoChestsAndSnapshot(runSeed, openLeftFirst: false);

            Assert.That(second.Left, Is.EqualTo(first.Left));
            Assert.That(second.Right, Is.EqualTo(first.Right));
        }

        private static (string Left, string Right) OpenTwoChestsAndSnapshot(int runSeed, bool openLeftFirst)
        {
            var worldRng = new WorldRngState(runSeed);
            var lootRngStateBefore = worldRng.LootRng.State;

            var map = new MapManager(8, 8);
            var log = new LogSystem();
            var actionSystem = new ActionSystem();
            var player = new PlayerEntity(2, 2);

            Assert.That(map.TryAddProp(1, 2, new PropInstance(PropDefinition.ChestPropId)), Is.True);
            Assert.That(map.TryAddProp(3, 2, new PropInstance(PropDefinition.ChestPropId)), Is.True);

            var left = new GridPosition(1, 2);
            var right = new GridPosition(3, 2);

            if (openLeftFirst)
            {
                Assert.That(actionSystem.TryOpenContainer(player, left, worldRng, map, log), Is.True);
                Assert.That(actionSystem.TryOpenContainer(player, right, worldRng, map, log), Is.True);
            }
            else
            {
                Assert.That(actionSystem.TryOpenContainer(player, right, worldRng, map, log), Is.True);
                Assert.That(actionSystem.TryOpenContainer(player, left, worldRng, map, log), Is.True);
            }

            Assert.That(worldRng.LootRng.State, Is.EqualTo(lootRngStateBefore), "LootRng should not be consumed by container loot rolling.");

            return (SnapshotContainerAt(map, left), SnapshotContainerAt(map, right));
        }

        private static string SnapshotContainerAt(MapManager map, GridPosition position)
        {
            Assert.That(map.TryGetProp(position.X, position.Y, out var prop), Is.True);
            Assert.That(prop, Is.Not.Null);
            Assert.That(prop!.LootSeed, Is.Not.Null);
            Assert.That(prop.ContainerItems, Is.Not.Null);

            var items = prop.ContainerItems!.Entries
                .Select(e => $"{e.Item.Id}:{e.Amount}")
                .ToArray();

            return string.Join("|", items);
        }

        [Test]
        public void EnemyDrop_IsOrderIndependent_AndDoesNotConsumeLootRng()
        {
            const int runSeed = 999;

            var first = RollTwoEnemyDropsAndSnapshot(runSeed, rollFirstEnemyFirst: true);
            var second = RollTwoEnemyDropsAndSnapshot(runSeed, rollFirstEnemyFirst: false);

            Assert.That(second.Enemy1, Is.EqualTo(first.Enemy1));
            Assert.That(second.Enemy2, Is.EqualTo(first.Enemy2));
        }

        private static (string Enemy1, string Enemy2) RollTwoEnemyDropsAndSnapshot(int runSeed, bool rollFirstEnemyFirst)
        {
            var worldRng = new WorldRngState(runSeed);
            var lootRngStateBefore = worldRng.LootRng.State;

            var enemy1 = new EnemyEntity(x: 2, y: 2, maxHp: 3, attack: 1, defense: 0, id: "enemy_a", displayName: "A");
            var enemy2 = new EnemyEntity(x: 5, y: 6, maxHp: 3, attack: 1, defense: 0, id: "enemy_b", displayName: "B");

            enemy1.ApplyRawDamage(999);
            enemy2.ApplyRawDamage(999);

            if (rollFirstEnemyFirst)
            {
                Assert.That(enemy1.TryRollDropIfNeeded(worldRng, dropTurn: 0), Is.True);
                Assert.That(enemy2.TryRollDropIfNeeded(worldRng, dropTurn: 0), Is.True);
            }
            else
            {
                Assert.That(enemy2.TryRollDropIfNeeded(worldRng, dropTurn: 0), Is.True);
                Assert.That(enemy1.TryRollDropIfNeeded(worldRng, dropTurn: 0), Is.True);
            }

            Assert.That(worldRng.LootRng.State, Is.EqualTo(lootRngStateBefore), "LootRng should not be consumed by enemy drop rolling.");

            return (SnapshotDrop(enemy1), SnapshotDrop(enemy2));
        }

        private static string SnapshotDrop(EnemyEntity enemy)
        {
            Assert.That(enemy.DropSeed, Is.Not.Null);
            Assert.That(enemy.RolledDrop, Is.Not.Null);

            var items = enemy.RolledDrop!.Entries
                .Select(e => $"{e.Item.Id}:{e.Amount}")
                .ToArray();

            return string.Join("|", items);
        }
    }
}
