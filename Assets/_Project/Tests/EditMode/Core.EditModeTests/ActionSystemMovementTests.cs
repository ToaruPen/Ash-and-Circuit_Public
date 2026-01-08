using NUnit.Framework;
using AshNCircuit.Core.Entities;
using AshNCircuit.Core.Map;
using AshNCircuit.Core.Systems;

namespace AshNCircuit.Tests.Core
{
    /// <summary>
    /// ActionSystem.TryMovePlayer の基本挙動を検証するテスト。
    /// MapManager のタグ定義（blocking / ground_*）に基づき、
    /// プレイヤーがどのタイルに移動できるかを確認する。
    /// </summary>
    public class ActionSystemMovementTests
    {
        [Test]
        public void TryMovePlayer_Fails_WhenMovingOutOfBounds()
        {
            var map = new MapManager(8, 8);
            var log = new LogSystem();
            var actionSystem = new ActionSystem();

            // マップ左端の境界にプレイヤーを配置し、さらに外側へ移動しようとする。
            var player = new PlayerEntity(0, 1);

            var moved = actionSystem.TryMovePlayer(player, -1, 0, map, log);

            Assert.That(moved, Is.False, "Out-of-bounds move should fail.");
            Assert.That(player.X, Is.EqualTo(0));
            Assert.That(player.Y, Is.EqualTo(1));
            Assert.That(log.LoggedMessageIds, Has.Some.EqualTo(MessageId.MoveOutOfBounds));
        }

        [Test]
        public void TryMovePlayer_Fails_WhenTargetTileIsBlocking()
        {
            var map = new MapManager(8, 8);
            var log = new LogSystem();
            var actionSystem = new ActionSystem();

            // (2,1) に blocking タイルを配置し、(1,1) から右へ移動しようとする。
            map.SetTileType(2, 1, TileType.WallStone);
            var player = new PlayerEntity(1, 1);

            var moved = actionSystem.TryMovePlayer(player, 1, 0, map, log);

            Assert.That(moved, Is.False, "Move into blocking tile should fail.");
            Assert.That(player.X, Is.EqualTo(1));
            Assert.That(player.Y, Is.EqualTo(1));
            Assert.That(log.LoggedMessageIds, Has.Some.EqualTo(MessageId.MoveBlockedByWall));
        }

        [Test]
        public void TryMovePlayer_Succeeds_WhenTargetTileIsWalkableGround()
        {
            var map = new MapManager(8, 8);
            var log = new LogSystem();
            var actionSystem = new ActionSystem();

            // デフォルトでは ground_normal で埋められているため、そのまま右へ移動する。
            var player = new PlayerEntity(1, 1);

            var moved = actionSystem.TryMovePlayer(player, 1, 0, map, log);

            Assert.That(moved, Is.True, "Move onto walkable ground should succeed.");
            Assert.That(player.X, Is.EqualTo(2));
            Assert.That(player.Y, Is.EqualTo(1));
            Assert.That(log.LoggedMessageIds, Has.Some.EqualTo(MessageId.MoveSucceeded));
        }
    }
}
