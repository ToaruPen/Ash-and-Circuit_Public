using AshNCircuit.Core.Items;

namespace AshNCircuit.Core.Entities
{
    /// <summary>
    /// プレイヤー固有の情報を扱うエンティティクラスの骨組み。
    /// 入力処理の具体実装は Core の ActionSystem および UnityIntegration 層へ委譲していく想定。
    /// </summary>
    public class PlayerEntity : Entity
    {
        /// <summary>
        /// プレイヤーに紐づくインベントリ。
        /// Core 層ではスタック数・所持品一覧のみを管理し、UI は別チケットで扱う。
        /// </summary>
        public Inventory Inventory { get; }

        public PlayerEntity(int x, int y) : base(x, y)
        {
            Inventory = new Inventory();

            // MVP 用の仮ステータス。
            // 将来的には ActorDefinition や装備から値を決定する。
            InitializeStats(maxHp: 10, attack: 4, defense: 1);
        }
    }
}
