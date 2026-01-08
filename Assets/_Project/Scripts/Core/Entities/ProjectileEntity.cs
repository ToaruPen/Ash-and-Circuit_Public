using AshNCircuit.Core.Map;

namespace AshNCircuit.Core.Entities
{
    /// <summary>
    /// 矢などの投射物を表すエンティティ。
    /// タイル座標と環境タグ（burning など）を保持する。
    /// </summary>
    public class ProjectileEntity : Entity
    {
        public TileTag Tags { get; private set; }

        public ProjectileEntity(int x, int y, TileTag initialTags)
            : base(x, y)
        {
            Tags = initialTags;
        }

        public bool HasTag(TileTag tag)
        {
            return (Tags & tag) != 0;
        }

        public void AddTag(TileTag tag)
        {
            Tags |= tag;
        }

        public void RemoveTag(TileTag tag)
        {
            Tags &= ~tag;
        }
    }
}

