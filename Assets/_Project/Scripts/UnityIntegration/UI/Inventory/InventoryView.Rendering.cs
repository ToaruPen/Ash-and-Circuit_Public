using AshNCircuit.Core.Items;
using AshNCircuit.UnityIntegration.Audio;
using AshNCircuit.UnityIntegration.Content;
using AshNCircuit.UnityIntegration.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace AshNCircuit.UnityIntegration.UI.InventoryUi
{
    public sealed partial class InventoryView
    {
        private void RefreshItemList()
        {
            if (_itemsScrollView == null)
            {
                return;
            }

            foreach (var row in _itemRows)
            {
                _itemsScrollView.Remove(row);
            }
            _itemRows.Clear();

            if (_inventory == null)
            {
                return;
            }

            foreach (var entry in _inventory.Entries)
            {
                var row = BuildItemRow(entry);
                _itemsScrollView.Add(row);
                _itemRows.Add(row);
            }
        }

        private void RefreshEquipmentSlots()
        {
            if (_inventory == null)
            {
                return;
            }

            foreach (var (slot, item) in _inventory.GetAllEquipment())
            {
                if (!_equipmentLabels.TryGetValue(slot, out var label))
                {
                    continue;
                }

                var slotName = EquipmentSlotUtility.GetDisplayName(slot);
                if (item != null)
                {
                    label.text = $"{slotName}: {item.DisplayName}";

                    if (_equipmentSlotElements.TryGetValue(slot, out var element))
                    {
                        element.AddToClassList("equipment-slot--equipped");
                    }
                }
                else
                {
                    label.text = $"{slotName}: -";

                    if (_equipmentSlotElements.TryGetValue(slot, out var element))
                    {
                        element.RemoveFromClassList("equipment-slot--equipped");
                    }
                }
            }
        }

        private VisualElement BuildItemRow(InventoryEntry entry)
        {
            var item = entry.Item;

            var row = new VisualElement
            {
                name = "inventory-item-row"
            };
            row.AddToClassList("inventory-item-row");

            var icon = new VisualElement
            {
                name = "inventory-item-icon"
            };
            icon.AddToClassList("inventory-item-icon");

            var sprite = SpriteCatalog.LoadFromResources().GetSpriteOrThrow(item.SpriteId);
            icon.style.width = 16;
            icon.style.height = 24;
            icon.style.backgroundImage = new StyleBackground(sprite);

            var labelText = entry.Amount > 1
                ? $"{item.DisplayName} x{entry.Amount}"
                : item.DisplayName;

            var label = new Label(labelText)
            {
                name = "inventory-item-label"
            };
            label.AddToClassList("inventory-item-label");

            row.Add(icon);
            row.Add(label);

            row.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button == 1)
                {
                    var screenPos = UiToolkitPositioningUtility.GetMouseScreenPosition();
                    ShowItemContextMenu(item, screenPos);
                    evt.StopPropagation();
                }
            });

            row.RegisterCallback<PointerEnterEvent>(_ =>
            {
                if (_contextMenuView != null && _contextMenuView.IsVisible)
                {
                    return;
                }

                var screenPos = UiToolkitPositioningUtility.GetMouseScreenPosition();
                ShowTooltipForItem(item, screenPos);
                _sfxPlayer?.PlayOneShot(SfxIds.UiItemHover);
                row.AddToClassList("inventory-item-row--hover");
            });

            row.RegisterCallback<PointerLeaveEvent>(_ =>
            {
                _tooltipView?.Hide();
                row.RemoveFromClassList("inventory-item-row--hover");
            });

            return row;
        }
    }
}
