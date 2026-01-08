using System.Collections.Generic;
using AshNCircuit.Core.Items;

namespace AshNCircuit.Core.Map
{
    /// <summary>
    /// シンプルな矩形グリッドマップを管理するクラス。
    /// MVP初期段階では、固定レイアウトのデモマップを内部に保持する。
    /// </summary>
    public class MapManager
    {
        private const int GroundItemPileTtlTurns = 3600;

        private readonly int _width;
        private readonly int _height;
        private readonly TileType[,] _groundTiles;
        private readonly TileType?[,] _solidTiles;

        private readonly Dictionary<GridPosition, HashSet<TileType>> _overlays =
            new Dictionary<GridPosition, HashSet<TileType>>();

        private readonly Dictionary<GridPosition, PropInstance> _props =
            new Dictionary<GridPosition, PropInstance>();

        private readonly Dictionary<GridPosition, ItemPile> _itemPiles =
            new Dictionary<GridPosition, ItemPile>();

        public MapManager(int width, int height)
        {
            _width = width;
            _height = height;
            _groundTiles = new TileType[_width, _height];
            _solidTiles = new TileType?[_width, _height];

            InitializeDemoMap();
        }

        public int Width => _width;
        public int Height => _height;

        // ------------------------------------------------------------
        // Basic tile queries / mutations
        // ------------------------------------------------------------

        /// <summary>
        /// 指定したグリッド座標がマップ範囲内かどうか判定する。
        /// </summary>
        public bool IsInBounds(int x, int y)
        {
            return x >= 0 && y >= 0 && x < _width && y < _height;
        }

        /// <summary>
        /// 指定したグリッド座標のタイル種を取得する（legacy）。
        /// 既存の 1セル=1枚スプライト表示など、層モデル導入前の呼び出し向け。
        ///
        /// 優先順位（上ほど優先）:
        /// - Overlay
        /// - Solid
        /// - Ground
        ///
        /// 範囲外を指定した場合は GroundNormal を返す（既存方針）。
        /// </summary>
        public TileType GetTileType(int x, int y)
        {
            if (!IsInBounds(x, y))
            {
                return TileType.GroundNormal;
            }

            if (_overlays.TryGetValue(new GridPosition(x, y), out var overlays) && overlays.Count > 0)
            {
                return ChooseTopOverlay(overlays);
            }

            var solid = _solidTiles[x, y];
            if (solid.HasValue)
            {
                return solid.Value;
            }

            return _groundTiles[x, y];
        }

        /// <summary>
        /// 指定したグリッド座標のタイル種を設定する（legacy）。
        /// 層モデル導入前の呼び出し互換のため、入力の種類に応じて内部状態を更新する。
        /// 範囲外を指定した場合は何もしない（既存方針）。
        /// </summary>
        public void SetTileType(int x, int y, TileType tileType)
        {
            if (!IsInBounds(x, y))
            {
                return;
            }

            if (IsGroundTileType(tileType))
            {
                _groundTiles[x, y] = tileType;
                _solidTiles[x, y] = null;
                ClearOverlaysAt(x, y);
                return;
            }

            if (IsSolidTileType(tileType))
            {
                _solidTiles[x, y] = tileType;
                ClearOverlaysAt(x, y);
                return;
            }

            if (IsOverlayTileType(tileType))
            {
                _solidTiles[x, y] = null;
                ClearOverlaysAt(x, y);
                TryAddOverlay(x, y, tileType);
                return;
            }

            throw new System.ArgumentOutOfRangeException(nameof(tileType), tileType, "Unknown TileType.");
        }

        /// <summary>
        /// 指定したグリッド座標の Ground タイル種を取得する。
        /// 範囲外を指定した場合は GroundNormal を返す（既存方針）。
        /// </summary>
        public TileType GetGroundType(int x, int y)
        {
            if (!IsInBounds(x, y))
            {
                return TileType.GroundNormal;
            }

            return _groundTiles[x, y];
        }

        /// <summary>
        /// 指定したグリッド座標の Solid タイル種を取得する（存在しなければ null）。
        /// 範囲外を指定した場合は null を返す。
        /// </summary>
        public TileType? GetSolidType(int x, int y)
        {
            if (!IsInBounds(x, y))
            {
                return null;
            }

            return _solidTiles[x, y];
        }

