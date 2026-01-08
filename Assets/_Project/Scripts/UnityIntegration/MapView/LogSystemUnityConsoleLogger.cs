using System;
using AshNCircuit.Core.Systems;
using UnityEngine;

namespace AshNCircuit.UnityIntegration.MapView
{
    public sealed class LogSystemUnityConsoleLogger : IDisposable
    {
        private LogSystem? _logSystem;

        public LogSystemUnityConsoleLogger(LogSystem logSystem)
        {
            _logSystem = logSystem ?? throw new ArgumentNullException(nameof(logSystem));
            _logSystem.OnMessageLogged += HandleMessageLogged;
        }

        public void Dispose()
        {
            Detach();
            GC.SuppressFinalize(this);
        }

        public void Detach()
        {
            if (_logSystem == null)
            {
                return;
            }

            _logSystem.OnMessageLogged -= HandleMessageLogged;
            _logSystem = null;
        }

        private static void HandleMessageLogged(string message)
        {
            Debug.Log(message);
        }
    }
}
