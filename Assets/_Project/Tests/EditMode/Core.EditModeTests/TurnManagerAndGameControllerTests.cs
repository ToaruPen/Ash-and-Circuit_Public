using NUnit.Framework;
using AshNCircuit.Core.Entities;
using AshNCircuit.Core.GameLoop;
using AshNCircuit.Core.Map;
using AshNCircuit.Core.Systems;

namespace AshNCircuit.Tests.Core
{
    /// <summary>
    /// TurnManager と GameController の基本的なターン進行契約を検証するテスト。
    /// </summary>
    public class TurnManagerAndGameControllerTests
    {
        [Test]
        public void AdvanceTurn_IncrementsTurnAndTotalTimeUnits()
        {
            var map = new MapManager(8, 8);
            var log = new LogSystem();
            var actionSystem = new ActionSystem();
            var effectSystem = new EffectSystem();
            var turnManager = new TurnManager();
            var gameController = new GameController(turnManager, actionSystem, effectSystem, map, log);

            var player = new PlayerEntity(1, 1);
            gameController.SetPlayerEntity(player);

            Assert.That(turnManager.CurrentTurn, Is.EqualTo(0));
            Assert.That(turnManager.TotalTimeUnits, Is.EqualTo(0));

            gameController.QueueWait();
            gameController.AdvanceTurn();

            Assert.That(turnManager.CurrentTurn, Is.EqualTo(1));
            Assert.That(turnManager.TotalTimeUnits, Is.EqualTo(TurnManager.TurnTimeUnits));
        }

        [Test]
        public void AdvanceTurn_AppliesQueuedMoveInPlayerPhase()
        {
            var map = new MapManager(8, 8);
            var log = new LogSystem();
            var actionSystem = new ActionSystem();
            var effectSystem = new EffectSystem();
            var turnManager = new TurnManager();
            var gameController = new GameController(turnManager, actionSystem, effectSystem, map, log);

            var player = new PlayerEntity(1, 1);
            gameController.SetPlayerEntity(player);

            gameController.QueueMove(1, 0);

            // ターンを進める前には位置は変化しない。
            Assert.That(player.X, Is.EqualTo(1));
            Assert.That(player.Y, Is.EqualTo(1));

            gameController.AdvanceTurn();

            Assert.That(player.X, Is.EqualTo(2));
            Assert.That(player.Y, Is.EqualTo(1));
            Assert.That(turnManager.CurrentTurn, Is.EqualTo(1));
        }

        [Test]
        public void AdvanceTurn_WithoutQueuedAction_DoesNotMovePlayer()
        {
            var map = new MapManager(8, 8);
            var log = new LogSystem();
            var actionSystem = new ActionSystem();
            var effectSystem = new EffectSystem();
            var turnManager = new TurnManager();
            var gameController = new GameController(turnManager, actionSystem, effectSystem, map, log);

            var player = new PlayerEntity(3, 4);
            gameController.SetPlayerEntity(player);

            gameController.AdvanceTurn();

            Assert.That(player.X, Is.EqualTo(3));
            Assert.That(player.Y, Is.EqualTo(4));
            Assert.That(turnManager.CurrentTurn, Is.EqualTo(1));
        }
    }
}