        /// <summary>
        /// 指定したグリッド座標の Ground タイル種を設定する（Solid/Overlay は変更しない）。
        /// </summary>
        public void SetGroundType(int x, int y, TileType groundType)
        {
            if (!IsInBounds(x, y))
            {
                return;
            }

            if (!IsGroundTileType(groundType))
            {
                throw new System.ArgumentException("Expected a ground TileType.", nameof(groundType));
            }

            _groundTiles[x, y] = groundType;
        }

        /// <summary>
        /// 指定したグリッド座標の Solid タイル種を設定する（Ground/Overlay は変更しない）。
        /// </summary>
        public void SetSolidType(int x, int y, TileType solidType)
        {
            if (!IsInBounds(x, y))
            {
                return;
            }

            if (!IsSolidTileType(solidType))
            {
                throw new System.ArgumentException("Expected a solid TileType.", nameof(solidType));
            }

            _solidTiles[x, y] = solidType;
        }

        /// <summary>
        /// 指定したグリッド座標の Solid を除去する。
        /// </summary>
        public void ClearSolid(int x, int y)
        {
            if (!IsInBounds(x, y))
            {
                return;
            }

            _solidTiles[x, y] = null;
        }

        /// <summary>
        /// 指定したグリッド座標に Overlay を追加する（重複は追加しない）。
        /// 範囲外の場合は false を返す。
        /// </summary>
        public bool TryAddOverlay(int x, int y, TileType overlayType)
        {
            if (!IsInBounds(x, y))
            {
                return false;
            }

            if (!IsOverlayTileType(overlayType))
            {
                throw new System.ArgumentException("Expected an overlay TileType.", nameof(overlayType));
            }

            var key = new GridPosition(x, y);
            if (!_overlays.TryGetValue(key, out var overlays))
            {
                overlays = new HashSet<TileType>();
                _overlays.Add(key, overlays);
            }

            return overlays.Add(overlayType);
        }

        /// <summary>
        /// 指定したグリッド座標から Overlay を除去する。
        /// </summary>
        public bool TryRemoveOverlay(int x, int y, TileType overlayType)
        {
            if (!IsInBounds(x, y))
            {
                return false;
            }

            if (!_overlays.TryGetValue(new GridPosition(x, y), out var overlays))
            {
                return false;
            }

            var removed = overlays.Remove(overlayType);
            if (!removed)
            {
                return false;
            }

            if (overlays.Count == 0)
            {
                _overlays.Remove(new GridPosition(x, y));
            }

            return true;
        }

        /// <summary>
        /// 指定したグリッド座標が投射物をブロックするかどうか判定する。
        /// </summary>
        public bool BlocksProjectiles(int x, int y)
        {
            if (!IsInBounds(x, y))
            {
                return true;
            }

            if (_solidTiles[x, y].HasValue)
            {
                return true;
            }

            return _props.TryGetValue(new GridPosition(x, y), out var prop) && prop.BlocksProjectiles;
        }

        /// <summary>
        /// 指定したグリッド座標が射線/視線（LOS）をブロックするかどうか判定する。
        /// </summary>
        public bool BlocksLOS(int x, int y)
        {
            if (!IsInBounds(x, y))
            {
                return true;
            }

            if (_solidTiles[x, y].HasValue)
            {
                return true;
            }

            return _props.TryGetValue(new GridPosition(x, y), out var prop) && prop.BlocksLOS;
        }

        // ------------------------------------------------------------
        // Tags / walkability / pickup
        // ------------------------------------------------------------

        /// <summary>
        /// 指定したグリッド座標が歩行可能かどうか判定する。
        /// </summary>
        public bool IsWalkable(int x, int y)
        {
            if (!IsInBounds(x, y))
            {
                return false;
            }

            if (_solidTiles[x, y].HasValue)
            {
                return false;
            }

            return !_props.TryGetValue(new GridPosition(x, y), out var prop) || !prop.BlocksMovement;
        }

