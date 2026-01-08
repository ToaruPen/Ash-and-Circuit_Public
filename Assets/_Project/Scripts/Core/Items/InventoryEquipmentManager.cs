using System.Collections.Generic;

namespace AshNCircuit.Core.Items
{
    internal sealed class InventoryEquipmentManager
    {
        private readonly Dictionary<EquipmentSlot, ItemDefinition?> _equipment = new Dictionary<EquipmentSlot, ItemDefinition?>();

        public InventoryEquipmentManager()
        {
            _equipment[EquipmentSlot.Head] = null;
            _equipment[EquipmentSlot.Body] = null;
            _equipment[EquipmentSlot.MainHand] = null;
            _equipment[EquipmentSlot.OffHand] = null;
            _equipment[EquipmentSlot.Back] = null;
            _equipment[EquipmentSlot.Feet] = null;
        }

        public ItemDefinition? GetEquipped(EquipmentSlot slot)
        {
            return _equipment.TryGetValue(slot, out var item) ? item : null;
        }

        public void SetEquipped(EquipmentSlot slot, ItemDefinition item)
        {
            _equipment[slot] = item;
        }

        public void ClearEquipped(EquipmentSlot slot)
        {
            _equipment[slot] = null;
        }

        public IEnumerable<(EquipmentSlot Slot, ItemDefinition? Item)> GetAllEquipment()
        {
            foreach (var kvp in _equipment)
            {
                yield return (kvp.Key, kvp.Value);
            }
        }
    }
}

