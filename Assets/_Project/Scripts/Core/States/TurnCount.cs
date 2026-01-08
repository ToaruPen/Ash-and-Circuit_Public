namespace AshNCircuit.Core.States
{
    /// <summary>
    /// 「ターン数（残りターンなど）」を表す状態 struct。
    /// - 0 以上の整数を保証する。
    ///
    /// 状態異常の残りターンや環境ギミックの持続など、
    /// 「単なる int だと意味が混ざりやすい状態」を型で明確化するための共通型。
    /// </summary>
    public readonly struct TurnCount
    {
        public int Value { get; }

        public TurnCount(int value)
        {
            Value = value < 0 ? 0 : value;
        }

        public static TurnCount Zero => new TurnCount(0);

        public TurnCount Decrement(int amount = 1)
        {
            if (amount <= 0)
            {
                return this;
            }

            return new TurnCount(Value - amount);
        }
    }
}

