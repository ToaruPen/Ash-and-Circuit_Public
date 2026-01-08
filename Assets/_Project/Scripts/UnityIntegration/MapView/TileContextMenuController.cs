using System;
using System.Collections.Generic;
using AshNCircuit.Core.Entities;
using AshNCircuit.Core.GameLoop;
using AshNCircuit.Core.Items;
using AshNCircuit.Core.Map;
using AshNCircuit.Core.Systems;
using AshNCircuit.Core.States;
using AshNCircuit.UnityIntegration.UI.ContextMenuUi;
using AshNCircuit.UnityIntegration.UI.InventoryUi;

namespace AshNCircuit.UnityIntegration.MapView
{
    /// <summary>
    /// タイルを対象とした右クリックコンテキストメニューの項目構築とアクション実行を担当するコントローラ。
    /// </summary>
    public sealed class TileContextMenuController
    {
        private readonly MapManager _mapManager;
        private readonly InventoryView _inventoryView;
        private readonly LogSystem _logSystem;
        private readonly GameController _gameController;
        private readonly WorldRngState _worldRng;
        private readonly Func<int, int, EnemyEntity?>? _findEnemyAt;

        public TileContextMenuController(
            MapManager mapManager,
            InventoryView inventoryView,
            LogSystem logSystem,
            GameController gameController,
            WorldRngState worldRng,
            Func<int, int, EnemyEntity?>? findEnemyAt = null)
        {
            _mapManager = mapManager;
            _inventoryView = inventoryView;
            _logSystem = logSystem;
            _gameController = gameController;
            _worldRng = worldRng ?? throw new ArgumentNullException(nameof(worldRng));
            _findEnemyAt = findEnemyAt;
        }

        public bool HasLivingEnemyAt(GridPosition tile)
        {
            if (_findEnemyAt == null)
            {
                return false;
            }

            var enemy = _findEnemyAt(tile.X, tile.Y);
            return enemy != null && !enemy.IsDead;
        }

        public List<ContextMenuItem> BuildContextMenuItems(GridPosition targetTile, GridPosition playerPos)
        {
            var items = new List<ContextMenuItem>();

            if (_mapManager == null)
            {
                return items;
            }

            var enemy = _findEnemyAt?.Invoke(targetTile.X, targetTile.Y);
            if (enemy != null && enemy.IsDead)
            {
                enemy = null;
            }

            _mapManager.TryGetProp(targetTile.X, targetTile.Y, out var prop);
            var solid = _mapManager.GetSolidType(targetTile.X, targetTile.Y);
            _mapManager.TryGetItemPile(targetTile.X, targetTile.Y, out var pile);
            var hasPile = pile != null && !pile.IsEmpty;

            // ヘッダ（最高優先度の対象）
            var headerLabel = BuildHeaderLabel(targetTile, enemy, prop, solid, pile);
            if (!string.IsNullOrEmpty(headerLabel))
            {
                items.Add(new ContextMenuItem(headerLabel, () => { }, isEnabled: false));
            }

            var deltaX = targetTile.X - playerPos.X;
            var deltaY = targetTile.Y - playerPos.Y;
            var absDx = Math.Abs(deltaX);
            var absDy = Math.Abs(deltaY);
            var isInPickupRange = absDx <= 1 && absDy <= 1;
            var isInMeleeRange = absDx + absDy == 1;

            // 拾う（ItemPile のみ / 常にリスト）
            if (hasPile && isInPickupRange)
            {
                var pickupList = BuildPickupListMenuItems(targetTile, pile!);
                if (pickupList.Count > 0)
                {
                    items.Add(new ContextMenuItem(
                        "拾う",
                        () => { },
                        subMenuItems: pickupList));
                }
            }

            // 攻撃する（近接のみ / 上下左右の隣接1マスのみ）
            if (enemy != null && isInMeleeRange)
            {
                items.Add(new ContextMenuItem(
                    "攻撃する",
                    () =>
                    {
                        _gameController.QueueMove(deltaX, deltaY);
                        _gameController.AdvanceTurn();
                    }));
            }

            // 開く（コンテナのみ）
            if (prop != null && prop.IsContainer && isInPickupRange)
            {
                items.Add(new ContextMenuItem(
                    "開く",
                    () =>
                    {
                        if (prop.TryEnsureContainerLootRolled(_worldRng, targetTile.X, targetTile.Y))
                        {
                            _inventoryView.OpenContainer(prop, targetTile);
                            _gameController.AdvanceTurn();
                        }
                    }));
            }

            // 調べる（Inspect）
            items.Add(new ContextMenuItem(
                BuildUiText(MessageId.UiTileMenuExamine),
                () => { },
                subMenuItems: BuildInspectMenuItems(targetTile, enemy, prop, solid, pile)));

            // 閉じる（常に表示）
            items.Add(new ContextMenuItem("閉じる", () => { }));

            return items;
        }

