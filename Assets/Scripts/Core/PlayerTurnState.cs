using UnityEngine;
using PoC3.ManagerSystem;
using PoC3.BallSystem;
using PoC3.EnemySystem;

namespace PoC3.Core
{
    /// <summary>
    /// Represents the state where it is the player's turn to act.
    /// </summary>
    public class PlayerTurnState : IState
    {
        private readonly TurnManager _turnManager;
        private readonly StateMachine _stateMachine;
        private readonly Camera _mainCamera;

        private bool _isWaitingForBallsToStop = false;
        private float _stopTimer = 0f;
        private const float TIME_TO_CONSIDER_STOPPED = 0.5f;

        public PlayerTurnState(TurnManager turnManager, StateMachine stateMachine)
        {
            _turnManager = turnManager;
            _stateMachine = stateMachine;
            _mainCamera = Camera.main;
        }

        public void OnEnter()
        {
            Debug.Log("[State] Entering PlayerTurnState");
            _turnManager.PrepareNewTurn();
            _turnManager.PrepareNextBall(); // Automatically prepare one ball for the player
            _isWaitingForBallsToStop = false;
            _stopTimer = 0f;
            _turnManager.OnBallLaunched += HandleBallLaunched;
        }

        public void OnUpdate()
        {
            if (_isWaitingForBallsToStop)
            {
                // Check if balls are stopped
                if (_turnManager.GameBoard.AreAllBallsStopped())
                {
                    // If they are, start a timer
                    _stopTimer += Time.deltaTime;
                    if (_stopTimer >= TIME_TO_CONSIDER_STOPPED)
                    {
                        // If they have been stopped for long enough, proceed
                        Debug.Log("[PlayerTurnState] All balls have fully stopped.");
                        _isWaitingForBallsToStop = false;
                        _stopTimer = 0f;
                        
                        _turnManager.CalculateCurrentBonuses();
                        _turnManager.PrepareNextBall();
                    }
                }
                else
                {
                    // If any ball moves, reset the timer
                    _stopTimer = 0f;
                }
            }
            else
            {
                // If we are not waiting, the player can attack an enemy to end the turn.
                if (Input.GetMouseButtonDown(0))
                {
                    CheckForEnemyClick();
                }
            }
        }

        private void CheckForEnemyClick()
        {
            Vector3 mousePosition = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(new Vector2(mousePosition.x, mousePosition.y), Vector2.zero);

            if (hit.collider != null && hit.collider.CompareTag("Enemy"))
            {
                Enemy enemy = hit.collider.GetComponent<Enemy>();
                if (enemy != null)
                {
                    Debug.Log($"[PlayerTurnState] Player clicked on enemy: {enemy.name}. Attacking and ending turn.");
                    _turnManager.AttackEnemyAndEndTurn(enemy);
                    // The AttackEnemyAndEndTurn method will be responsible for changing the state
                }
            }
        }

        public void OnExit()
        {
            Debug.Log("[State] Exiting PlayerTurnState");
            _turnManager.OnBallLaunched -= HandleBallLaunched;
        }

        private void HandleBallLaunched(Ball ball)
        {
            Debug.Log($"[PlayerTurnState] Ball {ball.name} was launched. Waiting for it to stop.");
            _isWaitingForBallsToStop = true;
        }
    }
}
