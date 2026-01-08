using AshNCircuit.Core.Entities;
using AshNCircuit.Core.Systems;
using AshNCircuit.Core.GameLoop;
using AshNCircuit.UnityIntegration.UI.ContextMenuUi;
using AshNCircuit.UnityIntegration.UI.Hud;
using AshNCircuit.UnityIntegration.UI.InventoryUi;
using AshNCircuit.UnityIntegration.UI.Log;
using AshNCircuit.UnityIntegration.UI.TooltipUi;
using UnityEngine;
using UnityEngine.UIElements;

namespace AshNCircuit.UnityIntegration.UI
{
    /// <summary>
    /// Game シーンにおける HUD / ログ / インベントリ / コンテキストメニュー /
    /// ツールチップの初期化と相互接続を担当するプレゼンター。
    /// GameRoot からはこのクラス経由で UI を扱う。
    /// </summary>
    public sealed class GameUiPresenter
    {
        public HudView? HudView { get; private set; }
        public LogView? LogView { get; private set; }
        public InventoryView? InventoryView { get; private set; }
        public ContextMenuView? ContextMenuView { get; private set; }
        public TooltipView? TooltipView { get; private set; }

        public GameUiPresenter(
            UIDocument hudDocument,
            UIDocument logDocument,
            UIDocument inventoryDocument,
            UIDocument contextMenuDocument,
            UIDocument tooltipDocument,
            PlayerEntity playerEntity,
            LogSystem logSystem,
            TurnManager turnManager)
        {
            // すべての UI を同一 PanelSettings 上に載せることで、座標系を揃える。
            if (inventoryDocument != null)
            {
                if (contextMenuDocument != null && contextMenuDocument.panelSettings != inventoryDocument.panelSettings)
                {
                    contextMenuDocument.panelSettings = inventoryDocument.panelSettings;
                }

                if (tooltipDocument != null && tooltipDocument.panelSettings != inventoryDocument.panelSettings)
                {
                    tooltipDocument.panelSettings = inventoryDocument.panelSettings;
                }

                if (hudDocument != null && hudDocument.panelSettings != inventoryDocument.panelSettings)
                {
                    hudDocument.panelSettings = inventoryDocument.panelSettings;
                }

                if (logDocument != null && logDocument.panelSettings != inventoryDocument.panelSettings)
                {
                    logDocument.panelSettings = inventoryDocument.panelSettings;
                }
            }

            if (hudDocument != null && playerEntity != null)
            {
                HudView = new HudView(hudDocument, playerEntity, turnManager);
                HudView.Refresh();
            }

            if (logDocument != null && logSystem != null)
            {
                LogView = new LogView(logDocument, logSystem);
            }

            if (contextMenuDocument != null)
            {
                // コンテキストメニューは前面に表示する
                contextMenuDocument.sortingOrder = 900;
                ContextMenuView = new ContextMenuView(contextMenuDocument);
            }

            if (tooltipDocument != null)
            {
                // ツールチップは常に最前面に表示する
                tooltipDocument.sortingOrder = 1000;
                TooltipView = new TooltipView(tooltipDocument);
            }

            if (inventoryDocument != null && playerEntity != null)
            {
                InventoryView = new InventoryView(inventoryDocument, playerEntity.Inventory);

                // コンテキストメニューとツールチップを設定
                if (ContextMenuView != null)
                {
                    InventoryView.SetContextMenu(ContextMenuView);
                }

                if (TooltipView != null)
                {
                    InventoryView.SetTooltip(TooltipView);
                }
            }
        }

        /// <summary>
        /// インベントリの表示状態をトグルし、必要に応じて内容を更新する。
        /// </summary>
        public void ToggleInventory()
        {
            if (InventoryView == null)
            {
                return;
            }

            var nextVisible = !InventoryView.IsVisible;
            InventoryView.SetVisible(nextVisible);
            if (nextVisible)
            {
                InventoryView.Refresh();
            }
        }
    }
}
