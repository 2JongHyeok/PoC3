using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PoC3.ManagerSystem;
using PoC3.PlayerSystem;
using PoC3.EnemySystem;

namespace PoC3.Core
{
    /// <summary>
    /// Represents the state where enemies take their turn to attack the player.
    /// </summary>
    public class EnemyTurnState : IState
    {
        private readonly TurnManager _turnManager;
        private readonly StateMachine _stateMachine;
        private readonly EnemyManager _enemyManager;
        private readonly Player _player;

        public EnemyTurnState(TurnManager turnManager, StateMachine stateMachine, EnemyManager enemyManager, Player player)
        {
            _turnManager = turnManager;
            _stateMachine = stateMachine;
            _enemyManager = enemyManager;
            _player = player;
        }

        public void OnEnter()
        {
            Debug.Log("[State] Entering EnemyTurnState");
            _turnManager.StartCoroutine(ExecuteAttackSequence());
        }

        public void OnUpdate()
        {
            // Logic is handled by the coroutine
        }

        public void OnExit()
        {
            Debug.Log("[State] Exiting EnemyTurnState");
        }

        private IEnumerator ExecuteAttackSequence()
        {
            // Wait a brief moment before enemies start attacking
            yield return new WaitForSeconds(0.5f);

            List<Enemy> enemies = _enemyManager.GetEnemiesInAttackOrder();
            Debug.Log($"[EnemyTurnState] {enemies.Count} enemies will attack.");

            foreach (Enemy enemy in enemies)
            {
                if (enemy != null && enemy.gameObject.activeInHierarchy)
                {
                    Debug.Log($"[EnemyTurnState] {enemy.name} is attacking the player.");
                    // TODO: Add attack animation/visual effect here
                    _player.TakeDamage(enemy.BaseAttackDamage);
                    
                    // Wait before the next enemy attacks
                    yield return new WaitForSeconds(0.5f);
                }
            }

            // Wait 1 second after the last enemy's attack, as requested
            Debug.Log("[EnemyTurnState] All enemies have attacked. Waiting 1 second before player's turn.");
            yield return new WaitForSeconds(1.0f);

            // Transition back to Player's turn
            _stateMachine.ChangeState(new PlayerTurnState(_turnManager, _stateMachine));
        }
    }
}
