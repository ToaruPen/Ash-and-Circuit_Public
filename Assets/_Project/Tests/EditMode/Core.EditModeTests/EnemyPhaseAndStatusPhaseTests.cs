using NUnit.Framework;
using System.Collections.Generic;
using AshNCircuit.Core.Entities;
using AshNCircuit.Core.GameLoop;
using AshNCircuit.Core.Map;
using AshNCircuit.Core.Systems;

namespace AshNCircuit.Tests.Core
{
    /// <summary>
    /// 敵AIフェーズと状態異常フェーズの統合挙動を検証するテスト。
    /// TICKET-0060 に対応し、敵AIが EnemyPhase でのみ動き、
    /// burning ダメージが StatusEffectPhase でのみ適用されることを確認する。
    /// </summary>
    public class EnemyPhaseAndStatusPhaseTests
    {
        [Test]
        public void EnemyMovesTowardPlayer_OncePerTurn_InEnemyPhase()
        {
            var map = new MapManager(8, 8);
            var log = new LogSystem();
            var actionSystem = new ActionSystem();
            var effectSystem = new EffectSystem();
            var turnManager = new TurnManager();
            var gameController = new GameController(turnManager, actionSystem, effectSystem, map, log);

            var player = new PlayerEntity(4, 4);
            gameController.SetPlayerEntity(player);

            var enemyAi = new EnemyAISystem(map, actionSystem, log);
            enemyAi.SetPlayer(player);
            actionSystem.SetEnemyLocator((x, y) => enemyAi.FindEnemyAt(x, y));

            // プレイヤーの左側に敵を 1 体配置する。
            var enemy = new EnemyEntity(1, 4, maxHp: 5, attack: 2, defense: 0);
            enemyAi.AddEnemy(enemy);

            // 敵AIを EnemyPhase に接続する。
            turnManager.OnEnemyPhase += enemyAi.HandleEnemyPhase;

            // 初期位置を記録。
            var initialX = enemy.X;

            // 2 ターン進行させる（プレイヤーは待機）。
            gameController.QueueWait();
            gameController.AdvanceTurn();

            gameController.QueueWait();
            gameController.AdvanceTurn();

            // 敵は毎ターン 1 マスずつプレイヤーに近づくため、2マス前進しているはず。
            Assert.That(enemy.X, Is.EqualTo(initialX + 2), "Enemy should move exactly one tile toward the player per turn via EnemyPhase.");
        }

        [Test]
        public void BurningDamage_IsAppliedOnlyWhenTickStatusEffectsIsCalled()
        {
            var map = new MapManager(8, 8);
            var log = new LogSystem();
            var effectSystem = new EffectSystem();

            var player = new PlayerEntity(3, 3);

            // テスト用に burning を 2 ターン分付与する。
            player.ApplyBurning(2);
            var initialHp = player.CurrentHp;

            // 環境フェーズ相当の TickBurning を呼んでも、エンティティ HP には影響しないことを確認。
            effectSystem.TickBurning(map, log);
            Assert.That(player.CurrentHp, Is.EqualTo(initialHp), "Environment burning tick should not affect entity HP.");

            // 状態異常フェーズ相当の TickStatusEffects を呼ぶと、burning ダメージが 1 回だけ適用される。
            effectSystem.TickStatusEffects(player, new List<EnemyEntity>(), log);

            Assert.That(player.CurrentHp, Is.EqualTo(initialHp - 1), "Burning damage should be applied once per status effect tick.");
            Assert.That(player.BurningRemainingTurns, Is.EqualTo(1), "Burning duration should decrease by one per status effect tick.");

            // 再度 TickStatusEffects を呼ぶと、さらに 1 ダメージと残りターンの減少が発生する。
            effectSystem.TickStatusEffects(player, new List<EnemyEntity>(), log);

            Assert.That(player.CurrentHp, Is.EqualTo(initialHp - 2), "Burning damage should accumulate over multiple status effect ticks.");
            Assert.That(player.BurningRemainingTurns, Is.EqualTo(0), "Burning should expire after its duration elapses.");
        }
    }
}

