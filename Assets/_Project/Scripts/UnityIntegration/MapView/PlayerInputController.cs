using System;
using AshNCircuit.Core.Entities;
using AshNCircuit.Core.Map;
using AshNCircuit.Core.Systems;
using AshNCircuit.Core.GameLoop;
using AshNCircuit.UnityIntegration.UI.ContextMenuUi;
using AshNCircuit.UnityIntegration.UI.InventoryUi;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AshNCircuit.UnityIntegration.MapView
{
    /// <summary>
    /// プレイヤーのキーボード／マウス入力を解釈し、
    /// Core の ActionSystem / EffectSystem と Unity 側のビューを橋渡しするコントローラ。
    /// GameRoot から切り離された責務として、入力ブロックや行動実行のマッピングのみを担当する。
    /// </summary>
    public sealed partial class PlayerInputController
    {
        private readonly PlayerEntity _player;
        private readonly MapManager _mapManager;
        private readonly MapViewPresenter _mapView;
        private readonly LogSystem _logSystem;
        private readonly InventoryView? _inventoryView;
        private readonly ContextMenuView? _contextMenuView;
        private readonly ThrowTargetingController? _throwTargetingController;
        private readonly TileContextMenuController? _tileContextMenuController;
        private readonly Transform _playerTransform;
        private readonly float _tileSize;
        private readonly System.Action<ProjectileResult, Vector2> _playProjectileAnimation;
        private readonly GameController _gameController;

        // 足元の拾えるアイテムのガイドを1回だけ出すためのフラグ。
        private GridPosition? _lastPickupGuidePosition;

        public PlayerInputController(
            PlayerEntity player,
            MapManager mapManager,
            MapViewPresenter mapView,
            LogSystem logSystem,
            InventoryView? inventoryView,
            ContextMenuView? contextMenuView,
            ThrowTargetingController? throwTargetingController,
            TileContextMenuController? tileContextMenuController,
            Transform playerTransform,
            float tileSize,
            System.Action<ProjectileResult, Vector2> playProjectileAnimation,
            GameController gameController)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));
            if (mapManager == null) throw new ArgumentNullException(nameof(mapManager));
            if (mapView == null) throw new ArgumentNullException(nameof(mapView));
            if (logSystem == null) throw new ArgumentNullException(nameof(logSystem));
            if (playerTransform == null) throw new ArgumentNullException(nameof(playerTransform));
            if (playProjectileAnimation == null) throw new ArgumentNullException(nameof(playProjectileAnimation));
            if (gameController == null) throw new ArgumentNullException(nameof(gameController));

            _player = player;
            _mapManager = mapManager;
            _mapView = mapView;
            _logSystem = logSystem;
            _inventoryView = inventoryView;
            _contextMenuView = contextMenuView;
            _throwTargetingController = throwTargetingController;
            _tileContextMenuController = tileContextMenuController;
            _playerTransform = playerTransform;
            _tileSize = tileSize;
            _playProjectileAnimation = playProjectileAnimation;
            _gameController = gameController;

            InitializeContainerTransferIfAvailable();
        }

        public void Tick(Keyboard? keyboard, Mouse? mouse, float deltaTime)
        {
            _ = deltaTime;

            if (keyboard == null && mouse == null)
            {
                return;
            }

            var globalUiIntent = BuildGlobalUiIntent(keyboard);
            if (globalUiIntent.HasValue)
            {
                Execute(globalUiIntent.Value);
            }

            var inventoryVisible = IsInventoryVisible();

            if (IsContextMenuVisible())
            {
                var intent = BuildContextMenuIntent(keyboard, mouse, inventoryVisible);
                if (intent.HasValue)
                {
                    Execute(intent.Value);
                }
                return;
            }

            if (inventoryVisible)
            {
                var intent = BuildInventoryIntent(keyboard);
                if (intent.HasValue)
                {
                    Execute(intent.Value);
                }
                return;
            }

            if (IsThrowTargetingMode())
            {
                Execute(Intent.ThrowTargetingTick(keyboard, mouse));
                return;
            }

            var normalIntent = BuildNormalIntent(keyboard, mouse);
            if (normalIntent.HasValue)
            {
                Execute(normalIntent.Value);
            }
        }

        private bool IsInventoryVisible()
        {
            return _inventoryView != null && _inventoryView.IsVisible;
        }

        private bool IsContextMenuVisible()
        {
            return _contextMenuView != null && _contextMenuView.IsVisible;
        }

        private bool IsThrowTargetingMode()
        {
            return _throwTargetingController != null && _throwTargetingController.IsTargeting;
        }
    }
}
