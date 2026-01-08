using System;

namespace AshNCircuit.Core.Map
{
    /// <summary>
    /// タイルが持ちうるタグ一覧。
    /// docs/registry/tags.yml に定義された TAG ID / TileTag 対応と、
    /// docs/04_environment_and_tags.md で説明されるタグの意味に対応する。
    /// </summary>
    [Flags]
    public enum TileTag
    {
        None = 0,
        Blocking = 1 << 0,
        Flammable = 1 << 1,
        Burning = 1 << 2,
        Wet = 1 << 3,
        Oily = 1 << 4,
        Conductive = 1 << 5,
        Hazardous = 1 << 6,
        Wood = 1 << 7,
        Metal = 1 << 8,
        Ground = 1 << 9
    }
}
