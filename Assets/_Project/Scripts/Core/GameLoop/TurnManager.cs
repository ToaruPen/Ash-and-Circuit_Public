using System;

namespace AshNCircuit.Core.GameLoop
{
    /// <summary>
    /// ターンの進行とフェーズ順序を管理するクラス。
    /// 1ターン=100TU を基準とし、docs/02_invariants.md / docs/03_combat_and_turns.md で定義された
    /// フェーズ順序に従って各フェーズ用のコールバックを呼び出す。
    /// </summary>
    public class TurnManager
    {
        /// <summary>
        /// 1 ターンあたりの time units（TU）。
        /// </summary>
        public const int TurnTimeUnits = 100;

        private int _currentTurn;
        private int _totalTimeUnits;

        /// <summary>
        /// 現在までに完了したターン数（0 起算）。
        /// </summary>
        public int CurrentTurn => _currentTurn;

        /// <summary>
        /// ゲーム開始から経過した累積 TU。
        /// </summary>
        public int TotalTimeUnits => _totalTimeUnits;

        /// <summary>
        /// プレイヤー行動フェーズ用コールバック。
        /// </summary>
        public event Action? OnPlayerPhase;

        /// <summary>
        /// 投射物の移動・命中処理フェーズ用コールバック。
        /// </summary>
        public event Action? OnProjectilePhase;

        /// <summary>
        /// 環境更新フェーズ用コールバック（炎の伝播など）。
        /// </summary>
        public event Action? OnEnvironmentPhase;

        /// <summary>
        /// 敵 AI フェーズ用コールバック。
        /// </summary>
        public event Action? OnEnemyPhase;

        /// <summary>
        /// 状態異常更新フェーズ用コールバック。
        /// </summary>
        public event Action? OnStatusEffectPhase;

        /// <summary>
        /// 1 ターン分の処理を実行し、ターンカウンタと累積 TU を進める。
        /// フェーズ順序:
        /// 1. プレイヤー行動
        /// 2. 投射物の移動・命中処理
        /// 3. 環境更新
        /// 4. 敵 AI
        /// 5. 状態異常更新
        /// </summary>
        public void AdvanceTurn()
        {
            // フェーズ順序は docs/02_invariants.md / docs/03_combat_and_turns.md に従う。
            OnPlayerPhase?.Invoke();
            OnProjectilePhase?.Invoke();
            OnEnvironmentPhase?.Invoke();
            OnEnemyPhase?.Invoke();
            OnStatusEffectPhase?.Invoke();

            _currentTurn++;
            _totalTimeUnits = _currentTurn * TurnTimeUnits;
        }
    }
}
