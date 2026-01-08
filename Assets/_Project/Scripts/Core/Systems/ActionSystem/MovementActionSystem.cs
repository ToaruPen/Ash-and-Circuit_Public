using AshNCircuit.Core.Entities;
using AshNCircuit.Core.Map;

namespace AshNCircuit.Core.Systems
{
    /// <summary>
    /// 移動（Movement）に関わるアクションを扱うサブシステム。
    /// ActionSystem から委譲され、public API/挙動を維持するための分割ステップとして導入する。
    /// </summary>
    internal sealed class MovementActionSystem
    {
        public bool TryMovePlayerTo(PlayerEntity player, int targetX, int targetY, MapManager mapManager, LogSystem logSystem)
        {
            if (player == null || player.IsDead || mapManager == null || logSystem == null)
            {
                return false;
            }

            // まずマップ範囲外かどうかを確認する。
            if (!mapManager.IsInBounds(targetX, targetY))
            {
                logSystem.LogById(MessageId.MoveOutOfBounds, player.X, player.Y, targetX, targetY);
                return false;
            }

            // 範囲内だが通行不可（壁など）の場合。
            if (!mapManager.IsWalkable(targetX, targetY))
            {
                logSystem.LogById(MessageId.MoveBlockedByWall, player.X, player.Y, targetX, targetY);
                return false;
            }

            player.SetPosition(targetX, targetY);
            logSystem.LogById(MessageId.MoveSucceeded, player.X, player.Y);
            return true;
        }
    }
}
