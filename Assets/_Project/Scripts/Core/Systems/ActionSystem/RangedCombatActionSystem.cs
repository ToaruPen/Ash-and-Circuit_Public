using System;
using AshNCircuit.Core.Entities;

namespace AshNCircuit.Core.Systems
{
    /// <summary>
    /// 射撃/投擲（Projectile）に関わるアクションを扱うサブシステム。
    /// ActionSystem から委譲され、public API/挙動を維持するための分割ステップとして導入する。
    /// </summary>
    public partial class RangedCombatActionSystem
    {
        private const int DefaultProjectileRange = 32;
        private const int DefaultThrowRange = 5;

        private Func<int, int, EnemyEntity?>? _findEnemyAt;

        public void SetEnemyLocator(Func<int, int, EnemyEntity?>? findEnemyAt)
        {
            _findEnemyAt = findEnemyAt;
        }
    }
}
