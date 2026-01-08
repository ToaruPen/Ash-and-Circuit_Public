namespace AshNCircuit.Core.Items
{
    /// <summary>
    /// 装備スロットの種別を定義する列挙型。
    /// Caves of Qud スタイルの身体部位ベース装備システムの基盤。
    /// </summary>
    public enum EquipmentSlot
    {
        /// <summary>
        /// 頭部スロット（兜、帽子、ゴーグルなど）。
        /// </summary>
        Head,

        /// <summary>
        /// 胴体スロット（鎧、ローブ、服など）。
        /// </summary>
        Body,

        /// <summary>
        /// メインハンドスロット（主武器：短剣、弓など）。
        /// </summary>
        MainHand,

        /// <summary>
        /// サブハンドスロット（盾、松明、副武器など）。
        /// </summary>
        OffHand,

        /// <summary>
        /// 背中スロット（マント、矢筒、バックパックなど）。
        /// </summary>
        Back,

        /// <summary>
        /// 足スロット（ブーツ、サンダルなど）。
        /// </summary>
        Feet
    }

    /// <summary>
    /// EquipmentSlot に関するユーティリティ。
    /// </summary>
    public static class EquipmentSlotUtility
    {
        /// <summary>
        /// スロット種別の日本語表示名を返す。
        /// </summary>
        public static string GetDisplayName(EquipmentSlot slot)
        {
            switch (slot)
            {
                case EquipmentSlot.Head:
                    return "頭";
                case EquipmentSlot.Body:
                    return "胴体";
                case EquipmentSlot.MainHand:
                    return "メインハンド";
                case EquipmentSlot.OffHand:
                    return "サブハンド";
                case EquipmentSlot.Back:
                    return "背中";
                case EquipmentSlot.Feet:
                    return "足";
                default:
                    return "不明";
            }
        }

        /// <summary>
        /// アイテムのタグから装備可能なスロットを判定する。
        /// 複数スロットに装備可能な場合は優先度の高いものを返す。
        /// 装備不可能な場合は null を返す。
        /// </summary>
        public static EquipmentSlot? GetEquippableSlot(ItemDefinition item)
        {
            if (item == null || item.Tags == null)
            {
                return null;
            }

            // 武器タグがあればハンドスロット
            if (HasTag(item, "weapon"))
            {
                return EquipmentSlot.MainHand;
            }

            // 矢筒や弾薬は背中スロット
            if (HasTag(item, "ammo"))
            {
                return EquipmentSlot.Back;
            }

            // 防具タグがあれば胴体
            if (HasTag(item, "armor") || HasTag(item, "body_armor"))
            {
                return EquipmentSlot.Body;
            }

            // 頭装備
            if (HasTag(item, "helmet") || HasTag(item, "head_armor"))
            {
                return EquipmentSlot.Head;
            }

            // 足装備
            if (HasTag(item, "boots") || HasTag(item, "feet_armor"))
            {
                return EquipmentSlot.Feet;
            }

            // 盾やオフハンド用
            if (HasTag(item, "shield") || HasTag(item, "offhand"))
            {
                return EquipmentSlot.OffHand;
            }

            return null;
        }

        private static bool HasTag(ItemDefinition item, string tag)
        {
            foreach (var t in item.Tags)
            {
                if (t == tag)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
