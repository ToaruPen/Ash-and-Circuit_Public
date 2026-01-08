using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace AshNCircuit.UnityIntegration.UI
{
    /// <summary>
    /// UI Toolkit 用の座標変換とポジショニングをまとめたユーティリティ。
    /// - スクリーン座標（Mouse.current.position）からパネル座標への変換
    /// - 要素がパネル外にはみ出さないようにするための調整
    /// </summary>
    public static class UiToolkitPositioningUtility
    {
        /// <summary>
        /// マウスのスクリーン座標を取得する。
        /// Mouse.current が利用できない場合は Vector2.zero を返す。
        /// </summary>
        public static Vector2 GetMouseScreenPosition()
        {
            return Mouse.current != null
                ? (Vector2)Mouse.current.position.ReadValue()
                : Vector2.zero;
        }

        /// <summary>
        /// スクリーン座標を指定したルート要素のパネル座標に変換する。
        /// ルートまたはパネルが未初期化の場合はスクリーン座標をそのまま返す。
        /// </summary>
        public static Vector2 ScreenToPanel(VisualElement root, Vector2 screenPosition)
        {
            if (root?.panel != null)
            {
                return RuntimePanelUtils.ScreenToPanel(root.panel, screenPosition);
            }

            // パネルがまだ初期化されていない場合は近似としてスクリーン座標を返す。
            return screenPosition;
        }

        /// <summary>
        /// スクリーン座標付近に要素を配置し、必要に応じてパネル内に収まるよう調整する。
        /// 即時に left/top を設定し、その直後の resolvedStyle を前提に簡易なクランプを行う。
        /// </summary>
        public static void PositionElementNearScreenPoint(
            VisualElement root,
            VisualElement element,
            Vector2 screenPosition,
            Vector2 offset,
            float margin = 8f)
        {
            if (root == null || element == null)
            {
                return;
            }

            var panelWidth = root.resolvedStyle.width;
            var panelHeight = root.resolvedStyle.height;
            var elementWidth = element.resolvedStyle.width;
            var elementHeight = element.resolvedStyle.height;

            // パネルサイズがまだ 0 の場合は、ScreenToPanel を使った素朴な変換にフォールバックする。
            if (panelWidth <= 0f || panelHeight <= 0f)
            {
                var panelPositionFallback = ScreenToPanel(root, screenPosition);
                element.style.left = panelPositionFallback.x + offset.x;
                element.style.top = panelPositionFallback.y + offset.y;
                return;
            }

            var screenWidth = (float)Screen.width;
            var screenHeight = (float)Screen.height;

            if (screenWidth <= 0f || screenHeight <= 0f)
            {
                // Screen サイズが取れない場合も ScreenToPanel にフォールバックする。
                var panelPositionFallback = ScreenToPanel(root, screenPosition);
                element.style.left = panelPositionFallback.x + offset.x;
                element.style.top = panelPositionFallback.y + offset.y;
                return;
            }

            // スクリーン座標を 0..1 に正規化し、パネルの幅・高さに応じて比例配置する。
            var normalizedX = screenPosition.x / screenWidth;
            var normalizedY = screenPosition.y / screenHeight;

            var panelX = normalizedX * panelWidth;
            var panelYFromBottom = normalizedY * panelHeight;
            // UI Toolkit のレイアウトは上原点なので、下原点座標から反転する。
            var panelY = panelHeight - panelYFromBottom;

            var desiredLeft = panelX + offset.x;
            var desiredTop = panelY + offset.y;

            // 要素サイズがまだ 0 の場合は、クランプせずにそのまま配置する。
            if (elementWidth <= 0f || elementHeight <= 0f)
            {
                element.style.left = desiredLeft;
                element.style.top = desiredTop;
                return;
            }

            var maxLeft = panelWidth - elementWidth - margin;
            var maxTop = panelHeight - elementHeight - margin;

            var left = Mathf.Clamp(desiredLeft, margin, maxLeft);
            var top = Mathf.Clamp(desiredTop, margin, maxTop);

            element.style.left = left;
            element.style.top = top;
        }
    }
}
