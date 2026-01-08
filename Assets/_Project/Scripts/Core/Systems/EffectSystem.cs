using System.Collections.Generic;
using AshNCircuit.Core.Entities;
using AshNCircuit.Core.Map;

namespace AshNCircuit.Core.Systems
{
    /// <summary>
    /// RULE E-01/E-03、RULE P-01 など環境相互作用を扱うシステムクラス。
    /// 本チケットでは RULE P-01（矢×火）と RULE E-01（火×木）のみ実装する。
    /// </summary>
    public class EffectSystem
    {
        private const int DefaultBurningDurationTurns = 3;
        private const int DefaultBurningDamagePerTurn = 1;

        private readonly Dictionary<GridPosition, BurningTileInfo> _burningTiles =
            new Dictionary<GridPosition, BurningTileInfo>();

        // Projectile フェーズで検出された「次の環境フェーズで燃え始めるべき木」の位置。
        private readonly HashSet<GridPosition> _pendingTreeIgnitions =
            new HashSet<GridPosition>();

        /// <summary>
        /// 投射物の結果に基づき、RULE P-01 / RULE E-01 のトリガーを適用する。
        /// - P-01: このメソッド内（投射物フェーズ）で矢に burning タグを付与する。
        /// - E-01:「どの木が燃え始めるべきか」のトリガーのみを登録し、実際の Ignite は環境フェーズで行う。
        /// </summary>
        public void ApplyProjectileEnvironmentRules(ProjectileResult result, MapManager mapManager, LogSystem logSystem)
        {
            if (result == null || mapManager == null || logSystem == null)
            {
                return;
            }

            ApplyRuleP01(result, mapManager, logSystem);
            QueueRuleE01Triggers(result, mapManager);
        }

        /// <summary>
        /// RULE P-01: 矢が火タイルや burning タイルを通過した場合に矢を燃え上がらせる。
        /// </summary>
        private void ApplyRuleP01(ProjectileResult result, MapManager mapManager, LogSystem logSystem)
        {
            var projectile = result.Projectile;
            if (projectile == null)
            {
                return;
            }

            // すでに burning タグを持つ矢はそのまま扱う。
            if (projectile.HasTag(TileTag.Burning))
            {
                return;
            }

            foreach (var position in result.TrajectoryTiles)
            {
                var tileType = mapManager.GetTileType(position.X, position.Y);
                var tags = MapManager.GetTags(tileType);

                var isBurningTile = tileType == TileType.FireTile || (tags & TileTag.Burning) != 0;
                if (!isBurningTile)
                {
                    continue;
                }

                projectile.AddTag(TileTag.Burning);
                logSystem.LogArrowIgnited();
                break;
            }
        }

        /// <summary>
        /// RULE E-01: burning タグ付き投射物が木（tree_normal）に命中した場合に、
        /// 「次の環境フェーズで燃え始めるべき木」をキューに登録する。
        /// </summary>
        private void QueueRuleE01Triggers(ProjectileResult result, MapManager mapManager)
        {
            var projectile = result.Projectile;
            if (projectile == null || !projectile.HasTag(TileTag.Burning))
            {
                return;
            }

            var impactTile = result.ImpactTile;
            if (!impactTile.HasValue)
            {
                return;
            }

            var position = impactTile.Value;
            var solidType = mapManager.GetSolidType(position.X, position.Y);
            if (!solidType.HasValue)
            {
                return;
            }

            var tags = MapManager.GetTags(solidType.Value);

            if ((tags & TileTag.Flammable) == 0)
            {
                return;
            }

            // MVPでは主役である tree_normal のみを対象とし、その他の flammable タイルは別RULE（E-02など）で扱う。
            if (solidType.Value == TileType.TreeNormal)
            {
                _pendingTreeIgnitions.Add(position);
            }
        }

