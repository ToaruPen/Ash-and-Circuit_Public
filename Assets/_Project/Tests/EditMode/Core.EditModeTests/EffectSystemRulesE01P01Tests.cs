using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using AshNCircuit.Core.Entities;
using AshNCircuit.Core.Map;
using AshNCircuit.Core.Systems;

namespace AshNCircuit.Tests.Core
{
    /// <summary>
    /// RULE P-01（矢 × 火）と RULE E-01（火 × 木）に関する Core 挙動を検証するテスト。
    /// MonoBehaviour に依存せず、EffectSystem / MapManager / ProjectileEntity のみを対象とする。
    /// </summary>
    public class EffectSystemRulesE01P01Tests
    {
        [Test]
        public void ProjectilePassingThroughFireTile_GainsBurningTag()
        {
            // CoversRules: [RULE P-01]

            var map = new MapManager(8, 3);
            var log = new LogSystem();
            var effect = new EffectSystem();

            // (3,1) に fire_tile を配置する。
            map.SetTileType(3, 1, TileType.FireTile);

            var projectile = new ProjectileEntity(0, 1, TileTag.None);
            var trajectory = new List<GridPosition>
            {
                new GridPosition(1, 1),
                new GridPosition(2, 1),
                new GridPosition(3, 1),
                new GridPosition(4, 1)
            };

            var result = new ProjectileResult(projectile, trajectory, impactIndex: null);

            effect.ApplyProjectileEnvironmentRules(result, map, log);

            Assert.That(projectile.HasTag(TileTag.Burning), Is.True, "Projectile should gain burning tag after passing through fire_tile.");
            Assert.That(log.LoggedMessageIds, Has.Some.EqualTo(MessageId.RuleP01ArrowIgnited));
        }

        [Test]
        public void ProjectileAdjacentToFireTile_DoesNotGainBurningTag()
        {
            // CoversRules: [RULE P-01]

            var map = new MapManager(8, 3);
            var log = new LogSystem();
            var effect = new EffectSystem();

            // (3,2) に fire_tile を配置するが、弾道は y=1 を通過するだけ。
            map.SetTileType(3, 2, TileType.FireTile);

            var projectile = new ProjectileEntity(0, 1, TileTag.None);
            var trajectory = new List<GridPosition>
            {
                new GridPosition(1, 1),
                new GridPosition(2, 1),
                new GridPosition(3, 1),
                new GridPosition(4, 1)
            };

            var result = new ProjectileResult(projectile, trajectory, impactIndex: null);

            effect.ApplyProjectileEnvironmentRules(result, map, log);

            Assert.That(projectile.HasTag(TileTag.Burning), Is.False, "Projectile should not gain burning tag when only adjacent to fire_tile.");
            Assert.That(log.LoggedMessageIds, Has.None.EqualTo(MessageId.RuleP01ArrowIgnited));
        }

        [Test]
        public void BurningProjectileHittingTree_IgnitesAndBurnsOutOverTurns()
        {
            // CoversRules: [RULE E-01]

            var map = new MapManager(8, 3);
            var log = new LogSystem();
            var effect = new EffectSystem();

            // (4,1) に tree_normal を配置し、他はデフォルト（ground_normal）のままとする。
            var treePosition = new GridPosition(4, 1);
            map.SetTileType(treePosition.X, treePosition.Y, TileType.TreeNormal);

            // 既に burning タグを持つ投射物が木に命中するケースを直接構成する。
            var projectile = new ProjectileEntity(0, 1, TileTag.Burning);
            var trajectory = new List<GridPosition>
            {
                new GridPosition(1, 1),
                new GridPosition(2, 1),
                new GridPosition(3, 1),
                treePosition
            };

            var impactIndex = trajectory.Count - 1;
            var result = new ProjectileResult(projectile, trajectory, impactIndex);

            effect.ApplyProjectileEnvironmentRules(result, map, log);

            // Projectile フェーズの直後: 木はまだ燃え始めていない（E-01 のトリガーのみが登録される）。
            Assert.That(map.GetTileType(treePosition.X, treePosition.Y), Is.EqualTo(TileType.TreeNormal), "Tree should not ignite until environment phase.");
            Assert.That(log.LoggedMessageIds, Has.None.EqualTo(MessageId.RuleE01TreeIgnited));

            // 環境フェーズを 1 回進めると木が燃え始める。
            effect.TickBurning(map, log);
            Assert.That(map.GetTileType(treePosition.X, treePosition.Y), Is.EqualTo(TileType.TreeBurning), "Tree should start burning after first environment tick.");
            Assert.That(log.LoggedMessageIds, Has.Some.EqualTo(MessageId.RuleE01TreeIgnited));

            // 以降の環境フェーズで燃焼ターンを進める。デフォルトでは 3 ターンで燃え尽きる。
            Assert.That(map.GetTileType(treePosition.X, treePosition.Y), Is.EqualTo(TileType.TreeBurning), "Tree should still be burning after 1 tick.");

            effect.TickBurning(map, log);
            Assert.That(map.GetTileType(treePosition.X, treePosition.Y), Is.EqualTo(TileType.TreeBurning), "Tree should still be burning after 2 ticks.");

            effect.TickBurning(map, log);
            Assert.That(map.GetTileType(treePosition.X, treePosition.Y), Is.EqualTo(TileType.TreeBurnt), "Tree should be burnt out after 3 ticks.");
            Assert.That(log.LoggedMessageIds, Has.Some.EqualTo(MessageId.RuleE01TreeBurnedOut));
        }
    }
}
