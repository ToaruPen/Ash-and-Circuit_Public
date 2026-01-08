using System.Collections.Generic;

namespace AshNCircuit.Core.Items
{
    /// <summary>
    /// プレイヤーの所持品とスタック数を管理するシンプルなインベントリクラス。
    /// UI からの表示・操作は別チケット（TICKET-0010/0011）で扱う前提とし、
    /// 本クラスは「追加・削除・列挙」の最低限の責務に絞る。
    /// </summary>
    public sealed class Inventory
    {
        private readonly InventoryStorage _storage;
        private readonly InventoryEquipmentManager _equipmentManager;

        /// <summary>
        /// 保持できるスタックの最大数。
        /// 暫定値として 20 とする（TICKET-0028 の想定に基づく）。
        /// </summary>
        public int MaxStacks { get; }

        public IReadOnlyList<InventoryEntry> Entries => _storage.Entries;

        public Inventory(int maxStacks = 20)
        {
            MaxStacks = maxStacks > 0 ? maxStacks : 20;
            _storage = new InventoryStorage(MaxStacks);
            _equipmentManager = new InventoryEquipmentManager();
        }

        /// <summary>
        /// UI 表示用にインベントリエントリを列挙するためのヘルパー。
        /// エントリごとに表示名・スタック数・ItemDefinition ID を返す。
        /// </summary>
        public IEnumerable<InventoryEntryView> GetEntriesForUi()
        {
            return _storage.GetEntriesForUi();
        }

        /// <summary>
        /// 指定アイテムを追加する（スタック可能な場合は既存エントリへ加算）。
        /// 追加しきれなかった分は破棄されるため、呼び出し側は戻り値で成否を確認する。
        /// </summary>
        public bool TryAdd(ItemDefinition item, int amount)
        {
            return _storage.TryAdd(item, amount);
        }

        /// <summary>
        /// 指定アイテムを消費（削除）する。
        /// 必要数を満たせなかった場合は何も変更せず false を返す。
        /// </summary>
        public bool TryRemove(ItemDefinition item, int amount)
        {
            return _storage.TryRemove(item, amount);
        }

        /// <summary>
        /// 指定アイテムの合計所持数を返す。
        /// </summary>
        public int GetTotalCount(ItemDefinition item)
        {
            return _storage.GetTotalCount(item);
        }

        /// <summary>
        /// 指定スロットに装備されているアイテムを返す。未装備の場合は null。
        /// </summary>
        public ItemDefinition? GetEquipped(EquipmentSlot slot)
        {
            return _equipmentManager.GetEquipped(slot);
        }

        /// <summary>
        /// 指定アイテムを対応スロットに装備する。
        /// 既に装備がある場合は自動的に外してインベントリに戻す。
        /// </summary>
        public bool TryEquip(ItemDefinition item)
        {
            if (item == null)
            {
                return false;
            }

            var slot = EquipmentSlotUtility.GetEquippableSlot(item);
            if (!slot.HasValue)
            {
                return false;
            }

            // インベントリから1つ消費する。
            if (!_storage.TryRemove(item, 1))
            {
                return false;
            }

            // 既存装備があれば外す。
            var currentEquip = GetEquipped(slot.Value);
            if (currentEquip != null)
            {
                _storage.TryAdd(currentEquip, 1);
            }

            _equipmentManager.SetEquipped(slot.Value, item);
            return true;
        }

        /// <summary>
        /// 指定スロットの装備を外し、インベントリに戻す。
        /// </summary>
        public bool TryUnequip(EquipmentSlot slot)
        {
            var current = GetEquipped(slot);
            if (current == null)
            {
                return false;
            }

            // インベントリに戻す。
            var added = _storage.TryAdd(current, 1);
            if (!added)
            {
                // インベントリがいっぱいで戻せない場合は装備を外さない。
                return false;
            }

            _equipmentManager.ClearEquipped(slot);
            return true;
        }

        /// <summary>
        /// 全装備スロットの状態を列挙する（UI表示用）。
        /// </summary>
        public IEnumerable<(EquipmentSlot Slot, ItemDefinition? Item)> GetAllEquipment()
        {
            return _equipmentManager.GetAllEquipment();
        }
    }

    /// <summary>
    /// UI 向けに公開するインベントリエントリのビュー用構造体。
    /// </summary>
    public readonly struct InventoryEntryView
    {
        public string ItemId { get; }
        public string DisplayName { get; }
        public int Amount { get; }

        public InventoryEntryView(string itemId, string displayName, int amount)
        {
            ItemId = itemId;
            DisplayName = displayName;
            Amount = amount;
        }
    }

    /// <summary>
    /// インベントリ内の 1 スタックを表すエントリ。
    /// </summary>
    public sealed class InventoryEntry
    {
        public ItemDefinition Item { get; }

        public int Amount { get; private set; }

        public InventoryEntry(ItemDefinition item, int amount)
        {
            Item = item;
            Amount = amount > 0 ? amount : 0;
        }

        public void AddAmount(int delta)
        {
            Amount += delta;
            if (Amount < 0)
            {
                Amount = 0;
            }
        }
    }
}
