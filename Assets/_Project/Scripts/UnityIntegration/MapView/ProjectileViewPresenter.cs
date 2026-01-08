using AshNCircuit.Core.Map;
using AshNCircuit.Core.Systems;
using AshNCircuit.UnityIntegration.Audio;
using UnityEngine;

namespace AshNCircuit.UnityIntegration.MapView
{
    /// <summary>
    /// ProjectileResult に基づき矢スプライトをアニメーション表示するプレゼンター。
    /// ロジック（RULE 適用や環境更新）は Core 層で行い、本クラスは見た目のみを担当する。
    /// </summary>
    public sealed class ProjectileViewPresenter
    {
        private readonly Transform _projectilesParent;
        private readonly Sprite _arrowSprite;
        private readonly float _tileSize;
        private readonly string _sortingLayerName;
        private readonly int _sortingOrder;
        private readonly SfxPlayer? _sfxPlayer;

        public ProjectileViewPresenter(
            Transform projectilesParent,
            Sprite arrowSprite,
            float tileSize,
            string sortingLayerName,
            int sortingOrder,
            SfxPlayer? sfxPlayer,
            EffectSystem effectSystem,
            MapManager mapManager,
            LogSystem logSystem,
            MapViewPresenter mapView)
        {
            _projectilesParent = projectilesParent;
            _arrowSprite = arrowSprite;
            _tileSize = tileSize;
            _sortingLayerName = sortingLayerName ?? string.Empty;
            _sortingOrder = sortingOrder;
            _sfxPlayer = sfxPlayer;
        }

        public System.Collections.IEnumerator PlayProjectileAnimation(
            ProjectileResult result,
            Vector2 direction)
        {
            if (_arrowSprite == null || _projectilesParent == null)
            {
                yield break;
            }

            if (result == null || result.TrajectoryTiles == null || result.TrajectoryTiles.Count == 0)
            {
                yield break;
            }

            var arrowObject = new GameObject("Arrow");
            arrowObject.transform.SetParent(_projectilesParent, false);

            var renderer = arrowObject.AddComponent<SpriteRenderer>();
            renderer.sprite = _arrowSprite;

            // プレイヤーの描画レイヤー設定を継承することで、地面や壁より前に表示する。
            if (!string.IsNullOrEmpty(_sortingLayerName))
            {
                renderer.sortingLayerName = _sortingLayerName;
            }
            renderer.sortingOrder = _sortingOrder;

            // 矢の向きをベクトル方向に合わせる。
            if (direction.sqrMagnitude > 0.0001f)
            {
                var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
                arrowObject.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }

            // 経路上を順に移動させる（impact まで）。
            var path = result.TrajectoryTiles;
            var stepCount = path.Count;
            int? impactStepIndex = null;
            if (result.HasImpact && result.ImpactIndex.HasValue)
            {
                stepCount = result.ImpactIndex.Value + 1;
                impactStepIndex = result.ImpactIndex.Value;
            }
            else if (result.ImpactKind == ProjectileImpactKind.Ground)
            {
                impactStepIndex = stepCount - 1;
            }

            _sfxPlayer?.PlayOneShot(SfxIds.CombatShoot);

            for (var i = 0; i < stepCount; i++)
            {
                var tile = path[i];
                var worldX = tile.X * _tileSize;
                var worldY = tile.Y * _tileSize;
                arrowObject.transform.position = new Vector3(worldX, worldY, 0f);

                // 数フレームに1タイル進むシンプルなアニメーション。
                yield return new WaitForSeconds(0.03f);

                if (impactStepIndex.HasValue && impactStepIndex.Value == i && result.ImpactKind != ProjectileImpactKind.None)
                {
                    _sfxPlayer?.PlayOneShot(SfxIds.CombatProjectileHit);
                }
            }

            Object.Destroy(arrowObject);
        }
    }
}
