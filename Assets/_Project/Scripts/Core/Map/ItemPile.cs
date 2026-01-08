using System.Collections.Generic;
using AshNCircuit.Core.Items;

namespace AshNCircuit.Core.Map
{
    /// <summary>
    /// 地面上の落ち物（ItemPile）。
    /// - 1マスに 1 つ（MapManager 側で制約）。
    /// - 複数アイテムを保持できる。
    /// - 代表スプライト（代表アイテム）は「最古固定」（最も古いエントリ）とする。
    /// - スタック可能アイテムはマージするが、寿命管理は dropTurn 単位で保持する。
    /// </summary>
    public sealed class ItemPile
    {
        public sealed class Entry
        {
            public ItemDefinition Item { get; }

            public int Amount { get; private set; }

            /// <summary>
            /// このエントリが地面に置かれたターン（0起算）。
            /// </summary>
            public int DropTurn { get; }

            public Entry(ItemDefinition item, int amount, int dropTurn)
            {
                Item = item;
                Amount = amount > 0 ? amount : 0;
                DropTurn = dropTurn >= 0 ? dropTurn : 0;
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

        private readonly List<Entry> _entries = new List<Entry>();

        public IReadOnlyList<Entry> Entries => _entries;

        public bool IsEmpty => _entries.Count == 0;

        /// <summary>
        /// 表示用の代表アイテム（最古固定）。
        /// </summary>
        public ItemDefinition? RepresentativeItem => _entries.Count > 0 ? _entries[0].Item : null;

        public void Add(ItemDefinition item, int amount, int dropTurn)
        {
            if (item == null || amount <= 0)
            {
                return;
            }

            var remaining = amount;

            // dropTurn 単位で寿命管理するため、まずは同一 dropTurn の既存スタックへ加算する。
            if (item.IsStackable)
            {
                foreach (var entry in _entries)
                {
                    if (entry.Item != item || entry.DropTurn != dropTurn)
                    {
                        continue;
                    }

                    var canAdd = item.MaxStack - entry.Amount;
                    if (canAdd <= 0)
                    {
                        continue;
                    }

                    var add = remaining <= canAdd ? remaining : canAdd;
                    entry.AddAmount(add);
                    remaining -= add;

                    if (remaining <= 0)
                    {
                        return;
                    }
                }
            }

            // 足りない分は新規エントリとして追加する。
            while (remaining > 0)
            {
                var add = item.IsStackable
                    ? (remaining <= item.MaxStack ? remaining : item.MaxStack)
                    : 1;

                _entries.Add(new Entry(item, add, dropTurn));
                remaining -= add;
            }
        }

        /// <summary>
        /// 指定ターンに対して、寿命切れのエントリを取り除く。
        /// </summary>
        public void RemoveExpiredEntries(int currentTurn, int ttlTurns)
        {
            if (_entries.Count == 0 || ttlTurns <= 0 || currentTurn < 0)
            {
                return;
            }

            for (var i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                if (entry.Amount <= 0)
                {
                    _entries.RemoveAt(i);
                    i--;
                    continue;
                }

                var age = currentTurn - entry.DropTurn;
                if (age < ttlTurns)
                {
                    continue;
                }

                _entries.RemoveAt(i);
                i--;
            }
        }

        /// <summary>
        /// 指定アイテムを 1 個取り出す（最古エントリ優先）。
        /// </summary>
        public bool TryTakeOne(ItemDefinition item)
        {
            if (item == null || _entries.Count == 0)
            {
                return false;
            }

            for (var i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                if (entry.Amount <= 0 || entry.Item != item)
                {
                    continue;
                }

                entry.AddAmount(-1);

                if (entry.Amount <= 0)
                {
                    _entries.RemoveAt(i);
                }

                return true;
            }

            return false;
        }
    }
}
