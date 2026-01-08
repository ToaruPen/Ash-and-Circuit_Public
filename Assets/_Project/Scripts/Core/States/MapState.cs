using AshNCircuit.Core.Map;

namespace AshNCircuit.Core.States
{
    /// <summary>
    /// 座標に紐づく状態（B案）。
    /// MVP段階では、タイル地形（TileType[,]）の参照点を用意し、
    /// 追加データ（Prop / ItemPile / FoW等）は後続チケットで段階的に拡張する。
    /// </summary>
    public sealed class MapState
    {
        public int Width { get; }

        public int Height { get; }

        public TileType[,] Tiles { get; }

        public MapState(int width, int height)
            : this(width, height, TileType.GroundNormal)
        {
        }

        public MapState(int width, int height, TileType defaultTile)
        {
            Width = width > 0 ? width : 1;
            Height = height > 0 ? height : 1;

            Tiles = new TileType[Width, Height];

            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    Tiles[x, y] = defaultTile;
                }
            }
        }
    }
}

