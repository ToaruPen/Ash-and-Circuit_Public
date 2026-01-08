using System;
using System.Collections.Generic;
using AshNCircuit.Core.Systems;
using UnityEngine;
using UnityEngine.UIElements;

namespace AshNCircuit.UnityIntegration.UI.Log
{
    /// <summary>
    /// LogSystem からのメッセージを UI Toolkit のスクロールビューに表示するプレゼンター。
    /// </summary>
    public sealed class LogView
    {
        private readonly LogSystem _logSystem = null!;
        private readonly ScrollView _scrollView = null!;
        private readonly List<string> _lines = new List<string>();
        private readonly int _maxEntries;

        public LogView(UIDocument document, LogSystem logSystem, int maxEntries = 20)
        {
            if (logSystem == null) throw new ArgumentNullException(nameof(logSystem));

            _logSystem = logSystem;
            _maxEntries = maxEntries > 0 ? maxEntries : 20;

            if (document == null)
            {
                Debug.LogWarning("LogView: UIDocument が未割り当てです。");
                return;
            }

            var root = document.rootVisualElement;
            if (root == null)
            {
                Debug.LogWarning("LogView: rootVisualElement が取得できません。");
                return;
            }

            _scrollView = root.Q<ScrollView>("log-scroll-view");
            if (_scrollView == null)
            {
                Debug.LogWarning("LogView: ScrollView 'log-scroll-view' が見つかりません。");
            }

            foreach (var message in _logSystem.Messages)
            {
                AppendLine(message);
            }

            _logSystem.OnMessageLogged += HandleMessageLogged;

            Refresh();
        }

        public void Dispose()
        {
            _logSystem.OnMessageLogged -= HandleMessageLogged;
        }

        private void HandleMessageLogged(string message)
        {
            AppendLine(message);
            Refresh();
        }

        private void AppendLine(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            _lines.Add(message);
            if (_lines.Count > _maxEntries)
            {
                var overflow = _lines.Count - _maxEntries;
                _lines.RemoveRange(0, overflow);
            }
        }

        private void Refresh()
        {
            if (_scrollView == null)
            {
                return;
            }

            _scrollView.Clear();
            foreach (var line in _lines)
            {
                var label = new Label(line);
                label.AddToClassList("log-entry");
                _scrollView.Add(label);
            }

            // 常に末尾が見えるようにスクロール位置を末尾に移動する。
            if (_scrollView.verticalScroller != null)
            {
                _scrollView.verticalScroller.value = float.MaxValue;
            }
        }
    }
}
