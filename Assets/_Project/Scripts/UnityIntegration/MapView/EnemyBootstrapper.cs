using AshNCircuit.Core.Entities;
using AshNCircuit.Core.GameLoop;
using AshNCircuit.Core.Map;
using AshNCircuit.Core.Systems;

namespace AshNCircuit.UnityIntegration.MapView
{
    public sealed class EnemyBootstrapper
    {
        public EnemyAISystem Bootstrap(
            MapManager mapManager,
            ActionSystem actionSystem,
            LogSystem logSystem,
            TurnManager turnManager,
            PlayerEntity? playerEntity)
        {
            var enemyAiSystem = new EnemyAISystem(mapManager, actionSystem, logSystem);

            if (playerEntity != null)
            {
                enemyAiSystem.SetPlayer(playerEntity);
            }

            actionSystem.SetEnemyLocator((x, y) => enemyAiSystem.FindEnemyAt(x, y));
            turnManager.OnEnemyPhase += enemyAiSystem.HandleEnemyPhase;

            var centerX = mapManager.Width / 2;
            var centerY = mapManager.Height / 2;

            var wolf = ActorDefinition.GetByIdOrThrow("actor_forest_wolf");
            var goblin = ActorDefinition.GetByIdOrThrow("actor_goblin");

            var enemy1 = new EnemyEntity(
                centerX + 2,
                centerY,
                maxHp: goblin.Hp,
                attack: goblin.Attack,
                defense: goblin.Defense,
                id: goblin.Id,
                displayName: goblin.DisplayName);

            var enemy2 = new EnemyEntity(
                centerX - 2,
                centerY,
                maxHp: wolf.Hp,
                attack: wolf.Attack,
                defense: wolf.Defense,
                id: wolf.Id,
                displayName: wolf.DisplayName);

            enemyAiSystem.AddEnemy(enemy1);
            enemyAiSystem.AddEnemy(enemy2);

            return enemyAiSystem;
        }
    }
}