        private string BuildHeaderLabel(
            GridPosition targetTile,
            EnemyEntity? enemy,
            PropInstance? prop,
            TileType? solid,
            ItemPile? pile)
        {
            if (enemy != null)
            {
                return enemy.DisplayName;
            }

            if (prop != null)
            {
                return prop.DisplayName;
            }

            if (solid.HasValue)
            {
                return GetTileDescription(solid.Value);
            }

            if (pile != null && !pile.IsEmpty && pile.RepresentativeItem != null)
            {
                return pile.RepresentativeItem.DisplayName;
            }

            var groundType = _mapManager.GetGroundType(targetTile.X, targetTile.Y);
            return GetTileDescription(groundType);
        }

        private List<ContextMenuItem> BuildPickupListMenuItems(GridPosition targetTile, ItemPile pile)
        {
            var totals = new Dictionary<ItemDefinition, int>();
            var orderedItems = new List<ItemDefinition>();

            foreach (var entry in pile.Entries)
            {
                if (entry.Amount <= 0)
                {
                    continue;
                }

                if (!totals.ContainsKey(entry.Item))
                {
                    totals[entry.Item] = 0;
                    orderedItems.Add(entry.Item);
                }

                totals[entry.Item] += entry.Amount;
            }

            var items = new List<ContextMenuItem>();

            foreach (var item in orderedItems)
            {
                var totalAmount = totals[item];
                var label = totalAmount > 1
                    ? $"{item.DisplayName} ×{totalAmount}"
                    : item.DisplayName;

                items.Add(new ContextMenuItem(
                    label,
                    () => ExecutePickupFromItemPile(targetTile, item, equipAfterPickup: false),
                    rightClickSubMenuItems: BuildItemContextMenuItems(targetTile, item)));
            }

            return items;
        }

        private List<ContextMenuItem> BuildItemContextMenuItems(GridPosition targetTile, ItemDefinition item)
        {
            var items = new List<ContextMenuItem>
            {
                new ContextMenuItem(
                    "拾う",
                    () => ExecutePickupFromItemPile(targetTile, item, equipAfterPickup: false))
            };

            var equippableSlot = EquipmentSlotUtility.GetEquippableSlot(item);
            if (equippableSlot.HasValue)
            {
                var slotName = EquipmentSlotUtility.GetDisplayName(equippableSlot.Value);
                items.Add(new ContextMenuItem(
                    BuildUiText(MessageId.UiInventoryMenuEquipWithSlot, slotName),
                    () => ExecutePickupFromItemPile(targetTile, item, equipAfterPickup: true)));
            }

            items.Add(new ContextMenuItem(
                BuildUiText(MessageId.UiInventoryMenuExamine),
                () => { },
                subMenuItems: BuildItemInspectMenuItems(item)));

            return items;
        }

        private void ExecutePickupFromItemPile(GridPosition targetTile, ItemDefinition item, bool equipAfterPickup)
        {
            if (equipAfterPickup)
            {
                _gameController.QueuePickupAndEquipFromItemPile(item, targetTile);
            }
            else
            {
                _gameController.QueuePickupFromItemPile(item, targetTile);
            }

            _gameController.AdvanceTurn();
            _inventoryView?.Refresh();
        }

