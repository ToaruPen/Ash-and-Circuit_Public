using System.Collections.Generic;
using NUnit.Framework;
using AshNCircuit.Core.Map;

namespace AshNCircuit.Tests.Core
{
    /// <summary>
    /// MapManager のタグ定義・歩行可否・弾道まわりの基本挙動を検証するテスト。
    /// docs/04_environment_and_tags.md に定義されたタグと整合していることを確認する。
    /// </summary>
    public class MapManagerTests
    {
        [Test]
        public void GetTags_ReturnsExpectedFlags_ForAllTileTypes()
        {
            AssertTags(TileType.GroundNormal, TileTag.Ground);
            AssertTags(TileType.GroundBurnt, TileTag.Ground);
            AssertTags(TileType.GroundWater, TileTag.Ground | TileTag.Wet);
            AssertTags(TileType.GroundOil, TileTag.Ground | TileTag.Oily | TileTag.Flammable);
            AssertTags(TileType.WallStone, TileTag.Blocking);
            AssertTags(TileType.WallMetal, TileTag.Blocking | TileTag.Metal | TileTag.Conductive);
            AssertTags(TileType.TreeNormal, TileTag.Blocking | TileTag.Wood | TileTag.Flammable);
            AssertTags(TileType.TreeBurning, TileTag.Blocking | TileTag.Wood | TileTag.Burning | TileTag.Hazardous);
            AssertTags(TileType.TreeBurnt, TileTag.Blocking | TileTag.Wood);
            AssertTags(TileType.FireTile, TileTag.Burning | TileTag.Hazardous);
            AssertTags(TileType.OverlayWater, TileTag.Wet);
            AssertTags(TileType.OverlayOil, TileTag.Oily);
        }

        [Test]
        public void IsWalkable_ConsidersSolidAndProp_ForPassability()
        {
            var map = new MapManager(8, 8);
            const int x = 1;
            const int y = 1;

            // Ground は通行可能。
            map.SetTileType(x, y, TileType.GroundNormal);
            Assert.That(map.IsWalkable(x, y), Is.True);

            map.SetTileType(x, y, TileType.GroundBurnt);
            Assert.That(map.IsWalkable(x, y), Is.True);

            map.SetTileType(x, y, TileType.GroundWater);
            Assert.That(map.IsWalkable(x, y), Is.True);

            map.SetTileType(x, y, TileType.GroundOil);
            Assert.That(map.IsWalkable(x, y), Is.True);

            // Solid（壁/木など）は通行不可。
            map.SetTileType(x, y, TileType.WallStone);
            Assert.That(map.IsWalkable(x, y), Is.False);

            map.SetTileType(x, y, TileType.WallMetal);
            Assert.That(map.IsWalkable(x, y), Is.False);

            map.SetTileType(x, y, TileType.TreeNormal);
            Assert.That(map.IsWalkable(x, y), Is.False);

            map.SetTileType(x, y, TileType.TreeBurning);
            Assert.That(map.IsWalkable(x, y), Is.False);

            map.SetTileType(x, y, TileType.TreeBurnt);
            Assert.That(map.IsWalkable(x, y), Is.False);

            // Prop も blocks_movement=true なら通行不可。
            map.SetTileType(x, y, TileType.GroundNormal);
            Assert.That(map.TryAddProp(x, y, new PropInstance(PropDefinition.ChestPropId)), Is.True);
            Assert.That(map.IsWalkable(x, y), Is.False);
        }

        [Test]
        public void GetLineTrajectory_ReturnsEmpty_WhenStartEqualsEnd()
        {
            var map = new MapManager(8, 8);
            var start = new GridPosition(3, 3);

            var trajectory = map.GetLineTrajectory(start, start, maxRange: 10);

            Assert.That(trajectory, Is.Empty);
        }

        [Test]
        public void GetLineTrajectory_EndsAtTargetWithinMaxRange()
        {
            var map = new MapManager(8, 8);
            var start = new GridPosition(1, 1);
            var end = new GridPosition(6, 1);

            var trajectory = map.GetLineTrajectory(start, end, maxRange: 10);

            Assert.That(trajectory, Is.Not.Empty);
            Assert.That(trajectory[trajectory.Count - 1], Is.EqualTo(end));
        }

        [Test]
        public void GetLinearTrajectory_ProducesStraightLine_InGivenDirection()
        {
            var map = new MapManager(8, 8);
            var result = map.GetLinearTrajectory(1, 1, deltaX: 1, deltaY: 0, maxRange: 3);

            var expected = new List<GridPosition>
            {
                new GridPosition(2, 1),
                new GridPosition(3, 1),
                new GridPosition(4, 1)
            };

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void InitializeDemoMap_SetsFireAndTreeAlongCenterLine()
        {
            var map = new MapManager(16, 16);

            var centerX = map.Width / 2;
            var centerY = map.Height / 2;

            var fireX = centerX + 1;
            var treeX = centerX + 3;

            // このレイアウトは RULE P-01 / RULE E-01 のサンドボックスとして利用する想定。
            // CoversRules: [RULE E-01, RULE P-01]
            Assert.That(map.GetTileType(fireX, centerY), Is.EqualTo(TileType.FireTile));
            Assert.That(map.GetTileType(treeX, centerY), Is.EqualTo(TileType.TreeNormal));
        }

        [Test]
        public void SetTileType_FireTile_AddsOverlay_AndPreservesGround()
        {
            var map = new MapManager(8, 8);
            const int x = 3;
            const int y = 3;

            map.SetTileType(x, y, TileType.GroundOil);
            map.SetTileType(x, y, TileType.FireTile);

            Assert.That(map.GetTileType(x, y), Is.EqualTo(TileType.FireTile), "Overlay should be visible via legacy GetTileType.");
            Assert.That(map.GetGroundType(x, y), Is.EqualTo(TileType.GroundOil), "Ground should not be lost when applying fire overlay.");
        }

        [Test]
        public void SetTileType_OverlayWater_AddsOverlay_AndPreservesGround()
        {
            var map = new MapManager(8, 8);
            const int x = 2;
            const int y = 2;

            map.SetTileType(x, y, TileType.GroundBurnt);
            map.SetTileType(x, y, TileType.OverlayWater);

            Assert.That(map.GetTileType(x, y), Is.EqualTo(TileType.OverlayWater), "Overlay should be visible via legacy GetTileType.");
            Assert.That(map.GetGroundType(x, y), Is.EqualTo(TileType.GroundBurnt), "Ground should not be lost when applying water overlay.");
        }

        [Test]
        public void SetTileType_OverlayOil_AddsOverlay_AndPreservesGround()
        {
            var map = new MapManager(8, 8);
            const int x = 4;
            const int y = 4;

            map.SetTileType(x, y, TileType.GroundNormal);
            map.SetTileType(x, y, TileType.OverlayOil);

            Assert.That(map.GetTileType(x, y), Is.EqualTo(TileType.OverlayOil), "Overlay should be visible via legacy GetTileType.");
            Assert.That(map.GetGroundType(x, y), Is.EqualTo(TileType.GroundNormal), "Ground should not be lost when applying oil overlay.");
        }

        private static void AssertTags(TileType tileType, TileTag expected)
        {
            var tags = MapManager.GetTags(tileType);
            Assert.That(tags, Is.EqualTo(expected), $"Tags for {tileType} should match SoT definition.");
        }
    }
}
