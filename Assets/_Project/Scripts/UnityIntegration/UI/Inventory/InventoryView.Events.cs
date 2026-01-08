using System;
using AshNCircuit.Core.Items;

namespace AshNCircuit.UnityIntegration.UI.InventoryUi
{
    public sealed partial class InventoryView
    {
        public event Action<ItemDefinition>? OnThrowRequested;
        public event Action<ItemDefinition>? OnDropRequested;
        public event Action<ItemDefinition>? OnExamineRequested;
        public event Action<ItemDefinition>? OnStoreToContainerRequested;
        public event Action<ItemDefinition>? OnTakeFromContainerRequested;
    }
}
