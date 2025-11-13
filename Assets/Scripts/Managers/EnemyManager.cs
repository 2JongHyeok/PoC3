using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using PoC3.EnemySystem;
using System;

namespace PoC3.ManagerSystem
{
    /// <summary>
    /// Manages all active enemies in the game, including their order for attacking.
    /// </summary>
    public class EnemyManager : MonoBehaviour
    {
        public static EnemyManager Instance { get; private set; }

        [SerializeField] private List<Enemy> _preRegisteredEnemies = new List<Enemy>();
        private readonly List<Enemy> _activeEnemies = new List<Enemy>();
        private readonly Dictionary<Enemy, Action> _enemyDeathCallbacks = new Dictionary<Enemy, Action>();

        public event Action<Enemy> OnEnemyRegistered;
        public event Action<Enemy> OnEnemyUnregistered;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
            }
        }

        private void Start()
        {
            // Manual registration: assign enemies in Inspector or call RegisterEnemy elsewhere.
            if (_preRegisteredEnemies == null)
            {
                return;
            }

            foreach (Enemy enemy in _preRegisteredEnemies)
            {
                RegisterEnemy(enemy);
            }
        }

        /// <summary>
        /// Registers an enemy with the manager.
        /// </summary>
        public void RegisterEnemy(Enemy enemy)
        {
            if (enemy == null || _activeEnemies.Contains(enemy))
            {
                return;
            }

            _activeEnemies.Add(enemy);
            Action deathCallback = () => UnregisterEnemy(enemy);
            _enemyDeathCallbacks[enemy] = deathCallback;
            enemy.OnDied += deathCallback;
            OnEnemyRegistered?.Invoke(enemy);
            Debug.Log($"[EnemyManager] Registered enemy: {enemy.name}");
        }

        /// <summary>
        /// Unregisters an enemy from the manager (e.g., when it dies).
        /// </summary>
        private void UnregisterEnemy(Enemy enemy)
        {
            if (!_activeEnemies.Remove(enemy))
            {
                return;
            }

            if (enemy != null && _enemyDeathCallbacks.TryGetValue(enemy, out Action callback))
            {
                enemy.OnDied -= callback;
                _enemyDeathCallbacks.Remove(enemy);
            }

            OnEnemyUnregistered?.Invoke(enemy);
            Debug.Log($"[EnemyManager] Unregistered enemy: {enemy?.name}");
        }

        public void ManualUnregister(Enemy enemy)
        {
            UnregisterEnemy(enemy);
        }

        /// <summary>
        /// Returns a list of active enemies, sorted by their attack order.
        /// (Currently sorted by Y position, then X position for "front to back" logic).
        /// </summary>
        public List<Enemy> GetEnemiesInAttackOrder()
        {
            // User specified "제일 앞에있는 Enemy부터 차례대로 Player를 공격해."
            // Assuming "front" means lower Y position, then lower X position.
            return _activeEnemies.OrderBy(e => e.transform.position.y)
                                 .ThenBy(e => e.transform.position.x)
                                 .ToList();
        }

        /// <summary>
        /// Returns a read-only list of all currently active enemies.
        /// </summary>
        public IReadOnlyList<Enemy> GetAllActiveEnemies()
        {
            return _activeEnemies.AsReadOnly();
        }

        // Usage in Unity:
        // 1. Create an empty GameObject in the Hierarchy, name it "EnemyManager".
        // 2. Add this EnemyManager.cs script as a component.
        // 3. Ensure all Enemy GameObjects in the scene have the Enemy.cs script attached and are tagged "Enemy".
        //    The EnemyManager will automatically find and register them on Start.
    }
}
