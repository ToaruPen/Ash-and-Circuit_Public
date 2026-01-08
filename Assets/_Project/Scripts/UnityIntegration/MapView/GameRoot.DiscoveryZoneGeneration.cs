using System;
using AshNCircuit.Core.Items;
using AshNCircuit.Core.Map;
using AshNCircuit.Core.States;
using UnityEngine;

namespace AshNCircuit.UnityIntegration.MapView
{
    public partial class GameRoot
    {
        private readonly struct DiscoveryZonePlacements
        {
            public int Seed { get; }
            public GridPosition PlayerStart { get; }
            public GridPosition WaterCenter { get; }
            public GridPosition TreesCenter { get; }
            public GridPosition RuinsCenter { get; }
            public GridPosition CampFire { get; }
            public GridPosition Chest { get; }

            public DiscoveryZonePlacements(
                int seed,
                GridPosition playerStart,
                GridPosition waterCenter,
                GridPosition treesCenter,
                GridPosition ruinsCenter,
                GridPosition campFire,
                GridPosition chest)
            {
                Seed = seed;
                PlayerStart = playerStart;
                WaterCenter = waterCenter;
                TreesCenter = treesCenter;
                RuinsCenter = ruinsCenter;
                CampFire = campFire;
                Chest = chest;
            }
        }

        private void ApplyDiscoveryZoneGenerationIfNeeded()
        {
            if (!IsDiscoveryScene())
            {
                return;
            }

            var playerStart = GetPlannedPlayerStart(_mapManager);
            var placements = GenerateDiscoveryZone(_mapManager, discoverySeed, playerStart);

            if (logDiscoveryGenerationSummary)
            {
                Debug.Log(
                    $"Discovery zone generated: seed={placements.Seed}, " +
                    $"playerStart=({placements.PlayerStart.X},{placements.PlayerStart.Y}), " +
                    $"water=({placements.WaterCenter.X},{placements.WaterCenter.Y}), " +
                    $"trees=({placements.TreesCenter.X},{placements.TreesCenter.Y}), " +
                    $"ruins=({placements.RuinsCenter.X},{placements.RuinsCenter.Y}), " +
                    $"camp=({placements.CampFire.X},{placements.CampFire.Y}), " +
                    $"chest=({placements.Chest.X},{placements.Chest.Y})");
            }
        }

        private bool IsDiscoveryScene()
        {
            return string.Equals(gameObject.scene.name, "Discovery", StringComparison.Ordinal);
        }

        private GridPosition GetPlannedPlayerStart(MapManager mapManager)
        {
            var startX = mapManager.Width / 2;
            var startY = mapManager.Height / 2;

            if (playerTransform != null && tileSize > 0.0001f)
            {
                startX = Mathf.RoundToInt(playerTransform.position.x / tileSize);
                startY = Mathf.RoundToInt(playerTransform.position.y / tileSize);
            }

            startX = ClampToInterior(startX, mapManager.Width);
            startY = ClampToInterior(startY, mapManager.Height);

            return new GridPosition(startX, startY);
        }

        private static int ClampToInterior(int value, int size)
        {
            if (size <= 2)
            {
                return 0;
            }

            if (value < 1)
            {
                return 1;
            }

            if (value > size - 2)
            {
                return size - 2;
            }

            return value;
        }

        private static int NextIntInclusive(RngStream rng, int minInclusive, int maxInclusive)
        {
            if (maxInclusive <= minInclusive)
            {
                return minInclusive;
            }

            if (maxInclusive == int.MaxValue)
            {
                return rng.NextInt(minInclusive, int.MaxValue);
            }

            return rng.NextInt(minInclusive, maxInclusive + 1);
        }

        private static GridPosition PlaceFilledRect(
            MapManager mapManager,
            RngStream rng,
            TileType tileType,
            int regionMinX,
            int regionMaxX,
            int regionMinY,
            int regionMaxY,
            int minWidth,
            int maxWidth,
            int minHeight,
            int maxHeight)
        {
            if (regionMaxX < regionMinX || regionMaxY < regionMinY)
            {
                return new GridPosition(mapManager.Width / 2, mapManager.Height / 2);
            }

            var regionWidth = regionMaxX - regionMinX + 1;
            var regionHeight = regionMaxY - regionMinY + 1;

            var rectWidth = NextIntInclusive(rng, minWidth, maxWidth);
            var rectHeight = NextIntInclusive(rng, minHeight, maxHeight);
            if (rectWidth > regionWidth) rectWidth = regionWidth;
            if (rectHeight > regionHeight) rectHeight = regionHeight;

            var x0 = NextIntInclusive(rng, regionMinX, regionMaxX - rectWidth + 1);
            var y0 = NextIntInclusive(rng, regionMinY, regionMaxY - rectHeight + 1);

            for (var x = x0; x < x0 + rectWidth; x++)
            {
                for (var y = y0; y < y0 + rectHeight; y++)
                {
                    mapManager.SetTileType(x, y, tileType);
                }
            }

            var centerX = x0 + (rectWidth / 2);
            var centerY = y0 + (rectHeight / 2);
            return new GridPosition(centerX, centerY);
        }

