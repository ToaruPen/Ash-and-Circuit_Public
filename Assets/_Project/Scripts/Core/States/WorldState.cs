using System;

namespace AshNCircuit.Core.States
{
    /// <summary>
    /// WorldState（B案）の薄い集約点。
    /// - 「世界の現在状態（データの箱）」を束ねるだけで、ルール実行や生成の責務は持たない。
    /// - Unity API への依存を持たない（Core 層）。
    /// </summary>
    public sealed class WorldState
    {
        public MapState Map { get; }

        public EntityRegistry Entities { get; }

        public WorldRngState Rng { get; }

        public WorldState(MapState map, EntityRegistry entities, WorldRngState rng)
        {
            Map = map ?? throw new ArgumentNullException(nameof(map));
            Entities = entities ?? throw new ArgumentNullException(nameof(entities));
            Rng = rng ?? throw new ArgumentNullException(nameof(rng));
        }
    }
}

