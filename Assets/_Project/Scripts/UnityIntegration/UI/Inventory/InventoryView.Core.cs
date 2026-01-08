using System.Collections.Generic;
using AshNCircuit.Core.Items;
using AshNCircuit.UnityIntegration.Audio;
using AshNCircuit.UnityIntegration.UI;
using AshNCircuit.UnityIntegration.UI.ContextMenuUi;
using AshNCircuit.UnityIntegration.UI.TooltipUi;
using UnityEngine;
using UnityEngine.UIElements;

namespace AshNCircuit.UnityIntegration.UI.InventoryUi
{
    public sealed partial class InventoryView
    {
        private readonly Inventory _inventory;
        private readonly VisualElement _root = null!;
        private readonly ScrollView _itemsScrollView = null!;
        private readonly VisualElement _equipmentArea = null!;

        private readonly Dictionary<EquipmentSlot, Label> _equipmentLabels = new Dictionary<EquipmentSlot, Label>();
        private readonly Dictionary<EquipmentSlot, VisualElement> _equipmentSlotElements = new Dictionary<EquipmentSlot, VisualElement>();

        private readonly List<VisualElement> _itemRows = new List<VisualElement>();

        private ContextMenuView? _contextMenuView;
        private TooltipView? _tooltipView;
        private SfxPlayer? _sfxPlayer;

        public bool IsVisible { get; private set; }

        public InventoryView(UIDocument document, Inventory inventory)
        {
            _inventory = inventory;

            if (document == null)
            {
                Debug.LogWarning("InventoryView: UIDocument が未割り当てです。");
                return;
            }

            _root = document.rootVisualElement;
            if (_root == null)
            {
                Debug.LogWarning("InventoryView: rootVisualElement が取得できません。");
                return;
            }

            _itemsScrollView = _root.Q<ScrollView>("inventory-items");
            if (_itemsScrollView == null)
            {
                Debug.LogWarning("InventoryView: ScrollView 'inventory-items' が見つかりません。");
            }

            _equipmentArea = _root.Q<VisualElement>("inventory-equipment");
            if (_equipmentArea == null)
            {
                Debug.LogWarning("InventoryView: VisualElement 'inventory-equipment' が見つかりません。");
            }

            CacheEquipmentSlotReferences();

            SetVisible(false);
            Refresh();
        }

        public void SetContextMenu(ContextMenuView contextMenuView)
        {
            _contextMenuView = contextMenuView;
        }

        public void SetSfxPlayer(SfxPlayer sfxPlayer)
        {
            _sfxPlayer = sfxPlayer;
        }

        public void SetTooltip(TooltipView tooltipView)
        {
            _tooltipView = tooltipView;
        }

        public void SetVisible(bool visible)
        {
            var wasVisible = IsVisible;
            IsVisible = visible;

            if (_root == null)
            {
                return;
            }

            if (!visible)
            {
                CloseContainer();
            }

            _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

            if (visible != wasVisible)
            {
                if (visible)
                {
                    _sfxPlayer?.PlayOneShot(SfxIds.UiInventoryOpen);
                }
                else
                {
                    _sfxPlayer?.PlayOneShot(SfxIds.UiInventoryClose);
                }
            }
        }

        public void Refresh()
        {
            RefreshItemList();
            RefreshEquipmentSlots();
            RefreshContainerList();
        }

        private void CacheEquipmentSlotReferences()
        {
            if (_root == null)
            {
                return;
            }

            var slotMappings = new[]
            {
                (EquipmentSlot.Head, "equipment-slot-head", "equipment-slot-label-head"),
                (EquipmentSlot.Body, "equipment-slot-body", "equipment-slot-label-body"),
                (EquipmentSlot.MainHand, "equipment-slot-mainhand", "equipment-slot-label-mainhand"),
                (EquipmentSlot.OffHand, "equipment-slot-offhand", "equipment-slot-label-offhand"),
                (EquipmentSlot.Back, "equipment-slot-back", "equipment-slot-label-back"),
                (EquipmentSlot.Feet, "equipment-slot-feet", "equipment-slot-label-feet")
            };

            foreach (var (slot, elementName, labelName) in slotMappings)
            {
                var element = _root.Q<VisualElement>(elementName);
                var label = _root.Q<Label>(labelName);

                if (element != null)
                {
                    _equipmentSlotElements[slot] = element;

                    element.RegisterCallback<PointerDownEvent>(evt =>
                    {
                        if (evt.button == 1)
                        {
                            var screenPos = UiToolkitPositioningUtility.GetMouseScreenPosition();
                            ShowEquipmentSlotContextMenu(slot, screenPos);
                            evt.StopPropagation();
                        }
                    });

                    element.RegisterCallback<PointerEnterEvent>(_ =>
                    {
                        element.AddToClassList("equipment-slot--hover");
                    });

                    element.RegisterCallback<PointerLeaveEvent>(_ =>
                    {
                        element.RemoveFromClassList("equipment-slot--hover");
                    });
                }

                if (label != null)
                {
                    _equipmentLabels[slot] = label;
                }
            }
        }

        private static bool HasTag(ItemDefinition item, string tag)
        {
            if (item?.Tags == null)
            {
                return false;
            }

            foreach (var t in item.Tags)
            {
                if (t == tag)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