        /// <summary>
        /// 指定したタイル種が持つタグを返す。
        /// docs/04_environment_and_tags.md の定義に対応。
        /// </summary>
        public static TileTag GetTags(TileType tileType)
        {
            switch (tileType)
            {
                case TileType.GroundNormal:
                    return TileTag.Ground;
                case TileType.GroundBurnt:
                    return TileTag.Ground;
                case TileType.GroundWater:
                    return TileTag.Ground | TileTag.Wet;
                case TileType.GroundOil:
                    return TileTag.Ground | TileTag.Oily | TileTag.Flammable;
                case TileType.WallStone:
                    return TileTag.Blocking;
                case TileType.WallMetal:
                    return TileTag.Blocking | TileTag.Metal | TileTag.Conductive;
                case TileType.TreeNormal:
                    return TileTag.Blocking | TileTag.Wood | TileTag.Flammable;
                case TileType.TreeBurning:
                    return TileTag.Blocking | TileTag.Wood | TileTag.Burning | TileTag.Hazardous;
                case TileType.TreeBurnt:
                    return TileTag.Blocking | TileTag.Wood;
                case TileType.FireTile:
                    return TileTag.Burning | TileTag.Hazardous;
                case TileType.OverlayWater:
                    return TileTag.Wet;
                case TileType.OverlayOil:
                    return TileTag.Oily;
                default:
                    return TileTag.None;
            }
        }

        /// <summary>
        /// タイルが指定のタグを持つかどうか判定する。
        /// </summary>
        public static bool HasTag(TileType tileType, TileTag tag)
        {
            return (GetTags(tileType) & tag) != 0;
        }

        /// <summary>
        /// 現在タイルから拾えるコアアイテムが存在するかを判定し、その定義を返す。
        /// TICKET-0027/TICKET-0028 の範囲では、ground_* 系タイルからの土くれ取得のみを扱う。
        /// </summary>
        public bool TryGetPickupItem(int x, int y, out ItemDefinition? itemDefinition)
        {
            itemDefinition = null;

            if (!IsWalkable(x, y))
            {
                return false;
            }

            var tileType = GetGroundType(x, y);

            switch (tileType)
            {
                case TileType.GroundNormal:
                case TileType.GroundBurnt:
                case TileType.GroundOil:
                    itemDefinition = ItemDefinition.DirtClod;
                    return true;
                default:
                    return false;
            }
        }

        // ------------------------------------------------------------
        // Trajectory / Line-of-sight helpers
        // ------------------------------------------------------------

        /// <summary>
        /// 4方向のシンプルな直線弾道候補タイル列を取得する。
        /// startX, startY の次のマスから開始し、最大 maxRange ステップぶんを返す。
        /// マップ外に出た時点で打ち切る。
        /// </summary>
        public List<GridPosition> GetLinearTrajectory(int startX, int startY, int deltaX, int deltaY, int maxRange)
        {
            return LineOfSightCalculator.GetLinearTrajectory(_width, _height, startX, startY, deltaX, deltaY, maxRange);
        }

        /// <summary>
        /// 任意の開始タイルと終了タイルの間に、Bresenham ラインに基づく弾道候補タイル列を生成する。
        /// start の次のマスから開始し、最大 maxRange ステップぶんを返す。
        /// マップ外に出た時点、または end に到達した時点で打ち切る。
        /// </summary>
        /// <param name="start">開始グリッド座標（通常は射手の位置）。</param>
        /// <param name="end">ターゲットグリッド座標。</param>
        /// <param name="maxRange">最大射程（タイル数）。</param>
        public List<GridPosition> GetLineTrajectory(GridPosition start, GridPosition end, int maxRange)
        {
            return LineOfSightCalculator.GetLineTrajectory(_width, _height, start, end, maxRange);
        }

        // ------------------------------------------------------------
        // Prop / ItemPile (per-tile state)
        // ------------------------------------------------------------

        public bool TryGetProp(int x, int y, out PropInstance? prop)
        {
            prop = null;

            if (!IsInBounds(x, y))
            {
                return false;
            }

            return _props.TryGetValue(new GridPosition(x, y), out prop);
        }

        public bool TryAddProp(int x, int y, PropInstance prop)
        {
            if (prop == null || !IsInBounds(x, y))
            {
                return false;
            }

            if (_solidTiles[x, y].HasValue)
            {
                return false;
            }

            var position = new GridPosition(x, y);
            if (_props.ContainsKey(position))
            {
                return false;
            }

            _props.Add(position, prop);
            return true;
        }

        public bool TryRemoveProp(int x, int y)
        {
            if (!IsInBounds(x, y))
            {
                return false;
            }

            return _props.Remove(new GridPosition(x, y));
        }

