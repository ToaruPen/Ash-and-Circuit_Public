using System.Collections.Generic;
using AshNCircuit.Core.Items;
using AshNCircuit.Core.Map;
using AshNCircuit.Core.Systems;
using AshNCircuit.UnityIntegration.Audio;
using AshNCircuit.UnityIntegration.Content;
using AshNCircuit.UnityIntegration.UI.ContextMenuUi;
using AshNCircuit.UnityIntegration.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace AshNCircuit.UnityIntegration.UI.InventoryUi
{
    public sealed partial class InventoryView
    {
        private VisualElement? _containerArea;
        private Label? _containerTitleLabel;
        private ScrollView? _containerItemsScrollView;

        private readonly List<VisualElement> _containerItemRows = new List<VisualElement>();
        private GridPosition? _openContainerTile;
        private PropInstance? _openContainer;

        public bool IsContainerOpen => _openContainerTile.HasValue && _openContainer != null;

        public bool TryGetOpenContainer(out GridPosition tile)
        {
            if (!_openContainerTile.HasValue || _openContainer == null)
            {
                tile = default;
                return false;
            }

            tile = _openContainerTile.Value;
            return true;
        }

        public void OpenContainer(PropInstance container, GridPosition tile)
        {
            if (container == null || !container.IsContainer)
            {
                return;
            }

            EnsureContainerUiCreated();
            if (_containerArea == null || _containerTitleLabel == null)
            {
                return;
            }

            _openContainer = container;
            _openContainerTile = tile;
            _containerTitleLabel.text = container.DisplayName;

            if (_equipmentArea != null)
            {
                _equipmentArea.style.display = DisplayStyle.None;
            }
            _containerArea.style.display = DisplayStyle.Flex;

            SetVisible(true);
            Refresh();
        }

        public void CloseContainer()
        {
            _openContainer = null;
            _openContainerTile = null;

            if (_containerArea != null)
            {
                _containerArea.style.display = DisplayStyle.None;
            }

            if (_equipmentArea != null)
            {
                _equipmentArea.style.display = DisplayStyle.Flex;
            }
        }

        private void EnsureContainerUiCreated()
        {
            if (_containerArea != null || _root == null)
            {
                return;
            }

            var contentRoot = _root.Q<VisualElement>("inventory-content");
            if (contentRoot == null)
            {
                return;
            }

            var area = new VisualElement
            {
                name = "inventory-container"
            };
            area.AddToClassList("inventory-equipment");

            var title = new Label("Container")
            {
                name = "inventory-container-title"
            };
            title.AddToClassList("inventory-title");

            var scrollView = new ScrollView
            {
                name = "inventory-container-items"
            };
            scrollView.AddToClassList("inventory-items");

            area.Add(title);
            area.Add(scrollView);

            area.style.display = DisplayStyle.None;

            contentRoot.Add(area);

            _containerArea = area;
            _containerTitleLabel = title;
            _containerItemsScrollView = scrollView;
        }

        private void RefreshContainerList()
        {
            if (!IsContainerOpen || _containerItemsScrollView == null || _openContainer == null)
            {
                return;
            }

            foreach (var row in _containerItemRows)
            {
                _containerItemsScrollView.Remove(row);
            }
            _containerItemRows.Clear();

            var pile = _openContainer.ContainerItems;
            if (pile == null || pile.IsEmpty)
            {
                var empty = new Label("(empty)");
                _containerItemsScrollView.Add(empty);
                _containerItemRows.Add(empty);
                return;
            }

            var totals = new Dictionary<ItemDefinition, int>();
            var orderedItems = new List<ItemDefinition>();

            foreach (var entry in pile.Entries)
            {
                if (entry.Amount <= 0)
                {
                    continue;
                }

                if (!totals.ContainsKey(entry.Item))
                {
                    totals[entry.Item] = 0;
                    orderedItems.Add(entry.Item);
                }

                totals[entry.Item] += entry.Amount;
            }

            foreach (var item in orderedItems)
            {
                var total = totals[item];
                var row = BuildContainerItemRow(item, total);
                _containerItemsScrollView.Add(row);
                _containerItemRows.Add(row);
            }
        }

        private VisualElement BuildContainerItemRow(ItemDefinition item, int amount)
        {
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

            var labelText = amount > 1
                ? $"{item.DisplayName} x{amount}"
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
                    ShowContainerItemContextMenu(item, screenPos);
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

        private void ShowContainerItemContextMenu(ItemDefinition item, Vector2 position)
        {
            _tooltipView?.Hide();

            if (_contextMenuView == null || item == null)
            {
                return;
            }

            var menuItems = new List<ContextMenuItem>
            {
                new ContextMenuItem(
                    "取り出す",
                    () =>
                    {
                        OnTakeFromContainerRequested?.Invoke(item);
                    })
            };

            menuItems.Add(new ContextMenuItem(
                MessageCatalog.Format(MessageId.UiInventoryMenuExamine),
                () =>
                {
                    OnExamineRequested?.Invoke(item);
                }));

            _contextMenuView.Show(position, menuItems);
        }
    }
}
