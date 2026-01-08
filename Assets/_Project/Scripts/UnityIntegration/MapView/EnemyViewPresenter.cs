using System;
using System.Collections.Generic;
using AshNCircuit.Core.Entities;
using AshNCircuit.Core.Systems;
using AshNCircuit.UnityIntegration.Content;
using UnityEngine;

namespace AshNCircuit.UnityIntegration.MapView
{
    public sealed class EnemyViewPresenter
    {
        private const string SortingLayerCharacters = "Characters";

        private readonly EnemyAISystem _enemyAiSystem;
        private readonly Transform _enemiesParent;
        private readonly SpriteCatalog _spriteCatalog;
        private readonly float _tileSize;

        private readonly Dictionary<EnemyEntity, SpriteRenderer> _renderersByEnemy =
            new Dictionary<EnemyEntity, SpriteRenderer>();

        public EnemyViewPresenter(EnemyAISystem enemyAiSystem, Transform enemiesParent, SpriteCatalog spriteCatalog, float tileSize)
        {
            _enemyAiSystem = enemyAiSystem ?? throw new ArgumentNullException(nameof(enemyAiSystem));
            _enemiesParent = enemiesParent ?? throw new ArgumentNullException(nameof(enemiesParent));
            _spriteCatalog = spriteCatalog ?? throw new ArgumentNullException(nameof(spriteCatalog));
            _tileSize = tileSize;
        }

        public void Refresh()
        {
            var alive = new HashSet<EnemyEntity>();

            foreach (var enemy in _enemyAiSystem.EnumerateEnemies())
            {
                if (enemy == null)
                {
                    continue;
                }

                if (enemy.IsDead)
                {
                    RemoveView(enemy);
                    continue;
                }

                alive.Add(enemy);

                var renderer = GetOrCreateRenderer(enemy);
                SyncPosition(enemy, renderer.transform, _tileSize);
            }

            RemoveMissingEnemies(alive);
        }

        private SpriteRenderer GetOrCreateRenderer(EnemyEntity enemy)
        {
            if (_renderersByEnemy.TryGetValue(enemy, out var renderer) && renderer != null)
            {
                return renderer;
            }

            var actor = ActorDefinition.GetByIdOrThrow(enemy.Id);
            if (string.IsNullOrEmpty(actor.SpriteId))
            {
                throw new InvalidOperationException($"EnemyViewPresenter: sprite_id が空です（actor_id={actor.Id}）。");
            }

            var sprite = _spriteCatalog.GetSpriteOrThrow(actor.SpriteId);

            var enemyObject = new GameObject($"Enemy_{actor.Id}_{enemy.X}_{enemy.Y}");
            enemyObject.transform.SetParent(_enemiesParent, false);

            renderer = enemyObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingLayerName = SortingLayerCharacters;
            renderer.sortingOrder = 0;

            _renderersByEnemy[enemy] = renderer;
            return renderer;
        }

        private static void SyncPosition(EnemyEntity enemy, Transform viewTransform, float tileSize)
        {
            viewTransform.position = new Vector3(enemy.X * tileSize, enemy.Y * tileSize, 0f);
        }

        private void RemoveMissingEnemies(HashSet<EnemyEntity> alive)
        {
            if (_renderersByEnemy.Count == 0)
            {
                return;
            }

            var keys = new List<EnemyEntity>(_renderersByEnemy.Keys);
            foreach (var enemy in keys)
            {
                if (!alive.Contains(enemy))
                {
                    RemoveView(enemy);
                }
            }
        }

        private void RemoveView(EnemyEntity enemy)
        {
            if (!_renderersByEnemy.TryGetValue(enemy, out var renderer) || renderer == null)
            {
                _renderersByEnemy.Remove(enemy);
                return;
            }

            if (renderer.gameObject != null)
            {
                UnityEngine.Object.Destroy(renderer.gameObject);
            }

            _renderersByEnemy.Remove(enemy);
        }
    }
}
