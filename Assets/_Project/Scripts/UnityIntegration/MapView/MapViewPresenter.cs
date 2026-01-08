using System;
using AshNCircuit.Core.Items;
using AshNCircuit.Core.Map;
using AshNCircuit.UnityIntegration.Content;
using UnityEngine;

namespace AshNCircuit.UnityIntegration.MapView
{
    /// <summary>
    /// MapManager が持つタイル情報をもとに、マップタイルの GameObject 生成と
    /// SpriteRenderer の更新を担当するプレゼンター。
    /// GameRoot からはこのクラス経由でマップ描画を行う。
    /// </summary>
    public sealed class MapViewPresenter
    {
        private const string SortingLayerGrounds = "Grounds";
        private const string SortingLayerProps = "Props";
        private const string SortingLayerWalls = "Walls";
        private const string SortingLayerEffects = "Effects";

        private const int ItemPileOrderInLayer = -10;

        private readonly MapManager _mapManager;
        private readonly Transform _tilesParent;
        private readonly TileSpriteMapping[] _tileSpriteMappings;
        private readonly PropSpriteMapping[] _propSpriteMappings;
        private readonly Sprite? _itemPileSprite;
        private readonly float _tileSize;

        private readonly SpriteRenderer[,] _groundRenderers;
        private readonly SpriteRenderer[,] _solidRenderers;
        private readonly SpriteRenderer[,] _itemPileRenderers;
        private readonly SpriteRenderer[,] _overlayRenderers;

        private bool _isBuilt;

        public MapViewPresenter(
            MapManager mapManager,
            Transform tilesParent,
            TileSpriteMapping[] tileSpriteMappings,
            PropSpriteMapping[] propSpriteMappings,
            Sprite? itemPileSprite,
            float tileSize)
        {
            _mapManager = mapManager ?? throw new ArgumentNullException(nameof(mapManager));
            _tilesParent = tilesParent ?? throw new ArgumentNullException(nameof(tilesParent));
            _tileSpriteMappings = tileSpriteMappings ?? throw new ArgumentNullException(nameof(tileSpriteMappings));
            _propSpriteMappings = propSpriteMappings ?? throw new ArgumentNullException(nameof(propSpriteMappings));
            _itemPileSprite = itemPileSprite;
            _tileSize = tileSize;

            _groundRenderers = new SpriteRenderer[_mapManager.Width, _mapManager.Height];
            _solidRenderers = new SpriteRenderer[_mapManager.Width, _mapManager.Height];
            _itemPileRenderers = new SpriteRenderer[_mapManager.Width, _mapManager.Height];
            _overlayRenderers = new SpriteRenderer[_mapManager.Width, _mapManager.Height];
        }

        /// <summary>
        /// マップ全体のタイル GameObject を生成し、初期スプライトを設定する。
        /// </summary>
        public void BuildInitialTiles()
        {
            if (_isBuilt)
            {
                return;
            }

            if (_tileSpriteMappings.Length == 0)
            {
                throw new InvalidOperationException("MapViewPresenter: tileSpriteMappings が空です（fail-fast）。");
            }

            for (var x = 0; x < _mapManager.Width; x++)
            {
                for (var y = 0; y < _mapManager.Height; y++)
                {
                    _groundRenderers[x, y] = CreateTileRenderer($"Ground_{x}_{y}", x, y);
                    _solidRenderers[x, y] = CreateTileRenderer($"Solid_{x}_{y}", x, y);
                    _itemPileRenderers[x, y] = CreateTileRenderer($"ItemPile_{x}_{y}", x, y);
                    _overlayRenderers[x, y] = CreateTileRenderer($"Overlay_{x}_{y}", x, y);
                }
            }

            _isBuilt = true;
            RefreshAllTiles();
        }

        /// <summary>
        /// MapManager が持つタイル種に合わせて、全タイルのスプライトを再設定する。
        /// 木の燃焼状態など、タイル状態の変化を見た目に反映するために使用する。
        /// </summary>
        public void RefreshAllTiles()
        {
            if (!_isBuilt)
            {
                return;
            }

            for (var x = 0; x < _mapManager.Width; x++)
            {
                for (var y = 0; y < _mapManager.Height; y++)
                {
                    UpdateGround(x, y);
                    UpdateSolidOrProp(x, y);
                    UpdateItemPile(x, y);
                    UpdateOverlay(x, y);
                }
            }
        }

        private SpriteRenderer CreateTileRenderer(string objectName, int x, int y)
        {
            var tileObject = new GameObject(objectName);
            tileObject.transform.SetParent(_tilesParent, false);
            tileObject.transform.position = new Vector3(x * _tileSize, y * _tileSize, 0f);
            return tileObject.AddComponent<SpriteRenderer>();
        }

        private void UpdateGround(int x, int y)
        {
            var groundType = _mapManager.GetGroundType(x, y);
            var mapping = GetTileMappingOrThrow(groundType);

            ApplySprite(
                _groundRenderers[x, y],
                mapping.Sprite,
                sortingLayerName: GetSortingLayerNameForTileTypeOrOverride(groundType, mapping.SortingLayerName),
                sortingOrder: mapping.OrderInLayer);
        }

