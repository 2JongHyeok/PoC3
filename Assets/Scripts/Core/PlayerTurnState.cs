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

        // We can add sub-states here later (e.g., Aiming, WaitingForBalls)
        private bool _isWaitingForBallsToStop = false;

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
            _isWaitingForBallsToStop = false;
            _turnManager.OnBallLaunched += HandleBallLaunched;
            _turnManager.OnBoardTimerEnded += HandleBoardTimerEnded;
        }

        public void OnUpdate()
        {
            if (_isWaitingForBallsToStop)
            {
                // If we are waiting for balls to stop, check their status
                if (_turnManager.GameBoard.AreAllBallsStopped())
                {
                    Debug.Log("[PlayerTurnState] All balls have stopped. Preparing next ball if timer allows.");
                    _isWaitingForBallsToStop = false;
                    if (_turnManager.CanPlayerLaunch())
                    {
                        _turnManager.PrepareNextBall();
                    }
                }
            }
            else
            {
                // If the timer is finished and we are not waiting, the player can attack.
                if (!_turnManager.IsBoardTimerRunning && Input.GetMouseButtonDown(0))
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
            _turnManager.OnBoardTimerEnded -= HandleBoardTimerEnded;
        }

        private void HandleBallLaunched(Ball ball)
        {
            Debug.Log($"[PlayerTurnState] Ball {ball.name} was launched. Waiting for it to stop.");
            _isWaitingForBallsToStop = true;
        }

        private void HandleBoardTimerEnded()
        {
            _isWaitingForBallsToStop = false;
        }
    }
}
