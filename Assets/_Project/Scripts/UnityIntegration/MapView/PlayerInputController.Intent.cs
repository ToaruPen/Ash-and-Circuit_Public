using AshNCircuit.Core.Map;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AshNCircuit.UnityIntegration.MapView
{
    public sealed partial class PlayerInputController
    {
        private enum IntentKind
        {
            ToggleInventory,
            CloseContextMenu,
            ShowTileContextMenu,
            PickupAtFeet,
            ShootAtTile,
            ShootDirectional,
            MoveDirectional,
            ThrowTargetingTick,
        }

        private readonly struct Intent
        {
            public IntentKind Kind { get; }
            public int DeltaX { get; }
            public int DeltaY { get; }
            public GridPosition TargetTile { get; }
            public Vector2 ScreenPosition { get; }
            public Keyboard? Keyboard { get; }
            public Mouse? Mouse { get; }

            private Intent(
                IntentKind kind,
                int deltaX = 0,
                int deltaY = 0,
                GridPosition targetTile = default,
                Vector2 screenPosition = default,
                Keyboard? keyboard = null,
                Mouse? mouse = null)
            {
                Kind = kind;
                DeltaX = deltaX;
                DeltaY = deltaY;
                TargetTile = targetTile;
                ScreenPosition = screenPosition;
                Keyboard = keyboard;
                Mouse = mouse;
            }

            public static Intent ToggleInventory()
            {
                return new Intent(IntentKind.ToggleInventory);
            }

            public static Intent CloseContextMenu()
            {
                return new Intent(IntentKind.CloseContextMenu);
            }

            public static Intent ShowTileContextMenu(GridPosition targetTile, Vector2 screenPosition)
            {
                return new Intent(IntentKind.ShowTileContextMenu, targetTile: targetTile, screenPosition: screenPosition);
            }

            public static Intent PickupAtFeet()
            {
                return new Intent(IntentKind.PickupAtFeet);
            }

            public static Intent ShootAtTile(GridPosition targetTile)
            {
                return new Intent(IntentKind.ShootAtTile, targetTile: targetTile);
            }

            public static Intent ShootDirectional(int deltaX, int deltaY)
            {
                return new Intent(IntentKind.ShootDirectional, deltaX: deltaX, deltaY: deltaY);
            }

            public static Intent MoveDirectional(int deltaX, int deltaY)
            {
                return new Intent(IntentKind.MoveDirectional, deltaX: deltaX, deltaY: deltaY);
            }

            public static Intent ThrowTargetingTick(Keyboard? keyboard, Mouse? mouse)
            {
                return new Intent(IntentKind.ThrowTargetingTick, keyboard: keyboard, mouse: mouse);
            }
        }
    }
}

