using AshNCircuit.Core.Map;
using AshNCircuit.UnityIntegration.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AshNCircuit.UnityIntegration.MapView
{
    public sealed partial class PlayerInputController
    {
        private Intent? BuildGlobalUiIntent(Keyboard? keyboard)
        {
            // インベントリ開閉はゲームループには影響しない UI 操作として扱う（従来どおり）。
            return keyboard != null && keyboard.iKey.wasPressedThisFrame ? Intent.ToggleInventory() : null;
        }

        private Intent? BuildContextMenuIntent(Keyboard? keyboard, Mouse? mouse, bool inventoryVisible)
        {
            if (_contextMenuView == null)
            {
                return null;
            }

            // Esc でメニューを閉じる。
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                return Intent.CloseContextMenu();
            }

            // インベントリが閉じているときのみ、右クリックでタイル用メニューを別位置に更新する。
            if (!inventoryVisible && mouse != null && mouse.rightButton.wasPressedThisFrame)
            {
                var targetTile = WorldToGrid(GetMouseWorldPosition(mouse));
                var screenPos = UiToolkitPositioningUtility.GetMouseScreenPosition();
                return Intent.ShowTileContextMenu(targetTile, screenPos);
            }

            return null;
        }

        private Intent? BuildInventoryIntent(Keyboard? keyboard)
        {
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                return Intent.ToggleInventory();
            }

            return null;
        }

        private Intent? BuildNormalIntent(Keyboard? keyboard, Mouse? mouse)
        {
            // 右クリックでコンテキストメニューを表示する。
            if (mouse != null && mouse.rightButton.wasPressedThisFrame)
            {
                var targetTile = WorldToGrid(GetMouseWorldPosition(mouse));
                var screenPos = UiToolkitPositioningUtility.GetMouseScreenPosition();
                return Intent.ShowTileContextMenu(targetTile, screenPos);
            }

            // Gキーで足元のアイテムを拾う。
            if (keyboard != null && keyboard.gKey.wasPressedThisFrame)
            {
                return Intent.PickupAtFeet();
            }

            // マウス左クリックで任意方向射撃。
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                var worldPos = GetMouseWorldPosition(mouse);
                var targetTile = WorldToGrid(worldPos);

                return Intent.ShootAtTile(targetTile);
            }

            return TryBuildDirectionalKeyboardIntent(keyboard, out var intent) ? intent : null;
        }

        private bool TryBuildDirectionalKeyboardIntent(Keyboard? keyboard, out Intent intent)
        {
            intent = default;
            if (keyboard == null)
            {
                return false;
            }

            int deltaX = 0;
            int deltaY = 0;

            if (keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame)
            {
                deltaY = 1;
            }
            else if (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame)
            {
                deltaY = -1;
            }
            else if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
            {
                deltaX = -1;
            }
            else if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
            {
                deltaX = 1;
            }

            if (deltaX == 0 && deltaY == 0)
            {
                return false;
            }

            // スペースキーが押されている間は射撃モードとして扱う（従来どおり4方向射撃）。
            if (keyboard.spaceKey.isPressed)
            {
                intent = Intent.ShootDirectional(deltaX, deltaY);
            }
            else
            {
                intent = Intent.MoveDirectional(deltaX, deltaY);
            }

            return true;
        }
    }
}

