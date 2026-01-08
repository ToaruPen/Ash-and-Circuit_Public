using System.Collections.Generic;
using AshNCircuit.Core.Items;
using AshNCircuit.Core.Systems;
using AshNCircuit.UnityIntegration.Audio;
using AshNCircuit.UnityIntegration.UI.ContextMenuUi;
using UnityEngine;

namespace AshNCircuit.UnityIntegration.UI.InventoryUi
{
    public sealed partial class InventoryView
    {
        private void ShowItemContextMenu(ItemDefinition item, Vector2 position)
        {
            _tooltipView?.Hide();

            if (_contextMenuView == null || item == null)
            {
                return;
            }

            var menuItems = BuildItemContextMenuItems(item);
            _contextMenuView.Show(position, menuItems);
        }

        private List<ContextMenuItem> BuildItemContextMenuItems(ItemDefinition item)
        {
            var menuItems = new List<ContextMenuItem>();

            if (IsContainerOpen)
            {
                menuItems.Add(new ContextMenuItem(
                    "しまう",
                    () =>
                    {
                        OnStoreToContainerRequested?.Invoke(item);
                    }
                ));
            }

            if (HasTag(item, "throwable") || HasTag(item, "projectile"))
            {
                menuItems.Add(new ContextMenuItem(
                    BuildMenuLabel(MessageId.UiInventoryMenuThrow),
                    () =>
                    {
                        SetVisible(false);
                        OnThrowRequested?.Invoke(item);
                    }
                ));
            }

            var equippableSlot = EquipmentSlotUtility.GetEquippableSlot(item);
            if (equippableSlot.HasValue)
            {
                var slotName = EquipmentSlotUtility.GetDisplayName(equippableSlot.Value);
                menuItems.Add(new ContextMenuItem(
                    BuildMenuLabel(MessageId.UiInventoryMenuEquipWithSlot, slotName),
                    () =>
                    {
                        _inventory?.TryEquip(item);
                        Refresh();
                    }
                ));
            }

            menuItems.Add(new ContextMenuItem(
                BuildMenuLabel(MessageId.UiInventoryMenuDrop),
                () =>
                {
                    _sfxPlayer?.PlayOneShot(SfxIds.UiDrop);
                    OnDropRequested?.Invoke(item);
                }
            ));

            menuItems.Add(new ContextMenuItem(
                BuildMenuLabel(MessageId.UiInventoryMenuExamine),
                () =>
                {
                    OnExamineRequested?.Invoke(item);
                }
            ));

            return menuItems;
        }

        private void ShowEquipmentSlotContextMenu(EquipmentSlot slot, Vector2 position)
        {
            _tooltipView?.Hide();

            if (_contextMenuView == null || _inventory == null)
            {
                return;
            }

            var equipped = _inventory.GetEquipped(slot);
            if (equipped == null)
            {
                return;
            }

            var menuItems = BuildEquipmentSlotContextMenuItems(slot, equipped);
            _contextMenuView.Show(position, menuItems);
        }

        private List<ContextMenuItem> BuildEquipmentSlotContextMenuItems(EquipmentSlot slot, ItemDefinition equipped)
        {
            return new List<ContextMenuItem>
            {
                new ContextMenuItem(
                    BuildMenuLabel(MessageId.UiInventoryMenuUnequip),
                    () =>
                    {
                        _inventory.TryUnequip(slot);
                        Refresh();
                    }
                ),
                new ContextMenuItem(
                    BuildMenuLabel(MessageId.UiInventoryMenuExamine),
                    () =>
                    {
                        OnExamineRequested?.Invoke(equipped);
                    }
                )
            };
        }

        private static string BuildMenuLabel(MessageId id, params object[] args)
        {
            return MessageCatalog.Format(id, args);
        }
    }
}
