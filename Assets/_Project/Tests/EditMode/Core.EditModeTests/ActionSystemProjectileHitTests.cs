using NUnit.Framework;
using AshNCircuit.Core.Entities;
using AshNCircuit.Core.Items;
using AshNCircuit.Core.Map;
using AshNCircuit.Core.Systems;

namespace AshNCircuit.Tests.Core
{
    public class ActionSystemProjectileHitTests
    {
        [Test]
        public void ShootProjectile_HitsEnemyOnLine_StopsAtEnemy_AndLogsHit()
        {
            var map = new MapManager(16, 16);
            var log = new LogSystem();
            var actionSystem = new ActionSystem();

            var player = new PlayerEntity(1, 1);
            var enemy = new EnemyEntity(4, 1, maxHp: 5, attack: 1, defense: 0, displayName: "敵");

            actionSystem.SetEnemyLocator((x, y) =>
            {
                return enemy.X == x && enemy.Y == y ? enemy : null;
            });

            var result = actionSystem.TryShootProjectile(player, deltaX: 1, deltaY: 0, map, log);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.ImpactKind, Is.EqualTo(ProjectileImpactKind.Enemy));
            Assert.That(result.ImpactEnemy, Is.SameAs(enemy));
            Assert.That(result.Projectile.X, Is.EqualTo(enemy.X));
            Assert.That(result.Projectile.Y, Is.EqualTo(enemy.Y));
            Assert.That(log.LoggedMessageIds, Has.Some.EqualTo(MessageId.ProjectileHitEnemy));
        }

        [Test]
        public void ShootProjectile_WallBeforeEnemy_StopsAtWall_DoesNotHitEnemy()
        {
            var map = new MapManager(16, 16);
            var log = new LogSystem();
            var actionSystem = new ActionSystem();

            var player = new PlayerEntity(1, 1);
            var enemy = new EnemyEntity(5, 1, maxHp: 5, attack: 1, defense: 0, displayName: "敵");

            // 敵の手前に壁を置き、遮蔽物が先なら壁で止まることを確認する。
            map.SetTileType(3, 1, TileType.WallStone);

            actionSystem.SetEnemyLocator((x, y) =>
            {
                return enemy.X == x && enemy.Y == y ? enemy : null;
            });

            var result = actionSystem.TryShootProjectile(player, deltaX: 1, deltaY: 0, map, log);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.ImpactKind, Is.EqualTo(ProjectileImpactKind.BlockingTile));
            Assert.That(result.ImpactEnemy, Is.Null);
            Assert.That(result.Projectile.X, Is.EqualTo(3));
            Assert.That(result.Projectile.Y, Is.EqualTo(1));
        }

        [Test]
        public void ThrowWoodenArrow_HitsEnemyOnLine_StopsAtEnemy_AndLogsHit()
        {
            var map = new MapManager(16, 16);
            var log = new LogSystem();
            var actionSystem = new ActionSystem();

            var player = new PlayerEntity(1, 1);
            player.Inventory.TryAdd(ItemDefinition.WoodenArrow, 1);

            var enemy = new EnemyEntity(3, 1, maxHp: 5, attack: 1, defense: 0, displayName: "敵");
            actionSystem.SetEnemyLocator((x, y) =>
            {
                return enemy.X == x && enemy.Y == y ? enemy : null;
            });

            var result = actionSystem.TryThrowItem(player, ItemDefinition.WoodenArrow, new GridPosition(6, 1), map, log);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.ImpactKind, Is.EqualTo(ProjectileImpactKind.Enemy));
            Assert.That(result.ImpactEnemy, Is.SameAs(enemy));
            Assert.That(result.Projectile.X, Is.EqualTo(enemy.X));
            Assert.That(result.Projectile.Y, Is.EqualTo(enemy.Y));
            Assert.That(log.LoggedMessageIds, Has.Some.EqualTo(MessageId.ProjectileHitEnemy));
        }
    }
}
