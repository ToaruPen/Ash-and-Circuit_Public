using AshNCircuit.Core.GameLoop;
using AshNCircuit.Core.Map;
using AshNCircuit.Core.Systems;
using AshNCircuit.UnityIntegration.Audio;
using UnityEngine;

namespace AshNCircuit.UnityIntegration.MapView
{
    public partial class GameRoot
    {
        private LogSystemUnityConsoleLogger? _unityConsoleLogger;
        private SfxPlayer? _sfxPlayer;
        private LogSystemSfxBridge? _logSystemSfxBridge;

        private void InitializeCoreSystems()
        {
            _mapManager = new MapManager(mapWidth, mapHeight);
            _mapView = new MapViewPresenter(_mapManager, tilesParent, tileSpriteMappings, propSpriteMappings, itemPileSprite, tileSize);
            _actionSystem = new ActionSystem();
            _logSystem = new LogSystem();
            _effectSystem = new EffectSystem();
            _unityConsoleLogger = new LogSystemUnityConsoleLogger(_logSystem);
            InitializeSfxPlayer();
        }

        private void InitializeTurnSystems()
        {
            _turnManager = new TurnManager();
            _gameController = new GameController(_turnManager, _actionSystem, _effectSystem, _mapManager, _logSystem);
            _turnManager.OnEnvironmentPhase += HandleEnvironmentPhase;
            _turnManager.OnStatusEffectPhase += HandleStatusEffectPhase;
        }

        private void InitializeSfxPlayer()
        {
            if (_sfxPlayer != null || _logSystem == null)
            {
                return;
            }

            var sfxObject = new GameObject("SfxPlayer");
            sfxObject.transform.SetParent(transform, false);
            _sfxPlayer = sfxObject.AddComponent<SfxPlayer>();
            _logSystemSfxBridge = new LogSystemSfxBridge(_logSystem, _sfxPlayer);
        }
    }
}
