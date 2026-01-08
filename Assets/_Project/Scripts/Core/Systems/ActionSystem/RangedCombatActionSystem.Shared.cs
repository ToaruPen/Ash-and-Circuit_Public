using System;
using System.Collections.Generic;
using AshNCircuit.Core.Entities;
using AshNCircuit.Core.Map;

namespace AshNCircuit.Core.Systems
{
    public partial class RangedCombatActionSystem
    {
        private readonly struct ProjectileSimulationResult
        {
            public GridPosition FinalPosition { get; }
            public int? ImpactIndex { get; }
            public ProjectileImpactKind ImpactKind { get; }
            public EnemyEntity? ImpactEnemy { get; }

            public ProjectileSimulationResult(
                GridPosition finalPosition,
                int? impactIndex,
                ProjectileImpactKind impactKind,
                EnemyEntity? impactEnemy)
            {
                FinalPosition = finalPosition;
                ImpactIndex = impactIndex;
                ImpactKind = impactKind;
                ImpactEnemy = impactEnemy;
            }
        }

        private ProjectileSimulationResult SimulateProjectileTrajectory(
            IReadOnlyList<GridPosition> trajectory,
            ProjectileParams parameters,
            MapManager mapManager)
        {
            int? impactIndex = null;
            ProjectileImpactKind impactKind = ProjectileImpactKind.None;
            EnemyEntity? impactEnemy = null;
            for (var i = 0; i < trajectory.Count; i++)
            {
                var pos = trajectory[i];

                var enemy = FindAliveEnemyAt(pos.X, pos.Y);
                if (enemy != null)
                {
                    impactIndex = i;
                    impactKind = ProjectileImpactKind.Enemy;
                    impactEnemy = enemy;
                    break;
                }

                if (mapManager.BlocksProjectiles(pos.X, pos.Y) && !parameters.CanPierce)
                {
                    impactIndex = i;
                    impactKind = ProjectileImpactKind.BlockingTile;
                    break;
                }
            }

            GridPosition finalPosition;
            if (impactIndex.HasValue)
            {
                finalPosition = trajectory[impactIndex.Value];
            }
            else
            {
                finalPosition = trajectory[trajectory.Count - 1];
                impactKind = ProjectileImpactKind.Ground;
            }

            return new ProjectileSimulationResult(finalPosition, impactIndex, impactKind, impactEnemy);
        }

        private EnemyEntity? FindAliveEnemyAt(int x, int y)
        {
            if (_findEnemyAt == null)
            {
                return null;
            }

            var enemy = _findEnemyAt(x, y);
            if (enemy == null || enemy.IsDead)
            {
                return null;
            }

            return enemy;
        }

        private static bool IsInvalidPlayer(PlayerEntity? player)
        {
            return player == null || player.IsDead;
        }

        private static string GetDirectionNameFromVector(int dx, int dy)
        {
            var angle = Math.Atan2(dy, dx);
            var octant = (int)Math.Round(angle / (Math.PI / 4.0));
            octant = (octant + 8) % 8;

            switch (octant)
            {
                case 0:
                    return "東";
                case 1:
                    return "北東";
                case 2:
                    return "北";
                case 3:
                    return "北西";
                case 4:
                    return "西";
                case 5:
                    return "南西";
                case 6:
                    return "南";
                case 7:
                    return "南東";
                default:
                    return string.Empty;
            }
        }

        private static string GetImpactDescription(TileType tileType)
        {
            switch (tileType)
            {
                case TileType.WallStone:
                case TileType.WallMetal:
                    return "壁";
                case TileType.TreeNormal:
                case TileType.TreeBurning:
                case TileType.TreeBurnt:
                    return "木";
                default:
                    return "何か";
            }
        }
    }
}
