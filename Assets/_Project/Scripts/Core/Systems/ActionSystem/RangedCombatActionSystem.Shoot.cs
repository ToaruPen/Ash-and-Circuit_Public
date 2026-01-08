using System;
using System.Collections.Generic;
using AshNCircuit.Core.Entities;
using AshNCircuit.Core.Map;

namespace AshNCircuit.Core.Systems
{
    public partial class RangedCombatActionSystem
    {
        public ProjectileResult? TryShootProjectile(PlayerEntity player, int deltaX, int deltaY, MapManager mapManager, LogSystem logSystem)
        {
            if (IsInvalidPlayer(player) || mapManager == null || logSystem == null)
            {
                return null;
            }

            if (deltaX == 0 && deltaY == 0)
            {
                return null;
            }

            var range = Math.Max(DefaultProjectileRange, Math.Max(mapManager.Width, mapManager.Height));
            var targetX = player.X + deltaX * range;
            var targetY = player.Y + deltaY * range;
            var target = new GridPosition(targetX, targetY);

            var parameters = ProjectileParams.CreateBasicArrow(range);
            return PerformShootProjectile(player, target, parameters, mapManager, logSystem);
        }

        public ProjectileResult? PerformShootProjectile(PlayerEntity player, GridPosition targetTile, ProjectileParams parameters, MapManager mapManager, LogSystem logSystem)
        {
            if (IsInvalidPlayer(player) || mapManager == null || logSystem == null || parameters == null)
            {
                return null;
            }

            var start = new GridPosition(player.X, player.Y);

            var dx = targetTile.X - start.X;
            var dy = targetTile.Y - start.Y;
            if (dx == 0 && dy == 0)
            {
                return null;
            }

            var directionName = GetDirectionNameFromVector(dx, dy);
            if (!string.IsNullOrEmpty(directionName))
            {
                logSystem.LogById(MessageId.ShootDirectional, directionName);
            }
            else
            {
                logSystem.LogById(MessageId.ShootGeneric);
            }

            var maxRange = Math.Max(parameters.Range, Math.Max(mapManager.Width, mapManager.Height));
            var trajectory = mapManager.GetLineTrajectory(start, targetTile, maxRange);
            var projectile = new ProjectileEntity(player.X, player.Y, parameters.InitialTags);

            if (trajectory == null || trajectory.Count == 0)
            {
                logSystem.LogById(MessageId.ShootBlockedImmediately);
                return new ProjectileResult(projectile, new List<GridPosition>(), null);
            }

            var sim = SimulateProjectileTrajectory(trajectory, parameters, mapManager);
            if (sim.ImpactIndex.HasValue)
            {
                if (sim.ImpactKind == ProjectileImpactKind.Enemy)
                {
                    var enemyName = sim.ImpactEnemy != null ? sim.ImpactEnemy.DisplayName : "敵";
                    logSystem.LogById(MessageId.ProjectileHitEnemy, enemyName);
                }
                else
                {
                    var impactTileType = mapManager.GetTileType(sim.FinalPosition.X, sim.FinalPosition.Y);
                    var impactDesc = GetImpactDescription(impactTileType);
                    logSystem.LogById(MessageId.ShootHitSurface, impactDesc);
                }
            }
            else
            {
                logSystem.LogById(MessageId.ShootFellToGround);
            }

            projectile.SetPosition(sim.FinalPosition.X, sim.FinalPosition.Y);

            return new ProjectileResult(projectile, trajectory, sim.ImpactIndex, sim.ImpactKind, sim.ImpactEnemy);
        }
    }
}

