using AshNCircuit.Core.Map;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AshNCircuit.UnityIntegration.MapView
{
    public sealed partial class PlayerInputController
    {
        private Vector3 GetMouseWorldPosition(Mouse mouse)
        {
            var mousePos = mouse.position.ReadValue();
            var camera = Camera.main;
            if (camera == null)
            {
                return Vector3.zero;
            }

            var worldPos = camera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, -camera.transform.position.z));
            return worldPos;
        }

        private GridPosition WorldToGrid(Vector3 worldPos)
        {
            var gridX = Mathf.RoundToInt(worldPos.x / _tileSize);
            var gridY = Mathf.RoundToInt(worldPos.y / _tileSize);
            return new GridPosition(gridX, gridY);
        }
    }
}

