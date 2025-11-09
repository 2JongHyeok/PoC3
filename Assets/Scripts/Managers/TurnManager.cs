using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using PoC3.Core;
using PoC3.BoardSystem;
using PoC3.BallSystem;
using PoC3.TileSystem;
using PoC3.EnemySystem;
using PoC3.PlayerSystem;

namespace PoC3.ManagerSystem
{
    /// <summary>
    /// Manages the overall turn-based flow of the game according to the new confirmed rules.
    /// </summary>
    public class TurnManager : MonoBehaviour
    {
        public static TurnManager Instance { get; private set; }

        [Header("Component References")]
        [SerializeField] public GameBoard GameBoard;
        [SerializeField] private BallLauncher _ballLauncher;
        [SerializeField] private EnemyManager _enemyManager;
        [SerializeField] private Player _player;

        [Header("Prefabs & Spawn")]
        [SerializeField] private Ball _ballPrefab;
        [SerializeField] private Transform _ballSpawnPoint;

        [Header("Turn Settings")]
        [SerializeField] private int _initialBalls = 3;
        [SerializeField] private int _ballsPerTurn = 1;
        [SerializeField] private int _baseAttackDamage = 5;

        private StateMachine _stateMachine;
        private int _currentBallsInHand;
        private int _accumulatedDamageThisTurn;

        public event Action OnTurnStart;
        public event Action OnTurnEnd;
        public event Action<int> OnBallsInHandChanged;
        public event Action<int> OnDamageAccumulated;
        public event Action<Ball> OnBallLaunched;

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

            _stateMachine = GetComponent<StateMachine>();
        }

        private void Start()
        {
            // Null checks for required components
            if (GameBoard == null || _ballLauncher == null || _enemyManager == null || _player == null || _ballPrefab == null || _ballSpawnPoint == null || _stateMachine == null)
            {
                Debug.LogError("[TurnManager] One or more required components are not assigned in the Inspector!");
                this.enabled = false;
                return;
            }

            _ballLauncher.OnLaunch += HandleLaunch;
            
            // Initialize and start the first turn
            _stateMachine.Initialize(new PlayerTurnState(this, _stateMachine));
        }

        private void OnDestroy()
        {
            if (_ballLauncher != null)
            {
                _ballLauncher.OnLaunch -= HandleLaunch;
            }
        }

        /// <summary>
        /// Prepares the manager for a new player turn.
        /// </summary>
        public void PrepareNewTurn()
        {
            Debug.Log("[TurnManager] Preparing new turn.");
            _accumulatedDamageThisTurn = 0;
            OnDamageAccumulated?.Invoke(_accumulatedDamageThisTurn);

            // If it's the very first turn, initialize with initial balls. Otherwise, add balls per turn.
            if (_currentBallsInHand == 0) 
            {
                _currentBallsInHand = _initialBalls;
            }
            else
            {
                _currentBallsInHand += _ballsPerTurn;
            }
            
            OnTurnStart?.Invoke();
            OnBallsInHandChanged?.Invoke(_currentBallsInHand);
        }

        /// <summary>
        /// Spawns a new ball at the spawn point if the player has balls in hand
        /// and no other ball is ready to be launched.
        /// </summary>
        public void PrepareNextBall()
        {
            if (_currentBallsInHand <= 0)
            {
                Debug.Log("[TurnManager] No balls left in hand.");
                return;
            }

            // Check if a launchable ball already exists
            bool launchableBallExists = GameBoard.ActiveBalls.Any(ball => !ball.IsLaunched);
            if (launchableBallExists)
            {
                Debug.Log("[TurnManager] A ball is already prepared for launch.");
                return;
            }
            
            Debug.Log("[TurnManager] Preparing next ball.");
            Ball newBall = Instantiate(_ballPrefab, _ballSpawnPoint.position, Quaternion.identity);
            GameBoard.AddBall(newBall);
        }

        /// <summary>
        /// Handles the launch event from the BallLauncher.
        /// </summary>
        private void HandleLaunch(Ball ball, Vector2 force)
        {
            if (ball != null && !ball.IsLaunched)
            {
                _currentBallsInHand--;
                OnBallsInHandChanged?.Invoke(_currentBallsInHand);
                
                ball.Launch(force);
                OnBallLaunched?.Invoke(ball);
            }
        }

        /// <summary>
        /// Calculates damage, applies it to the enemy, and ends the player's turn.
        /// </summary>
        public void AttackEnemyAndEndTurn(Enemy enemy)
        {
            Debug.Log("[TurnManager] Attacking enemy and ending turn.");
            
            // 1. Calculate damage from tiles
            _accumulatedDamageThisTurn = 0; // Reset before recalculating
            foreach (Ball ball in GameBoard.ActiveBalls)
            {
                if (!ball.IsLaunched) continue; // Don't count the un-launched ball

                Tile tileUnderBall = FindTileUnderBall(ball); 
                if (tileUnderBall != null && tileUnderBall.CurrentTileEffect != null)
                {
                    _accumulatedDamageThisTurn += tileUnderBall.ActivateTileEffect(ball.Level);
                }
            }
            OnDamageAccumulated?.Invoke(_accumulatedDamageThisTurn);
            
            // 2. Apply total damage to enemy
            int totalDamage = _baseAttackDamage + _accumulatedDamageThisTurn;
            Debug.Log($"[TurnManager] Applying total damage: {_baseAttackDamage} (base) + {_accumulatedDamageThisTurn} (tiles) = {totalDamage}");
            enemy.TakeDamage(totalDamage);

            // 3. Clean up the board
            CleanupBoard();
            OnTurnEnd?.Invoke();

            // 4. Transition to the next state
            _stateMachine.ChangeState(new EnemyTurnState(this, _stateMachine, _enemyManager, _player));
        }

        /// <summary>
        /// Removes all ball GameObjects from the board.
        /// </summary>
        private void CleanupBoard()
        {
            Debug.Log("[TurnManager] Cleaning up board.");
            // Create a copy as the collection will be modified during iteration
            List<Ball> ballsToClean = new List<Ball>(GameBoard.ActiveBalls);
            foreach (Ball ball in ballsToClean)
            {
                ball.UseBall();
            }
        }

        private Tile FindTileUnderBall(Ball ball)
        {
            RaycastHit2D hit = Physics2D.Raycast(ball.transform.position, Vector2.zero, 0f, LayerMask.GetMask("Tile"));
            if (hit.collider != null)
            {
                return hit.collider.GetComponent<Tile>();
            }
            return null;
        }
    }
}
