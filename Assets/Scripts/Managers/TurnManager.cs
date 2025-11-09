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
        
        // Bonus stats accumulated this turn
        private int _accumulatedAttack;
        private int _accumulatedDefense;
        private int _accumulatedHealth;

        public event Action OnTurnStart;
        public event Action OnTurnEnd;
        public event Action<int> OnBallsInHandChanged;
        public event Action<Ball> OnBallLaunched;
        
        public event Action<int> OnAttackAccumulated;
        public event Action<int> OnDefenseAccumulated;
        public event Action<int> OnHealthAccumulated;

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
            _accumulatedAttack = 0;
            _accumulatedDefense = 0;
            _accumulatedHealth = 0;
            OnAttackAccumulated?.Invoke(_accumulatedAttack);
            OnDefenseAccumulated?.Invoke(_accumulatedDefense);
            OnHealthAccumulated?.Invoke(_accumulatedHealth);

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
        /// Calculates the current bonus stats from all launched balls on the board and updates the UI.
        /// </summary>
        public void CalculateCurrentBonuses()
        {
            _accumulatedAttack = 0;
            _accumulatedDefense = 0;
            _accumulatedHealth = 0;

            foreach (Ball ball in GameBoard.ActiveBalls)
            {
                if (!ball.IsLaunched) continue; // Don't count the un-launched ball

                List<Tile> tilesUnderBall = FindTilesUnderBall(ball);
                foreach (Tile tile in tilesUnderBall)
                {
                    if (tile.CurrentTileEffect != null)
                    {
                        int effectValue = tile.ActivateTileEffect(ball.Level);
                        switch (tile.CurrentTileEffect.Type)
                        {
                            case EffectType.Attack:
                                _accumulatedAttack += effectValue;
                                break;
                            case EffectType.Defense:
                                _accumulatedDefense += effectValue;
                                break;
                            case EffectType.Health:
                                _accumulatedHealth += effectValue;
                                break;
                        }
                    }
                }
            }
            
            OnAttackAccumulated?.Invoke(_accumulatedAttack);
            OnDefenseAccumulated?.Invoke(_accumulatedDefense);
            OnHealthAccumulated?.Invoke(_accumulatedHealth);
            Debug.Log($"[TurnManager] Bonuses calculated: ATK+{_accumulatedAttack}, DEF+{_accumulatedDefense}, HEAL+{_accumulatedHealth}");
        }

        /// <summary>
        /// Applies calculated bonuses, deals damage to the enemy, and ends the player's turn.
        /// </summary>
        public void AttackEnemyAndEndTurn(Enemy enemy)
        {
            Debug.Log("[TurnManager] Attacking enemy and ending turn.");
            
            // 1. Apply total damage to enemy (using pre-calculated bonuses)
            int totalDamage = _player.CurrentAttackDamage + _accumulatedAttack;
            Debug.Log($"[TurnManager] Applying total damage: {_player.CurrentAttackDamage} (base) + {_accumulatedAttack} (bonus) = {totalDamage}");
            enemy.TakeDamage(totalDamage);

            // 2. Apply bonus stats to player
            _player.AddDefense(_accumulatedDefense);
            _player.AddHealth(_accumulatedHealth);

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

        private List<Tile> FindTilesUnderBall(Ball ball)
        {
            List<Tile> tiles = new List<Tile>();
            Collider2D[] hits = Physics2D.OverlapCircleAll(ball.transform.position, ball.Radius, LayerMask.GetMask("Tile"));
            
            foreach (Collider2D hit in hits)
            {
                Tile tile = hit.GetComponent<Tile>();
                if (tile != null)
                {
                    tiles.Add(tile);
                }
            }
            return tiles;
        }
    }
}
