using System;
using System.Collections.Generic;
using AshNCircuit.Core.Systems;
using AshNCircuit.UnityIntegration.Audio;
using AshNCircuit.UnityIntegration.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace AshNCircuit.UnityIntegration.UI.ContextMenuUi
{
    /// <summary>
    /// 右クリックで表示されるコンテキストメニューのプレゼンター。
    /// Caves of Qud スタイルのテキストリスト形式メニューを提供する。
    /// </summary>
    public sealed class ContextMenuView
    {
        private readonly struct MenuState
        {
            public Vector2 ScreenPosition { get; }
            public List<ContextMenuItem> Items { get; }

            public MenuState(Vector2 screenPosition, List<ContextMenuItem> items)
            {
                ScreenPosition = screenPosition;
                Items = items;
            }
        }

        private readonly VisualElement _root = null!;
        private readonly VisualElement _menuContainer = null!;
        private readonly VisualElement _backdrop = null!;

        private readonly List<VisualElement> _itemElements = new List<VisualElement>();
        private readonly List<MenuState> _menuStack = new List<MenuState>();
        private List<ContextMenuItem> _currentItems = new List<ContextMenuItem>();
        private Vector2 _currentScreenPosition;
        private bool _isVisible;
        private SfxPlayer? _sfxPlayer;

        public bool IsVisible => _isVisible;

        /// <summary>
        /// メニューが閉じられた際に発火するイベント。
        /// </summary>
        public event Action? OnMenuClosed;

        public ContextMenuView(UIDocument document)
        {
            if (document == null)
            {
                Debug.LogWarning("ContextMenuView: UIDocument が未割り当てです。");
                return;
            }

            _root = document.rootVisualElement;
            if (_root == null)
            {
                Debug.LogWarning("ContextMenuView: rootVisualElement が取得できません。");
                return;
            }

            _backdrop = _root.Q<VisualElement>("context-menu-backdrop");
            _menuContainer = _root.Q<VisualElement>("context-menu-container");

            if (_backdrop == null)
            {
                Debug.LogWarning("ContextMenuView: 'context-menu-backdrop' が見つかりません。");
            }

            if (_menuContainer == null)
            {
                Debug.LogWarning("ContextMenuView: 'context-menu-container' が見つかりません。");
            }

            // 背景クリックでメニューを閉じる。
            if (_backdrop != null)
            {
                _backdrop.RegisterCallback<PointerDownEvent>(OnBackdropClicked);
            }

            Hide();
        }

        public void SetSfxPlayer(SfxPlayer sfxPlayer)
        {
            _sfxPlayer = sfxPlayer;
        }

        /// <summary>
        /// 指定位置にメニューを表示する。
        /// </summary>
        /// <param name="screenPosition">スクリーン座標（Input System の Mouse.position）でのメニュー表示位置。</param>
        /// <param name="items">メニュー項目のコレクション。</param>
        public void Show(Vector2 screenPosition, IEnumerable<ContextMenuItem> items)
        {
            if (_root == null || _menuContainer == null || _backdrop == null)
            {
                return;
            }

            _menuStack.Clear();
            _currentScreenPosition = screenPosition;
            _currentItems = items != null ? new List<ContextMenuItem>(items) : new List<ContextMenuItem>();
            RenderCurrentMenu(playContextMenuSfx: true);
        }

        /// <summary>
        /// メニューを非表示にする。
        /// </summary>
        public void Hide()
        {
            if (_menuStack.Count > 0)
            {
                _sfxPlayer?.PlayOneShot(SfxIds.UiCancel);
                NavigateBack();
                return;
            }

            CloseAll(playCancelSfx: true);
        }

        private void RenderCurrentMenu(bool playContextMenuSfx)
        {
            if (_root == null || _menuContainer == null || _backdrop == null)
            {
                return;
            }

            ClearItems();

            foreach (var item in _currentItems)
            {
                var element = BuildMenuItemElement(item);
                _menuContainer.Add(element);
                _itemElements.Add(element);
            }

            _backdrop.style.display = DisplayStyle.Flex;
            _menuContainer.style.display = DisplayStyle.Flex;
            _isVisible = true;
            if (playContextMenuSfx)
            {
                _sfxPlayer?.PlayOneShot(SfxIds.UiContextMenu);
            }

            // マウス付近に配置し、パネル内に収まるよう調整する。
            UiToolkitPositioningUtility.PositionElementNearScreenPoint(
                _root,
                _menuContainer,
                _currentScreenPosition,
                Vector2.zero);
        }

        private void NavigateToSubMenu(IReadOnlyList<ContextMenuItem> items)
        {
            if (items == null || items.Count == 0)
            {
                return;
            }

            _menuStack.Add(new MenuState(_currentScreenPosition, _currentItems));
            _currentItems = new List<ContextMenuItem>(items);
            RenderCurrentMenu(playContextMenuSfx: false);
        }

        private void NavigateBack()
        {
            if (_menuStack.Count == 0)
            {
                return;
            }

            var index = _menuStack.Count - 1;
            var state = _menuStack[index];
            _menuStack.RemoveAt(index);

            _currentScreenPosition = state.ScreenPosition;
            _currentItems = state.Items;
            RenderCurrentMenu(playContextMenuSfx: false);
        }

        private void CloseAll(bool playCancelSfx)
        {
            _menuStack.Clear();
            _currentItems.Clear();
            HideInternal(playCancelSfx);
        }

        private void HideInternal(bool playCancelSfx)
        {
            if (_backdrop != null)
            {
                _backdrop.style.display = DisplayStyle.None;
            }

            if (_menuContainer != null)
            {
                _menuContainer.style.display = DisplayStyle.None;
            }

            _isVisible = false;
            ClearItems();
            if (playCancelSfx)
            {
                _sfxPlayer?.PlayOneShot(SfxIds.UiCancel);
            }
            OnMenuClosed?.Invoke();
        }

        private void ClearItems()
        {
            if (_menuContainer == null)
            {
                return;
            }

            foreach (var element in _itemElements)
            {
                _menuContainer.Remove(element);
            }

            _itemElements.Clear();
        }

        private VisualElement BuildMenuItemElement(ContextMenuItem item)
        {
            var row = new VisualElement();
            row.AddToClassList("context-menu-item");

            if (!item.IsEnabled)
            {
                row.AddToClassList("context-menu-item--disabled");
            }

            var label = new Label(item.Label);
            label.AddToClassList("context-menu-item-label");
            row.Add(label);

            if (item.IsEnabled && item.OnSelected != null)
            {
                row.RegisterCallback<PointerDownEvent>(evt =>
                {
                    evt.StopPropagation();

                    // 右クリックは「アイテムのコンテキスト」などのサブメニュー遷移に使うことがある。
                    if (evt.button == (int)MouseButton.RightMouse && item.RightClickSubMenuItems != null && item.RightClickSubMenuItems.Count > 0)
                    {
                        _sfxPlayer?.PlayOneShot(SfxIds.UiConfirm);
                        NavigateToSubMenu(item.RightClickSubMenuItems);
                        return;
                    }

                    // 左クリックでサブメニューへ遷移する。
                    if (evt.button == (int)MouseButton.LeftMouse && item.SubMenuItems != null && item.SubMenuItems.Count > 0)
                    {
                        _sfxPlayer?.PlayOneShot(SfxIds.UiConfirm);
                        if (IsInspectLabel(item.Label))
                        {
                            _sfxPlayer?.PlayOneShot(SfxIds.UiInspect);
                        }
                        NavigateToSubMenu(item.SubMenuItems);
                        return;
                    }

                    // 左クリックで通常のアクション実行。
                    if (evt.button != (int)MouseButton.LeftMouse)
                    {
                        return;
                    }

                    if (IsCloseLabel(item.Label))
                    {
                        _sfxPlayer?.PlayOneShot(SfxIds.UiCancel);
                    }
                    else
                    {
                        _sfxPlayer?.PlayOneShot(SfxIds.UiConfirm);
                    }
                    if (IsInspectLabel(item.Label))
                    {
                        _sfxPlayer?.PlayOneShot(SfxIds.UiInspect);
                    }
                    item.OnSelected.Invoke();
                    CloseAll(playCancelSfx: false);
                });

                row.RegisterCallback<PointerEnterEvent>(evt =>
                {
                    row.AddToClassList("context-menu-item--hover");
                });

                row.RegisterCallback<PointerLeaveEvent>(evt =>
                {
                    row.RemoveFromClassList("context-menu-item--hover");
                });
            }

            return row;
        }

        private void OnBackdropClicked(PointerDownEvent evt)
        {
            // メニュー外クリックのうち、左クリックでのみメニューを閉じる。
            // 右クリックは GameRoot 側の入力処理で別対象へのメニュー更新に用いるため、ここでは閉じない。
            if (evt.button == (int)MouseButton.LeftMouse)
            {
                Hide();
            }
        }

        private static bool IsInspectLabel(string label)
        {
            if (string.IsNullOrEmpty(label))
            {
                return false;
            }

            return label == MessageCatalog.Format(MessageId.UiTileMenuExamine)
                || label == MessageCatalog.Format(MessageId.UiInventoryMenuExamine);
        }

        private static bool IsCloseLabel(string label)
        {
            return label == "閉じる";
        }
    }
}
