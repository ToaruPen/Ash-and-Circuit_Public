using System;
using AshNCircuit.Core.Systems;

namespace AshNCircuit.UnityIntegration.Audio
{
    public sealed class LogSystemSfxBridge : IDisposable
    {
        private readonly LogSystem _logSystem;
        private readonly SfxPlayer _sfxPlayer;

        public LogSystemSfxBridge(LogSystem logSystem, SfxPlayer sfxPlayer)
        {
            _logSystem = logSystem ?? throw new ArgumentNullException(nameof(logSystem));
            _sfxPlayer = sfxPlayer ?? throw new ArgumentNullException(nameof(sfxPlayer));

            _logSystem.OnMessageLoggedById += HandleMessageLoggedById;
        }

        public void Dispose()
        {
            _logSystem.OnMessageLoggedById -= HandleMessageLoggedById;
        }

        private void HandleMessageLoggedById(MessageId id)
        {
            switch (id)
            {
                case MessageId.MeleePlayerHitEnemyDamage:
                case MessageId.MeleeHitGenericDamage:
                    _sfxPlayer.PlayOneShot(SfxIds.CombatMeleeHit);
                    break;
                case MessageId.MeleeEnemyHitPlayerDamage:
                    _sfxPlayer.PlayOneShot(SfxIds.CombatPlayerHit);
                    break;
                case MessageId.MeleeEnemyDefeated:
                    _sfxPlayer.PlayOneShot(SfxIds.CombatKill);
                    break;
                case MessageId.PickupGenericItem:
                case MessageId.PickupDirtGeneric:
                case MessageId.PickupDirtFromOilGround:
                    _sfxPlayer.PlayOneShot(SfxIds.UiPickup);
                    break;
            }
        }
    }
}
