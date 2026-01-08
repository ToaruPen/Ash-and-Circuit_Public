using System.Collections.Generic;
using System.Linq;

namespace AshNCircuit.Core.Items
{
    internal sealed class InventoryStorage
    {
        private readonly List<InventoryEntry> _entries = new List<InventoryEntry>();

        public int MaxStacks { get; }

        public IReadOnlyList<InventoryEntry> Entries => _entries;

        public InventoryStorage(int maxStacks)
        {
            MaxStacks = maxStacks > 0 ? maxStacks : 20;
        }

        public IEnumerable<InventoryEntryView> GetEntriesForUi()
        {
            foreach (var entry in _entries)
            {
                var item = entry.Item;
                if (item == null)
                {
                    continue;
                }

                yield return new InventoryEntryView(
                    item.Id,
                    item.DisplayName,
                    entry.Amount);
            }
        }

        public bool TryAdd(ItemDefinition item, int amount)
        {
            if (item == null || amount <= 0)
            {
                return false;
            }

            var remaining = amount;

            if (item.IsStackable)
            {
                foreach (var entry in _entries)
                {
                    if (entry.Item != item)
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
                        return true;
                    }
                }
            }

            while (remaining > 0 && _entries.Count < MaxStacks)
            {
                var add = item.IsStackable
                    ? (remaining <= item.MaxStack ? remaining : item.MaxStack)
                    : 1;

                _entries.Add(new InventoryEntry(item, add));
                remaining -= add;
            }

            return remaining <= 0;
        }

        public bool TryRemove(ItemDefinition item, int amount)
        {
            if (item == null || amount <= 0)
            {
                return false;
            }

            var total = _entries.Where(e => e.Item == item).Sum(e => e.Amount);
            if (total < amount)
            {
                return false;
            }

            var remaining = amount;

            for (var i = 0; i < _entries.Count && remaining > 0; i++)
            {
                var entry = _entries[i];
                if (entry.Item != item)
                {
                    continue;
                }

                if (entry.Amount <= remaining)
                {
                    remaining -= entry.Amount;
                    _entries.RemoveAt(i);
                    i--;
                }
                else
                {
                    entry.AddAmount(-remaining);
                    remaining = 0;
                }
            }

            return true;
        }

        public int GetTotalCount(ItemDefinition item)
        {
            if (item == null)
            {
                return 0;
            }

            return _entries.Where(e => e.Item == item).Sum(e => e.Amount);
        }
    }
}