        private readonly struct RuinsLayout
        {
            public GridPosition Center { get; }
            public int OuterMinX { get; }
            public int OuterMaxX { get; }
            public int OuterMinY { get; }
            public int OuterMaxY { get; }

            public int InnerMinX => OuterMinX + 1;
            public int InnerMaxX => OuterMaxX - 1;
            public int InnerMinY => OuterMinY + 1;
            public int InnerMaxY => OuterMaxY - 1;

            public RuinsLayout(GridPosition center, int outerMinX, int outerMaxX, int outerMinY, int outerMaxY)
            {
                Center = center;
                OuterMinX = outerMinX;
                OuterMaxX = outerMaxX;
                OuterMinY = outerMinY;
                OuterMaxY = outerMaxY;
            }
        }

        private static RuinsLayout PlaceRuins(
            MapManager mapManager,
            RngStream rng,
            int regionMinX,
            int regionMaxX,
            int regionMinY,
            int regionMaxY)
        {
            if (regionMaxX < regionMinX || regionMaxY < regionMinY)
            {
                var fallback = new GridPosition(mapManager.Width / 2, mapManager.Height / 2);
                return new RuinsLayout(fallback, fallback.X, fallback.X, fallback.Y, fallback.Y);
            }

            var outerWidth = NextIntInclusive(rng, 8, 12);
            var outerHeight = NextIntInclusive(rng, 7, 11);

            var maxWidth = regionMaxX - regionMinX + 1;
            var maxHeight = regionMaxY - regionMinY + 1;
            if (outerWidth > maxWidth) outerWidth = maxWidth;
            if (outerHeight > maxHeight) outerHeight = maxHeight;

            var x0 = NextIntInclusive(rng, regionMinX, regionMaxX - outerWidth + 1);
            var y0 = NextIntInclusive(rng, regionMinY, regionMaxY - outerHeight + 1);

            var x1 = x0 + outerWidth - 1;
            var y1 = y0 + outerHeight - 1;

            // 外周壁
            for (var x = x0; x <= x1; x++)
            {
                mapManager.SetTileType(x, y0, TileType.WallStone);
                mapManager.SetTileType(x, y1, TileType.WallStone);
            }

            for (var y = y0; y <= y1; y++)
            {
                mapManager.SetTileType(x0, y, TileType.WallStone);
                mapManager.SetTileType(x1, y, TileType.WallStone);
            }

            // 入口（ドア）: 下辺中央を開ける
            var doorX = x0 + (outerWidth / 2);
            mapManager.SetTileType(doorX, y0, TileType.GroundNormal);

            // 金属壁のアクセント（導電床の入口にもなる）
            mapManager.SetTileType(x0, y1, TileType.WallMetal);

            var center = new GridPosition(x0 + (outerWidth / 2), y0 + (outerHeight / 2));
            return new RuinsLayout(center, x0, x1, y0, y1);
        }

        private static void CarveSafeStartArea(MapManager mapManager, GridPosition start, int radius)
        {
            for (var dx = -radius; dx <= radius; dx++)
            {
                for (var dy = -radius; dy <= radius; dy++)
                {
                    var x = start.X + dx;
                    var y = start.Y + dy;
                    if (x <= 0 || y <= 0 || x >= mapManager.Width - 1 || y >= mapManager.Height - 1)
                    {
                        continue;
                    }

                    mapManager.SetTileType(x, y, TileType.GroundNormal);
                }
            }
        }

        private static void ApplyBorderWalls(MapManager mapManager)
        {
            for (var x = 0; x < mapManager.Width; x++)
            {
                mapManager.SetTileType(x, 0, TileType.WallStone);
                mapManager.SetTileType(x, mapManager.Height - 1, TileType.WallStone);
            }

            for (var y = 0; y < mapManager.Height; y++)
            {
                mapManager.SetTileType(0, y, TileType.WallStone);
                mapManager.SetTileType(mapManager.Width - 1, y, TileType.WallStone);
            }
        }

        private static void ApplyDetourWall(MapManager mapManager, GridPosition playerStart)
        {
            if (mapManager.Height < 10 || mapManager.Width < 10)
            {
                return;
            }

            var y = (mapManager.Height / 2) - 2;
            if (y <= 1 || y >= mapManager.Height - 2)
            {
                y = mapManager.Height / 2;
            }

            var gap1 = (mapManager.Width / 2) - 3;
            var gap2 = (mapManager.Width / 2) + 3;

            gap1 = ClampToInterior(gap1, mapManager.Width);
            gap2 = ClampToInterior(gap2, mapManager.Width);

            if (playerStart.Y == y)
            {
                gap1 = playerStart.X;
            }

            for (var x = 1; x <= mapManager.Width - 2; x++)
            {
                if (x == gap1 || x == gap2)
                {
                    continue;
                }

                mapManager.SetTileType(x, y, TileType.WallStone);
            }
        }

