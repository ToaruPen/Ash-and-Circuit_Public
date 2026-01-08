using NUnit.Framework;
using AshNCircuit.Core.Entities;
using AshNCircuit.Core.GameLoop;
using AshNCircuit.Core.Map;
using AshNCircuit.Core.Systems;

namespace AshNCircuit.Tests.Core
{
    /// <summary>
    /// 基本戦闘（近接攻撃・敵AI・死亡処理）の Core 挙動を検証するテスト。
    /// docs/03_combat_and_turns.md および TICKET-0015 の AcceptanceCriteria に対応。
    /// </summary>
    public class CombatMeleeAndEnemyAiTests
    {
        [Test]
        public void MeleeAttack_ReducesEnemyHp_AndKillsWhenHpReachesZero()
        {
            var map = new MapManager(8, 8);
            var log = new LogSystem();
            var actionSystem = new ActionSystem();

            var player = new PlayerEntity(3, 3); // Attack=4, Defense=1

            // HP=4, Defense=0 の敵を 1 回の攻撃で倒す。
            // damage = max(1, 4 - 0) = 4
            var enemy = new EnemyEntity(4, 3, maxHp: 4, attack: 2, defense: 0);

            Assert.That(enemy.IsDead, Is.False);

            var succeeded = actionSystem.TryMeleeAttack(player, enemy, log);

            Assert.That(succeeded, Is.True, "Melee attack should succeed against adjacent enemy.");
            Assert.That(enemy.CurrentHp, Is.EqualTo(0), "Enemy HP should be reduced to zero.");
            Assert.That(enemy.IsDead, Is.True, "Enemy should be marked as dead.");
        }

        [Test]
        public void EnemyAi_AttacksPlayer_WhenAdjacentAndReducesHp()
        {
            var map = new MapManager(8, 8);
            var log = new LogSystem();
            var actionSystem = new ActionSystem();
            var effectSystem = new EffectSystem();
            var turnManager = new TurnManager();
            var gameController = new GameController(turnManager, actionSystem, effectSystem, map, log);

            var player = new PlayerEntity(3, 3);
            gameController.SetPlayerEntity(player);

            var enemyAi = new EnemyAISystem(map, actionSystem, log);
            enemyAi.SetPlayer(player);
            actionSystem.SetEnemyLocator((x, y) => enemyAi.FindEnemyAt(x, y));

            // プレイヤーの右隣に近接攻撃用の敵を配置。
            var enemy = new EnemyEntity(4, 3, maxHp: 5, attack: 5, defense: 0);
            enemyAi.AddEnemy(enemy);

            var initialHp = player.CurrentHp;

            enemyAi.HandleEnemyPhase();

            Assert.That(player.CurrentHp, Is.LessThan(initialHp), "Enemy AI should reduce player HP when adjacent.");
        }

        [Test]
        public void PlayerCannotActAfterDeath()
        {
            var map = new MapManager(8, 8);
            var log = new LogSystem();
            var actionSystem = new ActionSystem();

            var player = new PlayerEntity(3, 3);

            // 非公開 setter を持つため、敵との戦闘を通じて死亡状態まで削る。
            var strongEnemy = new EnemyEntity(4, 3, maxHp: 5, attack: 10, defense: 0);

            // 2 回攻撃すれば確実にプレイヤー HP は 0 以下になる（Attack=10, Defense=1）。
            actionSystem.TryMeleeAttack(strongEnemy, player, log);
            actionSystem.TryMeleeAttack(strongEnemy, player, log);

            Assert.That(player.IsDead, Is.True, "Player should be dead after taking lethal damage.");

            var originalX = player.X;
            var originalY = player.Y;

            // 死亡後の移動は無効化され、位置は変化しない。
            var moved = actionSystem.TryMovePlayer(player, 1, 0, map, log);

            Assert.That(moved, Is.False, "Move should fail when player is dead.");
            Assert.That(player.X, Is.EqualTo(originalX));
            Assert.That(player.Y, Is.EqualTo(originalY));
        }
    }
}
