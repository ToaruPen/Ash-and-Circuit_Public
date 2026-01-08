using NUnit.Framework;
using AshNCircuit.Core.Items;
using AshNCircuit.Core.Map;

namespace AshNCircuit.Tests.Core
{
    public class MapManagerPropAndItemPileTests
    {
        [Test]
        public void Prop_CanAddGetRemove_WithOnePerTileConstraint()
        {
            var map = new MapManager(8, 8);
            var prop = new PropInstance(PropDefinition.ChestPropId);

            Assert.That(map.TryAddProp(2, 2, prop), Is.True);

            Assert.That(map.TryGetProp(2, 2, out var got), Is.True);
            Assert.That(got, Is.SameAs(prop));

            // 1マス1つ: 同一座標への追加は失敗する。
            Assert.That(map.TryAddProp(2, 2, new PropInstance(PropDefinition.ChestPropId)), Is.False);

            Assert.That(map.TryRemoveProp(2, 2), Is.True);
            Assert.That(map.TryGetProp(2, 2, out _), Is.False);
        }

        [Test]
        public void ItemPile_MergesStackableItems_PerDropTurn_AndKeepsOldestRepresentative()
        {
            var pile = new ItemPile();
            var arrow = ItemDefinition.WoodenArrow;
            var oil = ItemDefinition.OilBottle;

            pile.Add(arrow, amount: 10, dropTurn: 0);
            Assert.That(pile.Entries.Count, Is.EqualTo(1));
            Assert.That(pile.Entries[0].Item, Is.EqualTo(arrow));
            Assert.That(pile.Entries[0].Amount, Is.EqualTo(10));
            Assert.That(pile.Entries[0].DropTurn, Is.EqualTo(0));
            Assert.That(pile.RepresentativeItem, Is.EqualTo(arrow));

            // 同一 dropTurn の同一アイテムはマージされる。
            pile.Add(arrow, amount: 5, dropTurn: 0);
            Assert.That(pile.Entries.Count, Is.EqualTo(1));
            Assert.That(pile.Entries[0].Amount, Is.EqualTo(15));
            Assert.That(pile.RepresentativeItem, Is.EqualTo(arrow));

            // 別 dropTurn の追加は、寿命管理単位を分けるため別エントリになる（代表は最古固定）。
            pile.Add(oil, amount: 1, dropTurn: 1);
            Assert.That(pile.Entries.Count, Is.EqualTo(2));
            Assert.That(pile.RepresentativeItem, Is.EqualTo(arrow));
        }

        [Test]
        public void ExpireGroundItemPiles_RemovesExpiredEntries_AndRemovesPileWhenEmpty()
        {
            var map = new MapManager(8, 8);
            var pile = new ItemPile();
            pile.Add(ItemDefinition.DirtClod, amount: 1, dropTurn: 0);

            Assert.That(map.TryAddItemPile(3, 3, pile), Is.True);
            Assert.That(map.TryGetItemPile(3, 3, out _), Is.True);

            // 3日(3600 turns)未満は残る。
            map.ExpireGroundItemPiles(currentTurn: 3599);
            Assert.That(map.TryGetItemPile(3, 3, out _), Is.True);

            // 3日(3600 turns)到達で消滅する。
            map.ExpireGroundItemPiles(currentTurn: 3600);
            Assert.That(map.TryGetItemPile(3, 3, out _), Is.False);
        }

        [Test]
        public void ExpireGroundItemPiles_RemovesOnlyExpiredEntries_AndUpdatesRepresentative()
        {
            var map = new MapManager(8, 8);
            var pile = new ItemPile();

            // 古いエントリ（0）と新しいエントリ（100）を同一Pileに入れる。
            pile.Add(ItemDefinition.ShortSword, amount: 1, dropTurn: 0);
            pile.Add(ItemDefinition.WoodenArrow, amount: 1, dropTurn: 100);

            Assert.That(map.TryAddItemPile(4, 4, pile), Is.True);
            Assert.That(pile.RepresentativeItem, Is.EqualTo(ItemDefinition.ShortSword));

            // 現在ターン 3600: dropTurn 0 は期限切れ、dropTurn 100 は残る。
            map.ExpireGroundItemPiles(currentTurn: 3600);

            Assert.That(map.TryGetItemPile(4, 4, out var remaining), Is.True);
            Assert.That(remaining, Is.Not.Null);
            Assert.That(remaining!.Entries.Count, Is.EqualTo(1));
            Assert.That(remaining.Entries[0].Item, Is.EqualTo(ItemDefinition.WoodenArrow));
            Assert.That(remaining.RepresentativeItem, Is.EqualTo(ItemDefinition.WoodenArrow));
        }
    }
}
