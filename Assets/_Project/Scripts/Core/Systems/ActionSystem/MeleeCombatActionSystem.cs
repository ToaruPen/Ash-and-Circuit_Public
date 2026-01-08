using System;
using AshNCircuit.Core.Entities;

namespace AshNCircuit.Core.Systems
{
    /// <summary>
    /// 近接攻撃（Melee）に関わるアクションを扱うサブシステム。
    /// ActionSystem から委譲され、public API/挙動を維持するための分割ステップとして導入する。
    /// </summary>
    internal sealed class MeleeCombatActionSystem
    {
        public bool TryMeleeAttack(Entity attacker, Entity target, LogSystem logSystem)
        {
            if (attacker == null || target == null || attacker.IsDead || target.IsDead || logSystem == null)
            {
                return false;
            }

            // 射程 1（上下左右のみ）。
            var dx = Math.Abs(attacker.X - target.X);
            var dy = Math.Abs(attacker.Y - target.Y);
            if (dx + dy != 1)
            {
                return false;
            }

            var damage = target.ApplyDamageFrom(attacker);
            if (damage <= 0)
            {
                return false;
            }

            // プレイヤーが攻撃したかどうかでログ内容を出し分ける。
            if (attacker is PlayerEntity && target is EnemyEntity)
            {
                logSystem.LogById(MessageId.MeleePlayerHitEnemyDamage, damage);
                if (target.IsDead)
                {
                    logSystem.LogById(MessageId.MeleeEnemyDefeated);
                }
            }
            else if (target is PlayerEntity)
            {
                logSystem.LogById(MessageId.MeleeEnemyHitPlayerDamage, damage);
                if (target.IsDead)
                {
                    logSystem.LogById(MessageId.MeleePlayerDefeated);
                }
            }
            else
            {
                // 汎用ログ（現状は使用頻度は低い想定）。
                logSystem.LogById(MessageId.MeleeHitGenericDamage, damage);
            }

            return true;
        }
    }
}
