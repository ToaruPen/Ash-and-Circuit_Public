using System;
using System.Collections.Generic;
using AshNCircuit.Core.Items;
using AshNCircuit.Core.States;

namespace AshNCircuit.Core.Map
{
    /// <summary>
    /// プロップ（宝箱/家具など）の定義。
    /// JSON-only / fail-fast 運用のため、BootLoader または EditMode SetUpFixture により初期化される。
    /// </summary>
    public sealed class PropDefinition
    {
        public const string ChestPropId = "chest";

        public string Id { get; }

        public string DisplayName { get; }

        public string SpriteId { get; }

        public bool BlocksMovement { get; }

        public bool BlocksLOS { get; }

        public bool BlocksProjectiles { get; }

        private PropDefinition(
            string id,
            string displayName,
            string spriteId,
            bool blocksMovement,
            bool blocksLos,
            bool blocksProjectiles)
        {
            Id = id;
            DisplayName = displayName;
            SpriteId = spriteId;
            BlocksMovement = blocksMovement;
            BlocksLOS = blocksLos;
            BlocksProjectiles = blocksProjectiles;
        }

        public static PropDefinition FromJson(
            string id,
            string displayName,
            string spriteId,
            bool blocksMovement,
            bool blocksLos,
            bool blocksProjectiles)
        {
            if (string.IsNullOrEmpty(spriteId))
            {
                throw new InvalidOperationException($"PropDefinition: sprite_id が空です（id={id}）。");
            }

            return new PropDefinition(
                id,
                displayName,
                spriteId,
                blocksMovement,
                blocksLos,
                blocksProjectiles);
        }

        private static IReadOnlyDictionary<string, PropDefinition>? _definitions;
        private static PropDefinition? _chest;

        public static PropDefinition Chest => _chest
            ?? throw new InvalidOperationException("PropDefinition: 未初期化です（Chest）。BootLoader.InitializePropsFromJson または EditMode SetUpFixture による初期化が必要です。");

        public static IReadOnlyDictionary<string, PropDefinition> Definitions => _definitions
            ?? throw new InvalidOperationException("PropDefinition: 未初期化です（Definitions）。BootLoader.InitializePropsFromJson または EditMode SetUpFixture による初期化が必要です。");

        public static PropDefinition GetByIdOrThrow(string propId)
        {
            if (string.IsNullOrEmpty(propId))
            {
                throw new ArgumentException("PropDefinition: propId が空です。", nameof(propId));
            }

            var definitions = _definitions
                ?? throw new InvalidOperationException("PropDefinition: 未初期化です。BootLoader.InitializePropsFromJson または EditMode SetUpFixture による初期化が必要です。");

            if (!definitions.TryGetValue(propId, out var definition) || definition == null)
            {
                throw new InvalidOperationException($"PropDefinition: 未定義の propId です: {propId}");
            }

            return definition;
        }

        public static void ApplyDefinitions(IReadOnlyDictionary<string, PropDefinition> definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            if (definitions.Count == 0)
            {
                throw new InvalidOperationException("PropDefinition: 定義テーブルが空です。Resources/Props の props_*.json を確認してください。");
            }

            if (!definitions.TryGetValue(ChestPropId, out var chest) || chest == null)
            {
                throw new InvalidOperationException($"PropDefinition: 必須プロップIDが不足しています: {ChestPropId}");
            }

            _definitions = definitions;
            _chest = chest;
        }
    }

    /// <summary>
    /// マップ上に配置されるプロップ（宝箱など）の個体状態。
    /// MVP段階では「MapManager が座標に紐づくデータを保持できる器」を優先し、
    /// 詳細な挙動や中身（コンテナ等）は後続チケットで拡張する。
    /// </summary>
    public sealed class PropInstance
    {
        private readonly PropDefinition _definition;

        private ItemPile? _containerItems;

        public string PropId { get; }

        public string DisplayName => _definition.DisplayName;

        public string SpriteId => _definition.SpriteId;

        public bool BlocksMovement => _definition.BlocksMovement;

        public bool BlocksLOS => _definition.BlocksLOS;

        public bool BlocksProjectiles => _definition.BlocksProjectiles;

        /// <summary>
        /// コンテナ（チェスト等）の中身ロールに使用する個体 seed。
        /// - Open 時に 1 回だけ確定し、以後固定する（順序独立・再現性を優先）。
        /// </summary>
        public ulong? LootSeed { get; private set; }

        public bool IsContainer => PropId == PropDefinition.ChestPropId;

        public bool HasRolledLoot => _containerItems != null;

        public ItemPile? ContainerItems => _containerItems;

        public PropInstance(string propId)
        {
            if (string.IsNullOrEmpty(propId))
            {
                throw new ArgumentException("PropInstance: propId が空です。", nameof(propId));
            }

            _definition = PropDefinition.GetByIdOrThrow(propId);
            PropId = _definition.Id;
        }

        public bool TryEnsureContainerLootRolled(WorldRngState worldRng, int x, int y)
        {
            if (!IsContainer || worldRng == null)
            {
                return false;
            }

            if (_containerItems != null)
            {
                return true;
            }

            var seed = LootSeed ?? RngSeedDerivation.DeriveLootSeed(worldRng, x, y, PropId);
            LootSeed = seed;

            var rng = new RngStream(seed);
            _containerItems = CreateChestLoot(rng);
            return true;
        }

        public bool TryTakeOneFromContainerToInventory(ItemDefinition item, Inventory inventory)
        {
            if (!IsContainer || item == null || inventory == null)
            {
                return false;
            }

            if (_containerItems == null || _containerItems.IsEmpty)
            {
                return false;
            }

            // 先にインベントリへ追加できることを確認する（失敗時にコンテナの中身を変化させないため）。
            if (!inventory.TryAdd(item, 1))
            {
                return false;
            }

            if (!_containerItems.TryTakeOne(item))
            {
                // 想定外（直前の状態からズレた等）。追加した分はロールバックする。
                inventory.TryRemove(item, 1);
                return false;
            }

            return true;
        }

        public bool TryStoreOneFromInventoryToContainer(ItemDefinition item, Inventory inventory)
        {
            if (!IsContainer || item == null || inventory == null)
            {
                return false;
            }

            // 先にインベントリから消費できることを確認する（失敗時にコンテナを変化させないため）。
            if (!inventory.TryRemove(item, 1))
            {
                return false;
            }

            _containerItems ??= new ItemPile();
            _containerItems.Add(item, amount: 1, dropTurn: 0);
            return true;
        }

        private static ItemPile CreateChestLoot(RngStream rng)
        {
            var pile = new ItemPile();
            var roll = rng.NextInt(0, 100);

            if (roll < 50)
            {
                pile.Add(ItemDefinition.DirtClod, amount: 2 + rng.NextInt(0, 3), dropTurn: 0);
            }
            else
            {
                pile.Add(ItemDefinition.OilBottle, amount: 1, dropTurn: 0);
            }

            pile.Add(ItemDefinition.WoodenArrow, amount: 3 + rng.NextInt(0, 5), dropTurn: 0);
            return pile;
        }
    }
}
