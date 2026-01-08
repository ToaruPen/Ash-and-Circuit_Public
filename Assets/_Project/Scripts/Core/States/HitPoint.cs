namespace AshNCircuit.Core.States
{
    /// <summary>
    /// HP（最大値と現在値）を表す状態 struct。
    /// - Max は 0 以上。
    /// - Current は 0..Max の範囲に収まる。
    ///
    /// かめさいこお（型/命名/Scope/依存/コピー禁止/置き場）の入口として、
    /// HP を専用型で表現し、意味と不変条件をコード上で明確化する。
    /// </summary>
    public readonly struct HitPoint
    {
        public int Max { get; }
        public int Current { get; }

        public HitPoint(int max, int current)
        {
            if (max < 0)
            {
                max = 0;
            }

            if (current < 0)
            {
                current = 0;
            }

            if (current > max)
            {
                current = max;
            }

            Max = max;
            Current = current;
        }

        public static HitPoint FromMax(int max)
        {
            return new HitPoint(max, max);
        }

        public HitPoint WithCurrent(int current)
        {
            return new HitPoint(Max, current);
        }
    }
}

