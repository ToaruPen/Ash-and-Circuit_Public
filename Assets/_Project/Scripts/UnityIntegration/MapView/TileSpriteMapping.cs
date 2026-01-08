using System;
using AshNCircuit.Core.Map;
using UnityEngine;

namespace AshNCircuit.UnityIntegration.MapView
{
    [Serializable]
    public class TileSpriteMapping
    {
        public TileType TileType;
        public Sprite Sprite = null!;

        /// <summary>
        /// このタイルを描画する Sorting Layer 名。
        /// 空文字列の場合は既定のレイヤー（MapViewPresenter側の判定）を使用する。
        /// </summary>
        public string SortingLayerName = "";

        /// <summary>
        /// このタイルを描画する際の Order in Layer。
        /// 同じ Sorting Layer 内での前後関係を制御する。
        /// </summary>
        public int OrderInLayer = 0;
    }

    [Serializable]
    public class PropSpriteMapping
    {
        public string PropId = "";
        public Sprite Sprite = null!;

        /// <summary>
        /// このプロップを描画する Sorting Layer 名。
        /// 空文字列の場合は既定のレイヤー（Props）を使用する。
        /// </summary>
        public string SortingLayerName = "";

        /// <summary>
        /// このプロップを描画する際の Order in Layer。
        /// 同じ Sorting Layer 内での前後関係を制御する。
        /// </summary>
        public int OrderInLayer = 0;
    }
}

