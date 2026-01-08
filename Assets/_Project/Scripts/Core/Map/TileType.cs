namespace AshNCircuit.Core.Map
{
    /// <summary>
    /// タイル種の列挙。
    /// docs/04_environment_and_tags.md に定義された代表的なタイルに対応する。
    /// </summary>
    public enum TileType
    {
        GroundNormal,
        GroundBurnt,
        GroundWater,
        GroundOil,
        WallStone,
        WallMetal,
        TreeNormal,
        TreeBurning,
        TreeBurnt,
        FireTile,
        OverlayWater,
        OverlayOil
    }
}
