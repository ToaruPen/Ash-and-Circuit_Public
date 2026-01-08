using System;
using System.Collections.Generic;

namespace AshNCircuit.Core.Entities
{
    /// <summary>
    /// NPC / 敵アクターの共通定義を表す軽量 DTO。
    /// docs/06_content_schema.md の Actor スキーマに対応する。
    /// </summary>
    public sealed class ActorDefinition
    {
        private static IReadOnlyDictionary<string, ActorDefinition>? _definitions;

        public string Id { get; }
        public string DisplayName { get; }
        public string SpriteId { get; }
        public string Kind { get; }
        public string FactionId { get; }
        public IReadOnlyList<string> Tags { get; }
        public int Hp { get; }
        public int Attack { get; }
        public int Defense { get; }
        public int Speed { get; }
        public string AiProfileId { get; }
        public IReadOnlyList<ActorInventoryEntry> InitialInventory { get; }
        public string Notes { get; }

        private ActorDefinition(
            string id,
            string displayName,
            string spriteId,
            string kind,
            string factionId,
            IReadOnlyList<string> tags,
            int hp,
            int attack,
            int defense,
            int speed,
            string aiProfileId,
            IReadOnlyList<ActorInventoryEntry> initialInventory,
            string notes)
        {
            Id = id;
            DisplayName = displayName;
            SpriteId = spriteId;
            Kind = kind;
            FactionId = factionId;
            Tags = tags;
            Hp = hp;
            Attack = attack;
            Defense = defense;
            Speed = speed;
            AiProfileId = aiProfileId;
            InitialInventory = initialInventory;
            Notes = notes;
        }

        /// <summary>
        /// JSON から読み込んだフィールドをもとに ActorDefinition を生成する。
        /// </summary>
        public static ActorDefinition FromJson(
            string id,
            string displayName,
            string spriteId,
            string kind,
            string factionId,
            IReadOnlyList<string> tags,
            int hp,
            int attack,
            int defense,
            int speed,
            string aiProfileId,
            IReadOnlyList<ActorInventoryEntry> initialInventory,
            string notes)
        {
            return new ActorDefinition(
                id,
                displayName,
                spriteId,
                kind,
                factionId,
                tags ?? new List<string>(),
                hp,
                attack,
                defense,
                speed,
                aiProfileId,
                initialInventory ?? new List<ActorInventoryEntry>(),
                notes);
        }

        /// <summary>
        /// JSON から読み込んだ Actor 定義テーブルを保持する。
        /// JSON-only 運用のため、空テーブルは例外とする。
        /// </summary>
        public static void ApplyDefinitions(IReadOnlyDictionary<string, ActorDefinition> definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            if (definitions.Count == 0)
            {
                throw new InvalidOperationException("ActorDefinition: 定義テーブルが空です。Resources/Actors の Actor JSON を確認してください。");
            }

            _definitions = definitions;
        }

        public static ActorDefinition GetByIdOrThrow(string actorId)
        {
            if (string.IsNullOrEmpty(actorId))
            {
                throw new ArgumentException("ActorDefinition: actorId が空です。", nameof(actorId));
            }

            var definitions = _definitions
                ?? throw new InvalidOperationException("ActorDefinition: 未初期化です。BootLoader.InitializeActorsFromJson または EditMode SetUpFixture による初期化が必要です。");

            if (!definitions.TryGetValue(actorId, out var def) || def == null)
            {
                throw new InvalidOperationException($"ActorDefinition: 未定義の actorId です: {actorId}");
            }

            return def;
        }
    }

    /// <summary>
    /// Actor の初期所持品エントリ。
    /// item_id と数量のみを保持し、実際の ItemDefinition 解決は別レイヤで行う。
    /// </summary>
    public readonly struct ActorInventoryEntry
    {
        public string ItemId { get; }
        public int Count { get; }

        public ActorInventoryEntry(string itemId, int count)
        {
            ItemId = itemId;
            Count = count;
        }
    }
}
