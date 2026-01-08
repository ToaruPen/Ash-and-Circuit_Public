using System;
using AshNCircuit.Core.Entities;
using AshNCircuit.Core.Map;
using AshNCircuit.Core.States;
using AshNCircuit.Core.Systems;
using AshNCircuit.UnityIntegration.Content;
using UnityEngine;

namespace AshNCircuit.UnityIntegration.MapView
{
    public partial class GameRoot
    {
        private void InitializePlayerEntityAndProjectileView()
        {
            if (playerTransform == null)
            {
                Debug.LogWarning("GameRoot: playerTransform が未設定です。プレイヤー移動は行われません。");
                return;
            }

            var gridX = Mathf.RoundToInt(playerTransform.position.x / tileSize);
            var gridY = Mathf.RoundToInt(playerTransform.position.y / tileSize);
            _playerEntity = new PlayerEntity(gridX, gridY);
            SyncPlayerTransform();

            var playerRenderer = playerTransform.GetComponentInChildren<SpriteRenderer>();
            if (playerRenderer != null)
            {
                _projectileSortingLayerName = playerRenderer.sortingLayerName;
                _projectileSortingOrder = playerRenderer.sortingOrder + 1;

                _projectileView = new ProjectileViewPresenter(
                    projectilesParent,
                    arrowSprite,
                    tileSize,
                    _projectileSortingLayerName,
                    _projectileSortingOrder,
                    _sfxPlayer,
                    _effectSystem,
                    _mapManager,
                    _logSystem,
                    _mapView);
            }

            _gameController.SetPlayerEntity(_playerEntity);
        }

        private void InitializeUiAndInputControllers()
        {
            if (_playerEntity == null || playerTransform == null)
            {
                return;
            }

            InitializeUi();

            if (_sfxPlayer != null)
            {
                _contextMenuView?.SetSfxPlayer(_sfxPlayer);
                _inventoryView?.SetSfxPlayer(_sfxPlayer);
            }

            var inventoryView = _inventoryView;
            if (inventoryView != null)
            {
                _throwTargetingController = new ThrowTargetingController(
                    _playerEntity!,
                    _mapManager,
                    _logSystem,
                    _mapView,
                    inventoryView,
                    HandleProjectileAnimationRequested,
                    tileSize,
                    _gameController);

                System.Func<int, int, EnemyEntity?>? findEnemyAt =
                    _enemyAiSystem != null ? (x, y) => _enemyAiSystem.FindEnemyAt(x, y) : null;

                var worldRng = new WorldRngState(discoverySeed);
                _tileContextMenuController = new TileContextMenuController(
                    _mapManager,
                    inventoryView,
                    _logSystem,
                    _gameController,
                    worldRng,
                    findEnemyAt);
            }
            else
            {
                _throwTargetingController = null;
                _tileContextMenuController = null;
            }

            _playerInputController = new PlayerInputController(
                _playerEntity!,
                _mapManager,
                _mapView,
                _logSystem,
                _inventoryView,
                _contextMenuView,
                _throwTargetingController,
                _tileContextMenuController,
                playerTransform!,
                tileSize,
                HandleProjectileAnimationRequested,
                _gameController);
        }

        private void HandleProjectileAnimationRequested(ProjectileResult result, Vector2 direction)
        {
            if (_projectileView != null)
            {
                StartCoroutine(_projectileView.PlayProjectileAnimation(result, direction));
            }
        }

        private void InitializeEnemySystem()
        {
            if (_mapManager == null || _actionSystem == null || _logSystem == null || _turnManager == null)
            {
                return;
            }

            var bootstrapper = new EnemyBootstrapper();
            _enemyAiSystem = bootstrapper.Bootstrap(
                _mapManager,
                _actionSystem,
                _logSystem,
                _turnManager,
                _playerEntity);

            if (_enemyAiSystem != null)
            {
                if (tilesParent == null)
                {
                    throw new InvalidOperationException("GameRoot: tilesParent が未設定のため、敵スプライトを生成できません。");
                }

                var enemiesParentObject = new GameObject("Enemies");
                enemiesParentObject.transform.SetParent(tilesParent, false);

                var spriteCatalog = SpriteCatalog.LoadFromResources();
                _enemyViewPresenter = new EnemyViewPresenter(_enemyAiSystem, enemiesParentObject.transform, spriteCatalog, tileSize);
                _enemyViewPresenter.Refresh();
            }
        }
    }
}
