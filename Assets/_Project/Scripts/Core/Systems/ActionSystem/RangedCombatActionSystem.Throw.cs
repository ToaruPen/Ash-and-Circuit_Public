using System;
using System.Collections.Generic;
using AshNCircuit.Core.Entities;
using AshNCircuit.Core.Items;
using AshNCircuit.Core.Map;

namespace AshNCircuit.Core.Systems
{
    public partial class RangedCombatActionSystem
    {
        public ProjectileResult? TryThrowItem(
            PlayerEntity player,
            ItemDefinition? item,
            GridPosition targetTile,
            MapManager mapManager,
            LogSystem logSystem)
        {
            if (IsInvalidPlayer(player) || item == null || mapManager == null || logSystem == null)
            {
                return null;
            }

            var inventory = player.Inventory;
            if (inventory == null || inventory.GetTotalCount(item) <= 0)
            {
                logSystem.LogById(MessageId.ThrowMissingItem, item.DisplayName);
                return null;
            }

            var start = new GridPosition(player.X, player.Y);
            var dx = targetTile.X - start.X;
            var dy = targetTile.Y - start.Y;
            var distance = Math.Max(Math.Abs(dx), Math.Abs(dy));

            if (distance == 0)
            {
                logSystem.LogById(MessageId.ThrowTargetIsSelf);
                return null;
            }

            if (distance > DefaultThrowRange)
            {
                logSystem.LogById(MessageId.ThrowTooFar);
                return null;
            }

            if (item == ItemDefinition.WoodenArrow)
            {
                var parameters = new ProjectileParams(DefaultThrowRange, false, TileTag.None);
                var result = PerformThrowProjectile(player, targetTile, parameters, mapManager, logSystem);
                if (result != null)
                {
                    inventory.TryRemove(item, 1);
                    logSystem.LogById(MessageId.ThrowWoodenArrowFlavor);
                }

                return result;
            }

            if (item == ItemDefinition.OilBottle)
            {
                inventory.TryRemove(item, 1);

                if (!mapManager.IsInBounds(targetTile.X, targetTile.Y))
                {
                    logSystem.LogById(MessageId.ThrowOilBottleLost);
                    return null;
                }

                if (mapManager.GetSolidType(targetTile.X, targetTile.Y).HasValue || mapManager.TryGetProp(targetTile.X, targetTile.Y, out _))
                {
                    logSystem.LogById(MessageId.ThrowOilBottleNoSpread);
                    return null;
                }

                var currentGround = mapManager.GetGroundType(targetTile.X, targetTile.Y);
                switch (currentGround)
                {
                    case TileType.GroundNormal:
                    case TileType.GroundBurnt:
                    case TileType.GroundWater:
                        mapManager.SetGroundType(targetTile.X, targetTile.Y, TileType.GroundOil);
                        logSystem.LogById(MessageId.ThrowOilBottleCreatePuddle);
                        break;
                    default:
                        logSystem.LogById(MessageId.ThrowOilBottleNoSpread);
                        break;
                }

                return null;
            }

            if (item == ItemDefinition.DirtClod)
            {
                inventory.TryRemove(item, 1);
                logSystem.LogById(MessageId.ThrowDirtClodFlavor);
                return null;
            }

            logSystem.LogById(MessageId.ThrowGenericNoEffect, item.DisplayName);
            return null;
        }

        private ProjectileResult? PerformThrowProjectile(
            PlayerEntity player,
            GridPosition targetTile,
            ProjectileParams parameters,
            MapManager mapManager,
            LogSystem logSystem)
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

            var maxRange = parameters.Range;
            var trajectory = mapManager.GetLineTrajectory(start, targetTile, maxRange);
            var projectile = new ProjectileEntity(player.X, player.Y, parameters.InitialTags);

            if (trajectory == null || trajectory.Count == 0)
            {
                logSystem.LogById(MessageId.ThrowProjectileDroppedAtFeet);
                return new ProjectileResult(projectile, new List<GridPosition>(), null);
            }

            var sim = SimulateProjectileTrajectory(trajectory, parameters, mapManager);
            if (sim.ImpactIndex.HasValue && sim.ImpactKind == ProjectileImpactKind.Enemy)
            {
                var enemyName = sim.ImpactEnemy != null ? sim.ImpactEnemy.DisplayName : "敵";
                logSystem.LogById(MessageId.ProjectileHitEnemy, enemyName);
            }

            projectile.SetPosition(sim.FinalPosition.X, sim.FinalPosition.Y);

            return new ProjectileResult(projectile, trajectory, sim.ImpactIndex, sim.ImpactKind, sim.ImpactEnemy);
        }
    }
}
