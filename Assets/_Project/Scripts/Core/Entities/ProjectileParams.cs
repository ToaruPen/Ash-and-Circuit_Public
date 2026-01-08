using AshNCircuit.Core.Map;

namespace AshNCircuit.Core.Entities
{
    /// <summary>
    /// 投射物の基本パラメータ。
    /// 射程や貫通可否、初期タグなどを定義する。
    /// </summary>
    public class ProjectileParams
    {
        /// <summary>
        /// 最大射程（タイル数）。
        /// </summary>
        public int Range { get; }

        /// <summary>
        /// 貫通可能かどうか。MVP初期段階では常に false を想定する。
        /// </summary>
        public bool CanPierce { get; }

        /// <summary>
        /// 投射物が持つ初期タグ（例: wood, flammable など）。
        /// </summary>
        public TileTag InitialTags { get; }

        public ProjectileParams(int range, bool canPierce, TileTag initialTags)
        {
            Range = range;
            CanPierce = canPierce;
            InitialTags = initialTags;
        }

        /// <summary>
        /// 通常の矢用のシンプルなパラメータを生成するユーティリティ。
        /// </summary>
        public static ProjectileParams CreateBasicArrow(int range)
        {
            return new ProjectileParams(range, false, TileTag.None);
        }
    }
}

