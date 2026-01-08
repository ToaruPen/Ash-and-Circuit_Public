using AshNCircuit.Core.Items;
using AshNCircuit.Core.Systems;
using AshNCircuit.UnityIntegration.UI;

namespace AshNCircuit.UnityIntegration.MapView
{
    public partial class GameRoot
    {
        private void InitializeUi()
        {
            var uiPresenter = new GameUiPresenter(
                hudDocument,
                logDocument,
                inventoryDocument,
                contextMenuDocument,
                tooltipDocument,
                _playerEntity!,
                _logSystem,
                _turnManager);

            _hudView = uiPresenter.HudView;
            _logView = uiPresenter.LogView;
            _inventoryView = uiPresenter.InventoryView;
            _contextMenuView = uiPresenter.ContextMenuView;
            _tooltipView = uiPresenter.TooltipView;

            if (_inventoryView != null)
            {
                _inventoryView.OnThrowRequested += HandleThrowRequested;
                _inventoryView.OnDropRequested += HandleDropRequested;
                _inventoryView.OnExamineRequested += HandleExamineRequested;
            }
        }

        private void HandleThrowRequested(ItemDefinition item)
        {
            _throwTargetingController?.BeginTargeting(item);
        }

        private void HandleDropRequested(ItemDefinition item)
        {
            if (item == null || _gameController == null)
            {
                return;
            }

            _gameController.QueueDropItem(item);
            _gameController.AdvanceTurn();
            _inventoryView?.Refresh();
        }

        private void HandleExamineRequested(ItemDefinition item)
        {
            if (item == null)
            {
                return;
            }

            _logSystem.LogById(MessageId.UiItemExamined, item.DisplayName, item.Description);
        }
    }
}
