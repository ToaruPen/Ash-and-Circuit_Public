using AshNCircuit.Core.Entities;
using AshNCircuit.Core.GameLoop;
using AshNCircuit.Core.Systems;
using UnityEngine;
using UnityEngine.UIElements;

namespace AshNCircuit.UnityIntegration.UI.Hud
{
    /// <summary>
    /// プレイヤーのHP・各種ステータスを表示する HUD 用 UI Toolkit プレゼンター。
    /// 新レイアウト: 上部バー + 左上HP/XP + 右サイドバー（ミニマップ枠）
    /// </summary>
    public sealed class HudView
    {
        private readonly PlayerEntity _player;
        private readonly TurnManager _turnManager;

        // 上部バー
        private readonly Label _statusLabel = null!;
        private readonly Label _hungerLabel = null!;
        private readonly Label _thirstLabel = null!;
        private readonly Label _levelLabel = null!;
        private readonly Label _defenseLabel = null!;
        private readonly Label _evasionLabel = null!;
        private readonly Label _timeLabel = null!;
        private readonly Label _locationLabel = null!;

        // HP/XPエリア
        private readonly VisualElement _hpBarFill = null!;
        private readonly Label _hpValueLabel = null!;
        private readonly VisualElement _xpBarFill = null!;
        private readonly Label _xpValueLabel = null!;

        // ミニマップ
        private readonly VisualElement _minimapContent = null!;

        public HudView(UIDocument document, PlayerEntity player, TurnManager turnManager)
        {
            _player = player;
            _turnManager = turnManager;

            if (document == null)
            {
                Debug.LogWarning("HudView: UIDocument が未割り当てです。");
                return;
            }

            var root = document.rootVisualElement;
            if (root == null)
            {
                Debug.LogWarning("HudView: rootVisualElement が取得できません。");
                return;
            }

            // 上部バー
            _statusLabel = root.Q<Label>("hud-status-label");
            _hungerLabel = root.Q<Label>("hud-hunger-label");
            _thirstLabel = root.Q<Label>("hud-thirst-label");
            _levelLabel = root.Q<Label>("hud-level-label");
            _defenseLabel = root.Q<Label>("hud-defense-label");
            _evasionLabel = root.Q<Label>("hud-evasion-label");
            _timeLabel = root.Q<Label>("hud-time-label");
            _locationLabel = root.Q<Label>("hud-location-label");

            // HP/XPエリア
            _hpBarFill = root.Q<VisualElement>("hud-hp-bar-fill");
            _hpValueLabel = root.Q<Label>("hud-hp-value");
            _xpBarFill = root.Q<VisualElement>("hud-xp-bar-fill");
            _xpValueLabel = root.Q<Label>("hud-xp-value");

            // ミニマップ
            _minimapContent = root.Q<VisualElement>("hud-minimap-content");

            // 初期表示
            Refresh();
        }

        /// <summary>
        /// HUD の表示を更新する。
        /// </summary>
        public void Refresh()
        {
            RefreshHp();
            RefreshXp();
            RefreshTopBar();
        }

        private void RefreshHp()
        {
            if (_player == null)
            {
                return;
            }

            var current = _player.CurrentHp;
            var max = _player.MaxHp;
            var ratio = max > 0 ? (float)current / max : 0f;

            if (_hpBarFill != null)
            {
                _hpBarFill.style.width = new StyleLength(new Length(ratio * 100f, LengthUnit.Percent));
            }

            if (_hpValueLabel != null)
            {
                _hpValueLabel.text = ResolveHudText(MessageId.UiHudHpValue, current, max);
            }
        }

        private void RefreshXp()
        {
            // MVP: XPシステムは未実装のため仮表示
            if (_xpBarFill != null)
            {
                _xpBarFill.style.width = new StyleLength(new Length(0f, LengthUnit.Percent));
            }

            if (_xpValueLabel != null)
            {
                _xpValueLabel.text = ResolveHudText(MessageId.UiHudXpValue, 0, 100);
            }
        }

        private void RefreshTopBar()
        {
            // MVP: 多くのステータスは未実装のため仮表示
            if (_statusLabel != null)
            {
                _statusLabel.text = ResolveHudText(MessageId.UiHudStatusPlaceholder);
            }

            if (_hungerLabel != null)
            {
                _hungerLabel.text = ResolveHudText(MessageId.UiHudHungerPlaceholder);
            }

            if (_thirstLabel != null)
            {
                _thirstLabel.text = ResolveHudText(MessageId.UiHudThirstPlaceholder);
            }

            if (_levelLabel != null)
            {
                _levelLabel.text = ResolveHudText(MessageId.UiHudLevelValue, 1);
            }

            if (_defenseLabel != null)
            {
                _defenseLabel.text = ResolveHudText(MessageId.UiHudDefenseValue, 0);
            }

            if (_evasionLabel != null)
            {
                _evasionLabel.text = ResolveHudText(MessageId.UiHudEvasionValue, 0);
            }

            if (_timeLabel != null)
            {
                if (_turnManager != null)
                {
                    var displayTurn = _turnManager.CurrentTurn + 1;
                    _timeLabel.text = ResolveHudText(MessageId.UiHudTimeTurn, displayTurn);
                }
                else
                {
                    _timeLabel.text = ResolveHudText(MessageId.UiHudTimeTurnUnknown);
                }
            }

            if (_locationLabel != null)
            {
                _locationLabel.text = ResolveHudText(MessageId.UiHudLocationDefault);
            }
        }

        /// <summary>
        /// 場所名を更新する。
        /// </summary>
        public void SetLocation(string locationName)
        {
            if (_locationLabel != null)
            {
                _locationLabel.text = locationName;
            }
        }

        /// <summary>
        /// 時間帯を更新する。
        /// </summary>
        public void SetTimeOfDay(string timeOfDay)
        {
            if (_timeLabel != null)
            {
                _timeLabel.text = timeOfDay;
            }
        }

        private static string ResolveHudText(MessageId id, params object[] args)
        {
            return MessageCatalog.Format(id, args);
        }
    }
}
