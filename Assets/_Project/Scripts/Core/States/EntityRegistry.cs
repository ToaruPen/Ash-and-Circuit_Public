using System.Collections.Generic;
using AshNCircuit.Core.Entities;

namespace AshNCircuit.Core.States
{
    /// <summary>
    /// 動く存在（プレイヤー/敵など）をまとめるレジストリ（B案）。
    /// MVPでは「参照点を束ねる」ことを優先し、複雑な責務を持たせない。
    /// </summary>
    public sealed class EntityRegistry
    {
        private readonly List<EnemyEntity> _enemies = new List<EnemyEntity>();

        public PlayerEntity? Player { get; private set; }

        public IReadOnlyList<EnemyEntity> Enemies => _enemies;

        public void SetPlayer(PlayerEntity player)
        {
            Player = player;
        }

        public void RegisterEnemy(EnemyEntity enemy)
        {
            if (enemy == null)
            {
                return;
            }

            if (!_enemies.Contains(enemy))
            {
                _enemies.Add(enemy);
            }
        }

        public bool UnregisterEnemy(EnemyEntity enemy)
        {
            if (enemy == null)
            {
                return false;
            }

            return _enemies.Remove(enemy);
        }

        public EnemyEntity? FindEnemyAt(int x, int y)
        {
            foreach (var enemy in _enemies)
            {
                if (enemy == null || enemy.IsDead)
                {
                    continue;
                }

                if (enemy.X == x && enemy.Y == y)
                {
                    return enemy;
                }
            }

            return null;
        }
    }
}

