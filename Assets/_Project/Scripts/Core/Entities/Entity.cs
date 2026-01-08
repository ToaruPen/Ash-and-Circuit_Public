using System;
using AshNCircuit.Core.States;

namespace AshNCircuit.Core.Entities
{
    /// <summary>
    /// プレイヤー・敵など全てのエンティティの共通情報を表す基底クラス。
    /// 整数グリッド上の位置と、戦闘用の基本ステータスを管理する。
    /// </summary>
    public class Entity
    {
        private HitPoint _hitPoint;
        private TurnCount _burningRemainingTurns;

        public int X { get; private set; }
        public int Y { get; private set; }

        /// <summary>
        /// 最大HP。0 以下の場合、このエンティティは HP を持たないものとして扱う。
        /// </summary>
        public int MaxHp => _hitPoint.Max;

        /// <summary>
        /// 現在HP。0 のとき、このエンティティは戦闘不能（死亡）扱いとなる。
        /// </summary>
        public int CurrentHp => _hitPoint.Current;

        /// <summary>
        /// 攻撃力。
        /// </summary>
        public int Attack { get; private set; }

        /// <summary>
        /// 防御力。
        /// </summary>
        public int Defense { get; private set; }

        /// <summary>
        /// 戦闘不能かどうか。
        /// MaxHp が 0 の場合は常に false を返す（HP を持たないエンティティ）。
        /// </summary>
        public bool IsDead => _hitPoint.Max > 0 && _hitPoint.Current <= 0;

        /// <summary>
        /// burning 状態異常の残りターン数。
        /// 0 以下の場合、このエンティティは burning ではないものとして扱う。
        /// </summary>
        public int BurningRemainingTurns => _burningRemainingTurns.Value;

        /// <summary>
        /// burning 状態かどうか。
        /// </summary>
        public bool IsBurning => _burningRemainingTurns.Value > 0;

        protected Entity(int x, int y)
        {
            X = x;
            Y = y;
        }

        public void SetPosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// 戦闘用ステータスを初期化する。
        /// </summary>
        /// <param name="maxHp">最大HP。</param>
        /// <param name="attack">攻撃力。</param>
        /// <param name="defense">防御力。</param>
        protected void InitializeStats(int maxHp, int attack, int defense)
        {
            _hitPoint = HitPoint.FromMax(maxHp);
            Attack = attack;
            Defense = defense;
        }

        /// <summary>
        /// 攻撃者の攻撃力と自身の防御力にもとづきダメージを適用する。
        /// ダメージ式: damage = max(1, attacker.atk - target.def)
        /// 戻り値として実際に適用されたダメージ量を返す。
        /// </summary>
        public int ApplyDamageFrom(Entity attacker)
        {
            if (attacker == null)
            {
                return 0;
            }

            // HP を持たない、または既に死亡している場合は何もしない。
            if (MaxHp <= 0 || IsDead)
            {
                return 0;
            }

            var raw = attacker.Attack - Defense;
            var damage = Math.Max(1, raw);

            var newHp = CurrentHp - damage;
            _hitPoint = _hitPoint.WithCurrent(newHp);

            return damage;
        }

        /// <summary>
        /// 素のダメージ値をそのまま HP から減算する。
        /// 状態異常や環境ダメージなど、攻撃力・防御力を介さないダメージ源に使用する。
        /// </summary>
        /// <param name="amount">適用するダメージ量。</param>
        /// <returns>実際に適用されたダメージ量。</returns>
        public int ApplyRawDamage(int amount)
        {
            if (amount <= 0)
            {
                return 0;
            }

            if (MaxHp <= 0 || IsDead)
            {
                return 0;
            }

            var newHp = CurrentHp - amount;
            _hitPoint = _hitPoint.WithCurrent(newHp);
            return amount;
        }

        /// <summary>
        /// burning 状態異常を付与する。
        /// 既に burning 中の場合は、残りターン数がより長い方を採用する。
        /// </summary>
        /// <param name="durationTurns">付与するターン数。</param>
        public void ApplyBurning(int durationTurns)
        {
            if (durationTurns <= 0)
            {
                return;
            }

            if (_burningRemainingTurns.Value < durationTurns)
            {
                _burningRemainingTurns = new TurnCount(durationTurns);
            }
        }

        /// <summary>
        /// burning 状態異常の残りターン数を 1 減らす。
        /// 0 未満にはならない。
        /// </summary>
        public void TickBurningDuration()
        {
            if (_burningRemainingTurns.Value <= 0)
            {
                return;
            }

            _burningRemainingTurns = _burningRemainingTurns.Decrement();
        }

        /// <summary>
        /// burning 状態異常を解除する。
        /// </summary>
        public void ClearBurning()
        {
            _burningRemainingTurns = TurnCount.Zero;
        }
    }
}