        /// <summary>
        /// RULE E-01: 木を燃焼状態（tree_burning）にし、数ターン後に燃え尽きるよう登録する。
        /// </summary>
        private void IgniteTree(GridPosition position, MapManager mapManager, LogSystem logSystem)
        {
            var currentSolid = mapManager.GetSolidType(position.X, position.Y);
            if (!currentSolid.HasValue || currentSolid.Value != TileType.TreeNormal)
            {
                return;
            }

            mapManager.SetSolidType(position.X, position.Y, TileType.TreeBurning);
            logSystem.LogTreeIgnited();

            var info = new BurningTileInfo(
                position,
                TileType.TreeBurning,
                TileType.TreeBurnt,
                DefaultBurningDurationTurns);

            _burningTiles[position] = info;
        }

        /// <summary>
        /// 環境フェーズで呼び出されることを想定したメソッド。
        /// - Projectile フェーズで登録された E-01 のトリガーを適用し、木を燃焼状態に遷移させる。
        /// - すでに燃えているタイルの継続時間を1ターン分進め、燃え尽きたタイルを最終状態に変化させる。
        /// </summary>
        public void TickBurning(MapManager mapManager, LogSystem logSystem)
        {
            if (mapManager == null || logSystem == null)
            {
                return;
            }

            // まず、Projectile フェーズでキューされた木の Ignite を適用する。
            if (_pendingTreeIgnitions.Count > 0)
            {
                foreach (var position in _pendingTreeIgnitions)
                {
                    IgniteTree(position, mapManager, logSystem);
                }

                _pendingTreeIgnitions.Clear();
            }

            if (_burningTiles.Count == 0)
            {
                return;
            }

            var keys = new List<GridPosition>(_burningTiles.Keys);
            foreach (var key in keys)
            {
                var info = _burningTiles[key];
                info.RemainingTurns -= 1;

                if (info.RemainingTurns > 0)
                {
                    _burningTiles[key] = info;
                    continue;
                }

                mapManager.SetSolidType(key.X, key.Y, info.FinalTileType);
                logSystem.LogTreeBurnedOut();
                _burningTiles.Remove(key);
            }
        }

        /// <summary>
        /// 状態異常フェーズで呼び出されることを想定したメソッド。
        /// エンティティの burning 状態を 1 ターン分進め、burning ダメージを適用する。
        /// </summary>
        /// <param name="player">プレイヤーエンティティ。</param>
        /// <param name="enemies">敵エンティティの列挙。</param>
        /// <param name="logSystem">ログ出力先。</param>
        public void TickStatusEffects(PlayerEntity player, IEnumerable<EnemyEntity> enemies, LogSystem logSystem)
        {
            if (logSystem == null)
            {
                return;
            }

            if (player != null)
            {
                TickEntityBurning(player, isPlayer: true, logSystem);
            }

            if (enemies != null)
            {
                foreach (var enemy in enemies)
                {
                    if (enemy == null)
                    {
                        continue;
                    }

                    TickEntityBurning(enemy, isPlayer: false, logSystem);
                }
            }
        }

        private void TickEntityBurning(Entity entity, bool isPlayer, LogSystem logSystem)
        {
            if (entity == null || !entity.IsBurning || entity.IsDead)
            {
                return;
            }

            var damage = entity.ApplyRawDamage(DefaultBurningDamagePerTurn);
            entity.TickBurningDuration();

            if (damage <= 0)
            {
                return;
            }

            if (isPlayer)
            {
                logSystem.LogById(MessageId.BurningDamagePlayer);
            }
            else
            {
                logSystem.LogById(MessageId.BurningDamageEnemy);
            }
        }

        private struct BurningTileInfo
        {
            public GridPosition Position { get; }
            public TileType BurningTileType { get; }
            public TileType FinalTileType { get; }
            public int RemainingTurns { get; set; }

            public BurningTileInfo(GridPosition position, TileType burningTileType, TileType finalTileType, int remainingTurns)
            {
                Position = position;
                BurningTileType = burningTileType;
                FinalTileType = finalTileType;
                RemainingTurns = remainingTurns;
            }
        }
    }
}