        public bool TryGetItemPile(int x, int y, out ItemPile? itemPile)
        {
            itemPile = null;

            if (!IsInBounds(x, y))
            {
                return false;
            }

            return _itemPiles.TryGetValue(new GridPosition(x, y), out itemPile);
        }

        public bool TryAddItemPile(int x, int y, ItemPile itemPile)
        {
            if (itemPile == null || !IsInBounds(x, y))
            {
                return false;
            }

            var position = new GridPosition(x, y);
            if (_itemPiles.ContainsKey(position))
            {
                return false;
            }

            _itemPiles.Add(position, itemPile);
            return true;
        }

        public bool TryRemoveItemPile(int x, int y)
        {
            if (!IsInBounds(x, y))
            {
                return false;
            }

            return _itemPiles.Remove(new GridPosition(x, y));
        }

        /// <summary>
        /// 地面上の ItemPile を「3日（3600 turns）」で消滅させる。
        /// 本メソッドはターン進行に合わせて 1 ターンに 1 回呼ぶことを想定する。
        /// </summary>
        public void ExpireGroundItemPiles(int currentTurn)
        {
            if (_itemPiles.Count == 0 || currentTurn < 0)
            {
                return;
            }

            var keys = new List<GridPosition>(_itemPiles.Keys);
            foreach (var key in keys)
            {
                var pile = _itemPiles[key];
                pile.RemoveExpiredEntries(currentTurn, GroundItemPileTtlTurns);

                if (pile.IsEmpty)
                {
                    _itemPiles.Remove(key);
                }
            }
        }

        // ------------------------------------------------------------
        // Demo map (sandbox) initialization
        // ------------------------------------------------------------

        private void InitializeDemoMap()
        {
            // ベースを ground_normal で埋める。
            for (var x = 0; x < _width; x++)
            {
                for (var y = 0; y < _height; y++)
                {
                    _groundTiles[x, y] = TileType.GroundNormal;
                    _solidTiles[x, y] = null;
                }
            }

            // 周囲を石壁で囲む（32x32 など、任意の矩形サイズで利用可能）。
            for (var x = 0; x < _width; x++)
            {
                _solidTiles[x, 0] = TileType.WallStone;
                _solidTiles[x, _height - 1] = TileType.WallStone;
            }

            for (var y = 0; y < _height; y++)
            {
                _solidTiles[0, y] = TileType.WallStone;
                _solidTiles[_width - 1, y] = TileType.WallStone;
            }

            // テスト用レイアウト: 矢 × 火 × 木 の検証ラインを中央付近に配置する。
            // 例: プレイヤー (cx-3, cy) → fire_tile (cx+1, cy) → tree_normal (cx+3, cy)
            // RULE P-01 / RULE E-01 の挙動を確認するための簡易サンドボックス。
            if (_width >= 8 && _height >= 8)
            {
                var centerX = _width / 2;
                var centerY = _height / 2;

                var fireX = centerX + 1;
                var treeX = centerX + 3;

                if (IsInBounds(fireX, centerY))
                {
                    TryAddOverlay(fireX, centerY, TileType.FireTile);
                }

                if (IsInBounds(treeX, centerY))
                {
                    _solidTiles[treeX, centerY] = TileType.TreeNormal;
                }
            }
        }

        private void ClearOverlaysAt(int x, int y)
        {
            if (!IsInBounds(x, y))
            {
                return;
            }

            _overlays.Remove(new GridPosition(x, y));
        }

        private static TileType ChooseTopOverlay(HashSet<TileType> overlays)
        {
            if (overlays.Contains(TileType.FireTile))
            {
                return TileType.FireTile;
            }

            var hasAny = false;
            var chosen = TileType.FireTile;
            foreach (var overlay in overlays)
            {
                if (!hasAny || (int)overlay < (int)chosen)
                {
                    chosen = overlay;
                    hasAny = true;
                }
            }

            return chosen;
        }

        private static bool IsGroundTileType(TileType tileType)
        {
            switch (tileType)
            {
                case TileType.GroundNormal:
                case TileType.GroundBurnt:
                case TileType.GroundWater:
                case TileType.GroundOil:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsSolidTileType(TileType tileType)
        {
            switch (tileType)
            {
                case TileType.WallStone:
                case TileType.WallMetal:
                case TileType.TreeNormal:
                case TileType.TreeBurning:
                case TileType.TreeBurnt:
                    return true;
                default:
                    return false;
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
