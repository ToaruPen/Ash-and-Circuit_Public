using AshNCircuit.Core.Items;
using UnityEngine;

namespace AshNCircuit.UnityIntegration.UI.InventoryUi
{
    public sealed partial class InventoryView
    {
        private void ShowTooltipForItem(ItemDefinition item, Vector2 screenPosition)
        {
            if (_tooltipView == null || item == null)
            {
                return;
            }

            var tags = item.Tags != null ? string.Join(", ", item.Tags) : string.Empty;
            _tooltipView.Show(screenPosition, item.DisplayName, $"[{tags}]", item.Description);
        }
    }
}