        private List<ContextMenuItem> BuildInspectMenuItems(
            GridPosition targetTile,
            EnemyEntity? enemy,
            PropInstance? prop,
            TileType? solid,
            ItemPile? pile)
        {
            if (enemy != null)
            {
                return BuildEntityInspectMenuItems(
                    name: enemy.DisplayName,
                    attack: enemy.Attack,
                    defense: enemy.Defense,
                    description: "敵",
                    statusText: enemy.IsBurning ? "燃焼" : "なし",
                    tagsText: "なし");
            }

            if (prop != null)
            {
                return BuildEntityInspectMenuItems(
                    name: prop.DisplayName,
                    attack: null,
                    defense: null,
                    description: prop.PropId,
                    statusText: "なし",
                    tagsText: "なし");
            }

            if (solid.HasValue)
            {
                return BuildTileInspectMenuItems(solid.Value);
            }

            if (pile != null && !pile.IsEmpty && pile.RepresentativeItem != null)
            {
                return BuildItemInspectMenuItems(pile.RepresentativeItem);
            }

            var groundType = _mapManager.GetGroundType(targetTile.X, targetTile.Y);
            return BuildTileInspectMenuItems(groundType);
        }

        private static List<ContextMenuItem> BuildTileInspectMenuItems(TileType tileType)
        {
            var desc = GetTileDescription(tileType);
            var tagsText = FormatTileTags(tileType);

            return BuildEntityInspectMenuItems(
                name: desc,
                attack: null,
                defense: null,
                description: desc,
                statusText: "なし",
                tagsText: tagsText);
        }

        private static List<ContextMenuItem> BuildItemInspectMenuItems(ItemDefinition item)
        {
            var tagsText = item.Tags.Count > 0 ? string.Join(", ", item.Tags) : "なし";

            return BuildEntityInspectMenuItems(
                name: item.DisplayName,
                attack: null,
                defense: null,
                description: item.Description,
                statusText: "なし",
                tagsText: tagsText);
        }

        private static List<ContextMenuItem> BuildEntityInspectMenuItems(
            string name,
            int? attack,
            int? defense,
            string description,
            string statusText,
            string tagsText)
        {
            var atkText = attack.HasValue ? attack.Value.ToString() : "-";
            var defText = defense.HasValue ? defense.Value.ToString() : "-";

            return new List<ContextMenuItem>
            {
                new ContextMenuItem(name, () => { }, isEnabled: false),
                new ContextMenuItem($"攻撃: {atkText} / 防御: {defText}", () => { }, isEnabled: false),
                new ContextMenuItem(description ?? string.Empty, () => { }, isEnabled: false),
                new ContextMenuItem($"状態異常: {statusText}", () => { }, isEnabled: false),
                new ContextMenuItem("品質: 無傷", () => { }, isEnabled: false),
                new ContextMenuItem($"タグ: {tagsText}", () => { }, isEnabled: false),
            };
        }

        private static string FormatTileTags(TileType tileType)
        {
            var tags = MapManager.GetTags(tileType);
            if (tags == TileTag.None)
            {
                return "なし";
            }

            var parts = new List<string>();
            foreach (TileTag flag in Enum.GetValues(typeof(TileTag)))
            {
                if (flag == TileTag.None)
                {
                    continue;
                }

                if ((tags & flag) != 0)
                {
                    parts.Add(flag.ToString());
                }
            }

            return parts.Count > 0 ? string.Join(", ", parts) : "なし";
        }

        private static string GetTileDescription(TileType tileType)
        {
            return BuildUiText(GetTileDescriptionMessageId(tileType));
        }

        private static MessageId GetTileDescriptionMessageId(TileType tileType)
        {
            switch (tileType)
            {
                case TileType.GroundNormal:
                    return MessageId.UiTileDescGroundNormal;
                case TileType.GroundBurnt:
                    return MessageId.UiTileDescGroundBurnt;
                case TileType.GroundOil:
                    return MessageId.UiTileDescGroundOil;
                case TileType.GroundWater:
                    return MessageId.UiTileDescGroundWater;
                case TileType.TreeNormal:
                    return MessageId.UiTileDescTreeNormal;
                case TileType.TreeBurning:
                    return MessageId.UiTileDescTreeBurning;
                case TileType.TreeBurnt:
                    return MessageId.UiTileDescTreeBurnt;
                case TileType.WallStone:
                    return MessageId.UiTileDescWallStone;
                case TileType.WallMetal:
                    return MessageId.UiTileDescWallMetal;
                case TileType.FireTile:
                    return MessageId.UiTileDescFire;
                default:
                    return MessageId.UiTileDescUnknown;
            }
        }

        private static string BuildUiText(MessageId id, params object[] args)
        {
            return MessageCatalog.Format(id, args);
        }
    }
}
