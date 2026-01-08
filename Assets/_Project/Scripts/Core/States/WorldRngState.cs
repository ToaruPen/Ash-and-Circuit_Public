namespace AshNCircuit.Core.States
{
    /// <summary>
    /// WorldRngState（B案）。
    /// - 生成/戦利品/AI の 3 ストリームを保持し、システム間の乱数消費順干渉を抑える。
    /// - ここでの RNG は決定的であり、UnityEngine.Random / System.Random の実装差に依存しない。
    /// </summary>
    public sealed class WorldRngState
    {
        public int RunSeed { get; }

        public RngStream GenRng { get; }

        public RngStream LootRng { get; }

        public RngStream AiRng { get; }

        public WorldRngState(int runSeed)
        {
            RunSeed = runSeed;

            // runSeed から派生シードを作るためのシーダー（SplitMix64）。
            var seeder = new RngStream(ExpandSeedTo64(runSeed));

            GenRng = new RngStream(seeder.NextUInt64());
            LootRng = new RngStream(seeder.NextUInt64());
            AiRng = new RngStream(seeder.NextUInt64());
        }

        private static ulong ExpandSeedTo64(int seed)
        {
            unchecked
            {
                // 32-bit の seed を 64-bit に拡張（符号ビットを含めて同一ビット列を再現）。
                var u = (ulong)(uint)seed;
                return (u << 32) | u;
            }
        }
    }

    /// <summary>
    /// 決定的な RNG ストリーム（SplitMix64）。
    /// - State は内部状態（進行状態）であり、同一 State なら同一列が出力される。
    /// </summary>
    public sealed class RngStream
    {
        private ulong _state;

        public ulong State => _state;

        public RngStream(ulong seed)
        {
            _state = seed;
        }

        public RngStream Clone()
        {
            return new RngStream(_state);
        }

        public ulong NextUInt64()
        {
            unchecked
            {
                _state += 0x9E3779B97F4A7C15UL;
                var z = _state;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }

        public uint NextUInt32()
        {
            return (uint)(NextUInt64() >> 32);
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                return minInclusive;
            }

            unchecked
            {
                var range = (uint)(maxExclusive - minInclusive);
                var value = NextUInt32();
                return minInclusive + (int)(value % range);
            }
        }
    }

    /// <summary>
    /// lootSeed / dropSeed を「順序独立」に導出するためのユーティリティ。
    /// - 生成結果がグローバル RNG（LootRng）の消費順に依存しないようにする。
    /// - .NET の string.GetHashCode() はプロセスごとに変わり得るため使用しない。
    /// </summary>
    public static class RngSeedDerivation
    {
        private const ulong LootSeedSalt = 0x4C4F4F545F534545UL; // "LOOT_SEE"
        private const ulong DropSeedSalt = 0x44524F505F534545UL; // "DROP_SEE"

        public static ulong DeriveLootSeed(WorldRngState worldRng, int x, int y, string propId)
        {
            return DeriveLootSeed(worldRng?.RunSeed ?? 0, x, y, propId);
        }

        public static ulong DeriveDropSeed(WorldRngState worldRng, int x, int y, string enemyId)
        {
            return DeriveDropSeed(worldRng?.RunSeed ?? 0, x, y, enemyId);
        }

        public static ulong DeriveLootSeed(int runSeed, int x, int y, string propId)
        {
            return DeriveSeed(runSeed, x, y, propId, LootSeedSalt);
        }

        public static ulong DeriveDropSeed(int runSeed, int x, int y, string enemyId)
        {
            return DeriveSeed(runSeed, x, y, enemyId, DropSeedSalt);
        }

        private static ulong DeriveSeed(int runSeed, int x, int y, string stableId, ulong salt)
        {
            unchecked
            {
                var seed = ExpandSeedTo64(runSeed);
                seed ^= salt;
                seed ^= PackXY(x, y);
                seed ^= Fnv1a64(stableId ?? string.Empty);
                return Mix64(seed);
            }
        }

        private static ulong ExpandSeedTo64(int seed)
        {
            unchecked
            {
                var u = (ulong)(uint)seed;
                return (u << 32) | u;
            }
        }

        private static ulong PackXY(int x, int y)
        {
            unchecked
            {
                return ((ulong)(uint)x << 32) | (uint)y;
            }
        }

        private static ulong Mix64(ulong z)
        {
            unchecked
            {
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }

        private static ulong Fnv1a64(string value)
        {
            unchecked
            {
                var hash = 14695981039346656037UL;
                const ulong prime = 1099511628211UL;

                for (var i = 0; i < value.Length; i++)
                {
                    var c = value[i];
                    hash ^= (byte)c;
                    hash *= prime;
                    hash ^= (byte)(c >> 8);
                    hash *= prime;
                }

                return hash;
            }
        }
    }
}
