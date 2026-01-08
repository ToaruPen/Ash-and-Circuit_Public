using AshNCircuit.UnityIntegration.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace AshNCircuit.UnityIntegration.UI.TooltipUi
{
    /// <summary>
    /// アイテムホバー時に表示されるツールチップのプレゼンター。
    /// </summary>
    public sealed class TooltipView
    {
        private readonly VisualElement _root = null!;
        private readonly VisualElement _container = null!;
        private readonly Label _titleLabel = null!;
        private readonly Label _tagsLabel = null!;
        private readonly Label _descriptionLabel = null!;

        private bool _isVisible;

        public bool IsVisible => _isVisible;

        public TooltipView(UIDocument document)
        {
            if (document == null)
            {
                Debug.LogWarning("TooltipView: UIDocument が未割り当てです。");
                return;
            }

            _root = document.rootVisualElement;
            if (_root == null)
            {
                Debug.LogWarning("TooltipView: rootVisualElement が取得できません。");
                return;
            }

            _container = _root.Q<VisualElement>("tooltip-container");
            _titleLabel = _root.Q<Label>("tooltip-title");
            _tagsLabel = _root.Q<Label>("tooltip-tags");
            _descriptionLabel = _root.Q<Label>("tooltip-description");

            if (_container == null)
            {
                Debug.LogWarning("TooltipView: 'tooltip-container' が見つかりません。");
            }

            Hide();
        }

        /// <summary>
        /// 指定位置にツールチップを表示する。
        /// </summary>
        /// <param name="screenPosition">スクリーン座標（Input System の Mouse.position）。</param>
        /// <param name="title">タイトル（アイテム名など）。</param>
        /// <param name="tags">タグ情報。</param>
        /// <param name="description">説明文。</param>
        public void Show(Vector2 screenPosition, string title, string tags, string description)
        {
            if (_root == null || _container == null)
            {
                return;
            }

            if (_titleLabel != null)
            {
                _titleLabel.text = title ?? string.Empty;
            }

            if (_tagsLabel != null)
            {
                _tagsLabel.text = tags ?? string.Empty;
                _tagsLabel.style.display = string.IsNullOrEmpty(tags) ? DisplayStyle.None : DisplayStyle.Flex;
            }

            if (_descriptionLabel != null)
            {
                _descriptionLabel.text = description ?? string.Empty;
            }

            _container.style.display = DisplayStyle.Flex;
            _isVisible = true;

            // マウス付近に配置し、パネル内に収まるよう調整する。
            UiToolkitPositioningUtility.PositionElementNearScreenPoint(
                _root,
                _container,
                screenPosition,
                new Vector2(16f, 16f));
        }

        /// <summary>
        /// ツールチップを非表示にする。
        /// </summary>
        public void Hide()
        {
            if (_container != null)
            {
                _container.style.display = DisplayStyle.None;
            }

            _isVisible = false;
        }

        /// <summary>
        /// マウス位置に追従してツールチップを移動する。
        /// </summary>
        /// <param name="screenPosition">スクリーン座標（Input System の Mouse.position）。</param>
        public void UpdatePosition(Vector2 screenPosition)
        {
            if (_root == null || _container == null || !_isVisible)
            {
                return;
            }

            UiToolkitPositioningUtility.PositionElementNearScreenPoint(
                _root,
                _container,
                screenPosition,
                new Vector2(16f, 16f));
        }
    }
}