        private static DiscoveryZonePlacements GenerateDiscoveryZone(MapManager mapManager, int runSeed, GridPosition playerStart)
        {
            var rng = new WorldRngState(runSeed).GenRng;

            // ベース: ground_normal で埋める。
            for (var x = 0; x < mapManager.Width; x++)
            {
                for (var y = 0; y < mapManager.Height; y++)
                {
                    mapManager.SetTileType(x, y, TileType.GroundNormal);
                }
            }

            ApplyBorderWalls(mapManager);
            ApplyDetourWall(mapManager, playerStart);

            var leftMaxX = (mapManager.Width / 2) - 2;
            var rightMinX = (mapManager.Width / 2) + 2;

            var bottomMaxY = (mapManager.Height / 2) - 4;
            var topMinY = (mapManager.Height / 2);

            leftMaxX = leftMaxX < 1 ? 1 : leftMaxX;
            rightMinX = rightMinX > mapManager.Width - 2 ? mapManager.Width - 2 : rightMinX;
            bottomMaxY = bottomMaxY < 1 ? 1 : bottomMaxY;
            topMinY = topMinY < 1 ? 1 : topMinY;

            var waterCenter = PlaceFilledRect(
                mapManager,
                rng,
                TileType.OverlayWater,
                regionMinX: 1,
                regionMaxX: leftMaxX,
                regionMinY: 1,
                regionMaxY: bottomMaxY,
                minWidth: 5,
                maxWidth: 9,
                minHeight: 4,
                maxHeight: 7);

            var treesCenter = PlaceFilledRect(
                mapManager,
                rng,
                TileType.TreeNormal,
                regionMinX: rightMinX,
                regionMaxX: mapManager.Width - 2,
                regionMinY: topMinY,
                regionMaxY: mapManager.Height - 2,
                minWidth: 5,
                maxWidth: 10,
                minHeight: 5,
                maxHeight: 10);

            var ruins = PlaceRuins(
                mapManager,
                rng,
                regionMinX: 1,
                regionMaxX: leftMaxX,
                regionMinY: topMinY,
                regionMaxY: mapManager.Height - 2);

            var campFire = PlaceFilledRect(
                mapManager,
                rng,
                TileType.OverlayOil,
                regionMinX: rightMinX,
                regionMaxX: mapManager.Width - 2,
                regionMinY: 1,
                regionMaxY: bottomMaxY,
                minWidth: 6,
                maxWidth: 10,
                minHeight: 4,
                maxHeight: 7);

            // キャンプの火: 油パッチ中央に fire_tile を置き、隣接に油を残す。
            mapManager.SetTileType(campFire.X, campFire.Y, TileType.FireTile);
            mapManager.SetTileType(ClampToInterior(campFire.X + 1, mapManager.Width), campFire.Y, TileType.OverlayOil);

            // 生成後もプレイヤー開始地点は必ず安全にする（壁/木/火を潰す）。
            CarveSafeStartArea(mapManager, playerStart, radius: 1);

            // チェスト: キャンプ寄り 70% / 跡地寄り 30%（seedで決定）
            var chestNearCamp = NextIntInclusive(rng, 0, 99) < 70;
            var chest = chestNearCamp
                ? new GridPosition(ClampToInterior(campFire.X + 2, mapManager.Width), ClampToInterior(campFire.Y, mapManager.Height))
                : new GridPosition(
                    NextIntInclusive(rng, ruins.InnerMinX, ruins.InnerMaxX),
                    NextIntInclusive(rng, ruins.InnerMinY, ruins.InnerMaxY));

            chest = new GridPosition(
                ClampToInterior(chest.X, mapManager.Width),
                ClampToInterior(chest.Y, mapManager.Height));

            mapManager.SetTileType(chest.X, chest.Y, TileType.GroundNormal);
            mapManager.TryAddProp(chest.X, chest.Y, new PropInstance(PropDefinition.ChestPropId));

            var pile = new ItemPile();
            pile.Add(ItemDefinition.DirtClod, amount: 1, dropTurn: 0);

            var candidates = new[]
            {
                new GridPosition(chest.X + 1, chest.Y),
                new GridPosition(chest.X - 1, chest.Y),
                new GridPosition(chest.X, chest.Y + 1),
                new GridPosition(chest.X, chest.Y - 1),
                new GridPosition(chest.X + 1, chest.Y + 1),
                new GridPosition(chest.X + 1, chest.Y - 1),
                new GridPosition(chest.X - 1, chest.Y + 1),
                new GridPosition(chest.X - 1, chest.Y - 1),
            };

            foreach (var candidate in candidates)
            {
                var x = ClampToInterior(candidate.X, mapManager.Width);
                var y = ClampToInterior(candidate.Y, mapManager.Height);

                if (mapManager.GetSolidType(x, y).HasValue)
                {
                    continue;
                }

                if (mapManager.TryGetProp(x, y, out _))
                {
                    continue;
                }

                if (mapManager.TryAddItemPile(x, y, pile))
                {
                    break;
                }
            }

            return new DiscoveryZonePlacements(runSeed, playerStart, waterCenter, treesCenter, ruins.Center, campFire, chest);
        }
    }
}
