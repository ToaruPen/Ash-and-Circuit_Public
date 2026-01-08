using System;
using System.Collections;
using System.Collections.Generic;
using AshNCircuit.Core.Entities;
using AshNCircuit.Core.Items;
using AshNCircuit.Core.Map;
using AshNCircuit.Core.Systems;
using AshNCircuit.Core.GameLoop;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using AshNCircuit.UnityIntegration.UI;
using AshNCircuit.UnityIntegration.UI.Hud;
using AshNCircuit.UnityIntegration.UI.Log;
using AshNCircuit.UnityIntegration.UI.InventoryUi;
using AshNCircuit.UnityIntegration.UI.ContextMenuUi;
using AshNCircuit.UnityIntegration.UI.TooltipUi;

namespace AshNCircuit.UnityIntegration.MapView
{
    /// <summary>
    /// Game シーンにおける Core ロジックと Unity オブジェクトの橋渡しを行うエントリポイント。
    /// - MapManager / ActionSystem / EffectSystem / LogSystem の生成と保持
    /// - マップタイル表示用の MapViewPresenter と ProjectileViewPresenter の初期化
    /// - HUD / ログ / インベントリ / コンテキストメニュー / ツールチップの UI 初期化
    /// - PlayerInputController / ThrowTargetingController / TileContextMenuController など UnityIntegration コントローラの配線
    /// - プレイヤーエンティティと Transform の初期同期
    /// ※ゲームロジックや入力分岐を直接ここに追加するのではなく、基本的には専用コントローラ（PlayerInputController 等）側で拡張することを強く推奨する。
    /// </summary>
    [DefaultExecutionOrder(100)]
    public partial class GameRoot : MonoBehaviour
    {
        [Header("Player")]
        [SerializeField]
        private Transform playerTransform = null!;

        [Header("Map Settings")]
        [SerializeField]
        private int mapWidth = 32;

        [SerializeField]
        private int mapHeight = 32;

        [SerializeField]
        private float tileSize = 1.0f;

        [Header("Discovery")]
        [SerializeField]
        private int discoverySeed = 12345;

        [SerializeField]
        private bool logDiscoveryGenerationSummary = true;

        [Header("Map View")]
        [SerializeField]
        private Transform tilesParent = null!;

        [SerializeField]
        private TileSpriteMapping[] tileSpriteMappings = Array.Empty<TileSpriteMapping>();

        [SerializeField]
        private PropSpriteMapping[] propSpriteMappings = Array.Empty<PropSpriteMapping>();

        [SerializeField]
        private Sprite itemPileSprite = null!;

        [Header("Projectiles")]
        [SerializeField]
        private Transform projectilesParent = null!;

        [SerializeField]
        private Sprite arrowSprite = null!;

        [Header("UI")]
        [SerializeField]
        private UIDocument hudDocument = null!;

        [SerializeField]
        private UIDocument logDocument = null!;

        [SerializeField]
        private UIDocument inventoryDocument = null!;

        [SerializeField]
        private UIDocument contextMenuDocument = null!;

        [SerializeField]
        private UIDocument tooltipDocument = null!;

        private GameController _gameController = null!;
        private TurnManager _turnManager = null!;
        private MapManager _mapManager = null!;
        private ActionSystem _actionSystem = null!;
        private MapViewPresenter _mapView = null!;
        private ProjectileViewPresenter? _projectileView;
        private LogSystem _logSystem = null!;
        private EffectSystem _effectSystem = null!;
        private PlayerEntity? _playerEntity;
        private EnemyAISystem? _enemyAiSystem;
        private EnemyViewPresenter? _enemyViewPresenter;

        private HudView? _hudView;
        private LogView? _logView;
        private InventoryView? _inventoryView;
        private ContextMenuView? _contextMenuView;
        private TooltipView? _tooltipView;
        private PlayerInputController? _playerInputController;

        // ターゲット指定モード（投擲用）
        private ThrowTargetingController? _throwTargetingController;
        private TileContextMenuController? _tileContextMenuController;

        /// <summary>
        /// マップ上の各タイルに対応する SpriteRenderer の参照。
        /// タイル状態の変化に応じてスプライトを差し替えるために使用する。
        /// </summary>
        // 矢スプライト用の描画レイヤー設定（プレイヤーのSpriteRendererから継承する）。
        private string _projectileSortingLayerName = "";
        private int _projectileSortingOrder = 0;

    }
}