        private void UpdateSolidOrProp(int x, int y)
        {
            var renderer = _solidRenderers[x, y];

            if (_mapManager.TryGetProp(x, y, out var prop) && prop != null)
            {
                var mapping = GetPropMappingOrThrow(prop.PropId);
                ApplySprite(
                    renderer,
                    mapping.Sprite,
                    sortingLayerName: GetSortingLayerNameOrDefault(mapping.SortingLayerName, SortingLayerProps),
                    sortingOrder: mapping.OrderInLayer);
                return;
            }

            var solidType = _mapManager.GetSolidType(x, y);
            if (!solidType.HasValue)
            {
                ClearSprite(renderer);
                return;
            }

            var solidMapping = GetTileMappingOrThrow(solidType.Value);
            ApplySprite(
                renderer,
                solidMapping.Sprite,
                sortingLayerName: GetSortingLayerNameForTileTypeOrOverride(solidType.Value, solidMapping.SortingLayerName),
                sortingOrder: solidMapping.OrderInLayer);
        }

        private void UpdateItemPile(int x, int y)
        {
            var renderer = _itemPileRenderers[x, y];

            if (!_mapManager.TryGetItemPile(x, y, out var itemPile) || itemPile == null || itemPile.IsEmpty)
            {
                ClearSprite(renderer);
                return;
            }

            var representativeItem = itemPile.RepresentativeItem
                ?? throw new InvalidOperationException($"MapViewPresenter: ItemPile の RepresentativeItem が null です（x={x}, y={y}）。");

            var sprite = SpriteCatalog
                .LoadFromResources()
                .GetSpriteOrThrow(representativeItem.SpriteId);

            ApplySprite(
                renderer,
                sprite,
                sortingLayerName: SortingLayerProps,
                sortingOrder: ItemPileOrderInLayer);
        }

        private void UpdateOverlay(int x, int y)
        {
            var renderer = _overlayRenderers[x, y];

            var tileType = _mapManager.GetTileType(x, y);
            if (!IsOverlayTileType(tileType))
            {
                ClearSprite(renderer);
                return;
            }

            var mapping = GetTileMappingOrThrow(tileType);
            ApplySprite(
                renderer,
                mapping.Sprite,
                sortingLayerName: GetSortingLayerNameForTileTypeOrOverride(tileType, mapping.SortingLayerName),
                sortingOrder: mapping.OrderInLayer);
        }

        private static void ApplySprite(SpriteRenderer renderer, Sprite sprite, string sortingLayerName, int sortingOrder)
        {
            renderer.sprite = sprite;
            renderer.enabled = true;
            renderer.sortingLayerName = sortingLayerName;
            renderer.sortingOrder = sortingOrder;
        }

        private static void ClearSprite(SpriteRenderer renderer)
        {
            renderer.sprite = null;
            renderer.enabled = false;
        }

        private TileSpriteMapping GetTileMappingOrThrow(TileType tileType)
        {
            foreach (var mapping in _tileSpriteMappings)
            {
                if (mapping != null && mapping.TileType == tileType)
                {
                    if (mapping.Sprite == null)
                    {
                        throw new InvalidOperationException($"MapViewPresenter: Sprite が未設定です（TileType={tileType}）。");
                    }

                    return mapping;
                }
            }

            throw new InvalidOperationException($"MapViewPresenter: TileType の SpriteMapping が不足しています（TileType={tileType}）。");
        }

        private PropSpriteMapping GetPropMappingOrThrow(string propId)
        {
            if (string.IsNullOrEmpty(propId))
            {
                throw new InvalidOperationException("MapViewPresenter: PropId が空です。");
            }

            foreach (var mapping in _propSpriteMappings)
            {
                if (mapping == null)
                {
                    continue;
                }

                if (!string.Equals(mapping.PropId, propId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (mapping.Sprite == null)
                {
                    throw new InvalidOperationException($"MapViewPresenter: Prop Sprite が未設定です（PropId={propId}）。");
                }

                return mapping;
            }

            throw new InvalidOperationException($"MapViewPresenter: Prop の SpriteMapping が不足しています（PropId={propId}）。");
        }

        private static string GetSortingLayerNameOrDefault(string sortingLayerName, string defaultValue)
        {
            return string.IsNullOrEmpty(sortingLayerName) ? defaultValue : sortingLayerName;
        }

        private static string GetSortingLayerNameForTileTypeOrOverride(TileType tileType, string sortingLayerNameOverride)
        {
            if (!string.IsNullOrEmpty(sortingLayerNameOverride))
            {
                return sortingLayerNameOverride;
            }

            return GetSortingLayerNameForTileType(tileType);
        }

        private static string GetSortingLayerNameForTileType(TileType tileType)
        {
            switch (tileType)
            {
                case TileType.GroundNormal:
                case TileType.GroundBurnt:
                case TileType.GroundWater:
                case TileType.GroundOil:
                case TileType.OverlayWater:
                case TileType.OverlayOil:
                    return SortingLayerGrounds;

                case TileType.WallStone:
                case TileType.WallMetal:
                case TileType.TreeNormal:
                case TileType.TreeBurning:
                case TileType.TreeBurnt:
                    return SortingLayerWalls;

                case TileType.FireTile:
                    return SortingLayerEffects;

                default:
                    throw new ArgumentOutOfRangeException(nameof(tileType), tileType, "Unknown TileType.");
            }
        }

        private static bool IsOverlayTileType(TileType tileType)
        {
            switch (tileType)
            {
                case TileType.FireTile:
                case TileType.OverlayWater:
                case TileType.OverlayOil:
                    return true;
                default:
                    return false;
            }
        }
    }
}
