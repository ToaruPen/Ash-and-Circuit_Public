using System;
using System.Collections.Generic;
using AshNCircuit.Core.Entities;
using AshNCircuit.Core.Map;

namespace AshNCircuit.Core.Systems
{
    /// <summary>
    /// MVP 用のシンプルな敵AIを管理するシステム。
    /// - 一定距離内のプレイヤーを追跡
    /// - 隣接したら近接攻撃
    /// </summary>
    public sealed class EnemyAISystem
    {
        private readonly List<EnemyEntity> _enemies = new List<EnemyEntity>();
        private readonly MapManager _mapManager;
        private readonly ActionSystem _actionSystem;
        private readonly LogSystem _logSystem;

        private PlayerEntity? _player;

        private const int DetectionRange = 5;

        public EnemyAISystem(MapManager mapManager, ActionSystem actionSystem, LogSystem logSystem)
        {
            _mapManager = mapManager ?? throw new ArgumentNullException(nameof(mapManager));
            _actionSystem = actionSystem ?? throw new ArgumentNullException(nameof(actionSystem));
            _logSystem = logSystem ?? throw new ArgumentNullException(nameof(logSystem));
        }

        public void SetPlayer(PlayerEntity player)
        {
            _player = player;
        }

        public void AddEnemy(EnemyEntity enemy)
        {
            if (enemy == null)
            {
                return;
            }

            _enemies.Add(enemy);
        }

        /// <summary>
        /// 登録済みの敵エンティティを列挙する。
        /// 状態異常フェーズでの tick などに使用する。
        /// </summary>
        public IEnumerable<EnemyEntity> EnumerateEnemies()
        {
            return _enemies;
        }

        /// <summary>
        /// 指定座標に存在する生存中の敵を検索する。
        /// ActionSystem からの体当たり攻撃判定に使用する。
        /// </summary>
        public EnemyEntity? FindEnemyAt(int x, int y)
        {
            for (var i = 0; i < _enemies.Count; i++)
            {
                var enemy = _enemies[i];
                if (enemy.IsDead)
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

        /// <summary>
        /// TurnManager.OnEnemyPhase から呼び出される入口。
        /// </summary>
        public void HandleEnemyPhase()
        {
            if (_player == null || _player.IsDead)
            {
                return;
            }

            if (_enemies.Count == 0)
            {
                return;
            }

            foreach (var enemy in _enemies)
            {
                if (enemy.IsDead)
                {
                    continue;
                }

                var dx = Math.Abs(enemy.X - _player.X);
                var dy = Math.Abs(enemy.Y - _player.Y);
                var distance = dx + dy;

                if (distance <= 1)
                {
                    // 隣接していれば近接攻撃。
                    _actionSystem.TryMeleeAttack(enemy, _player, _logSystem);

                    // プレイヤーが死亡したら、それ以上の敵行動は不要。
                    if (_player.IsDead)
                    {
                        break;
                    }
                }
                else if (distance <= DetectionRange)
                {
                    // シンプルにプレイヤーへ 1 マス近づく。
                    TryMoveEnemyTowardsPlayer(enemy);
                }
            }
        }

        private void TryMoveEnemyTowardsPlayer(EnemyEntity enemy)
        {
            if (_player == null)
            {
                return;
            }

            var dx = _player.X - enemy.X;
            var dy = _player.Y - enemy.Y;

            int stepX = 0;
            int stepY = 0;

            // X/Y どちらの距離が大きいかで優先的に進む方向を決める。
            if (Math.Abs(dx) > Math.Abs(dy))
            {
                stepX = Math.Sign(dx);
            }
            else if (dy != 0)
            {
                stepY = Math.Sign(dy);
            }

            if (stepX == 0 && stepY == 0)
            {
                return;
            }

            var targetX = enemy.X + stepX;
            var targetY = enemy.Y + stepY;

            // マップ外・壁ブロックは移動しない。
            if (!_mapManager.IsInBounds(targetX, targetY) || !_mapManager.IsWalkable(targetX, targetY))
            {
                return;
            }

            // プレイヤーの位置には侵入しない。
            if (!_player.IsDead && _player.X == targetX && _player.Y == targetY)
            {
                return;
            }

            // 他の敵が既にいるタイルも避ける。
            var other = FindEnemyAt(targetX, targetY);
            if (other != null && !other.IsDead && !ReferenceEquals(other, enemy))
            {
                return;
            }

            enemy.SetPosition(targetX, targetY);
        }
    }
}
