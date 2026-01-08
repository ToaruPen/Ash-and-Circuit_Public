using System;
using System.Collections;
using AshNCircuit.Core.Entities;
using AshNCircuit.Core.Systems;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AshNCircuit.UnityIntegration.MapView
{
    public partial class GameRoot
    {
        private void Awake()
        {
            InitializeCoreSystems();
            InitializeTurnSystems();
            ApplyDiscoveryZoneGenerationIfNeeded();
            BuildTileMapVisuals();
            InitializePlayerEntityAndProjectileView();
            InitializeEnemySystem();
            InitializeUiAndInputControllers();
        }

        private void OnDestroy()
        {
            _unityConsoleLogger?.Dispose();
            _unityConsoleLogger = null;

            if (_inventoryView != null)
            {
                _inventoryView.OnThrowRequested -= HandleThrowRequested;
                _inventoryView.OnDropRequested -= HandleDropRequested;
                _inventoryView.OnExamineRequested -= HandleExamineRequested;
            }

            if (_turnManager != null)
            {
                _turnManager.OnEnvironmentPhase -= HandleEnvironmentPhase;
                if (_enemyAiSystem != null)
                {
                    _turnManager.OnEnemyPhase -= _enemyAiSystem.HandleEnemyPhase;
                }
                _turnManager.OnStatusEffectPhase -= HandleStatusEffectPhase;
            }

            _logView?.Dispose();
        }

        private void Update()
        {
            _enemyViewPresenter?.Refresh();

            if (_playerEntity == null || playerTransform == null)
            {
                return;
            }

            _hudView?.Refresh();

            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            _playerInputController?.Tick(keyboard, mouse, Time.deltaTime);
        }

        private void BuildTileMapVisuals()
        {
            _mapView.BuildInitialTiles();
        }

        private void RefreshAllTileVisuals()
        {
            _mapView.RefreshAllTiles();
        }

        private void HandleEnvironmentPhase()
        {
            if (_effectSystem == null || _mapManager == null || _logSystem == null)
            {
                return;
            }

            _effectSystem.TickBurning(_mapManager, _logSystem);
            RefreshAllTileVisuals();
        }

        private void HandleStatusEffectPhase()
        {
            if (_effectSystem == null || _logSystem == null || _playerEntity == null)
            {
                return;
            }

            var enemies = _enemyAiSystem != null
                ? _enemyAiSystem.EnumerateEnemies()
                : Array.Empty<EnemyEntity>();

            _effectSystem.TickStatusEffects(_playerEntity, enemies, _logSystem);
        }

        private void SyncPlayerTransform()
        {
            if (_playerEntity == null || playerTransform == null)
            {
                return;
            }

            var x = _playerEntity.X * tileSize;
            var y = _playerEntity.Y * tileSize;
            playerTransform.position = new Vector3(x, y, playerTransform.position.z);
        }
    }
}
