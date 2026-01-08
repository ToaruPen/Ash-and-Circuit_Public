using AshNCircuit.Core.Items;
using AshNCircuit.Core.Map;
using AshNCircuit.Core.States;

namespace AshNCircuit.Core.Entities
{
    /// <summary>
    /// 基本的な敵キャラクターを表すエンティティ。
    /// MVP 段階では単一種の近接敵のみを想定し、簡易なステータスを持つ。
    /// </summary>
    public sealed class EnemyEntity : Entity
    {
        private ItemPile? _rolledDrop;
        private readonly int _spawnX;
        private readonly int _spawnY;

        /// <summary>
        /// デバッグ用の識別子。
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// ログ表示用の名前。
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// 敵ドロップのロールに使用する個体 seed。
        /// - 死亡時に 1 回だけ確定し、倒す順番に依存しない（順序独立・再現性を優先）。
        /// </summary>
        public ulong? DropSeed { get; private set; }

        public ItemPile? RolledDrop => _rolledDrop;

        public EnemyEntity(
            int x,
            int y,
            int maxHp,
            int attack,
            int defense,
            string id = "enemy_basic",
            string displayName = "敵")
            : base(x, y)
        {
            _spawnX = x;
            _spawnY = y;

            Id = id;
            DisplayName = displayName;

            InitializeStats(maxHp, attack, defense);
        }

        public bool TryRollDropIfNeeded(WorldRngState worldRng, int dropTurn)
        {
            if (!IsDead || worldRng == null)
            {
                return false;
            }

            if (_rolledDrop != null)
            {
                return true;
            }

            var seed = DropSeed ?? RngSeedDerivation.DeriveDropSeed(worldRng, _spawnX, _spawnY, Id);
            DropSeed = seed;

            var rng = new RngStream(seed);
            _rolledDrop = CreateBasicDrop(rng, dropTurn);
            return true;
        }

        private static ItemPile CreateBasicDrop(RngStream rng, int dropTurn)
        {
            var pile = new ItemPile();
            var roll = rng.NextInt(0, 100);

            if (roll < 60)
            {
                pile.Add(ItemDefinition.DirtClod, amount: 1, dropTurn: dropTurn);
            }
            else
            {
                pile.Add(ItemDefinition.WoodenArrow, amount: 1 + rng.NextInt(0, 2), dropTurn: dropTurn);
            }

            return pile;
        }
    }
}
