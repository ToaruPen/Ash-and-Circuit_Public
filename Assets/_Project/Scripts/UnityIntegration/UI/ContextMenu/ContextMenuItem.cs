using System;
using System.Collections.Generic;

namespace AshNCircuit.UnityIntegration.UI.ContextMenuUi
{
    /// <summary>
    /// コンテキストメニューの1項目を表すデータクラス。
    /// </summary>
    public sealed class ContextMenuItem
    {
        /// <summary>
        /// メニューに表示されるラベルテキスト。
        /// </summary>
        public string Label { get; }

        /// <summary>
        /// 項目選択時に呼び出されるコールバック。
        /// </summary>
        public Action OnSelected { get; }

        /// <summary>
        /// 左クリックでサブメニューへ遷移する場合の項目リスト。
        /// null の場合はサブメニューを持たない。
        /// </summary>
        public IReadOnlyList<ContextMenuItem>? SubMenuItems { get; }

        /// <summary>
        /// 右クリックでサブメニューへ遷移する場合の項目リスト。
        /// null の場合はサブメニューを持たない。
        /// </summary>
        public IReadOnlyList<ContextMenuItem>? RightClickSubMenuItems { get; }

        /// <summary>
        /// 項目が選択可能かどうか。false の場合はグレーアウト表示される。
        /// </summary>
        public bool IsEnabled { get; }

        public ContextMenuItem(
            string label,
            Action onSelected,
            bool isEnabled = true,
            IReadOnlyList<ContextMenuItem>? subMenuItems = null,
            IReadOnlyList<ContextMenuItem>? rightClickSubMenuItems = null)
        {
            Label = label ?? string.Empty;
            OnSelected = onSelected ?? (() => { });
            IsEnabled = isEnabled;
            SubMenuItems = subMenuItems;
            RightClickSubMenuItems = rightClickSubMenuItems;
        }
    }
}
