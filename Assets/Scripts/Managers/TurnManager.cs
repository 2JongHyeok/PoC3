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
        [SerializeField] private BoardTimerManager _boardTimer;

        [Header("Prefabs & Spawn")]
        [SerializeField] private Ball _ballPrefab;
        [SerializeField] private Transform _ballSpawnPoint;

        [Header("Turn Settings")]
        [SerializeField] private int _baseAttackDamage = 5;

        [Header("Ball Charge Settings")]
        [SerializeField] private float _ballChargeDuration = 5f;

        private StateMachine _stateMachine;
        private int _currentBallsInHand;
        private float _ballChargeTimer;
        private bool _isChargingBall;
        private bool _playerActionPause;
        public bool IsPlayerActionPaused => _playerActionPause;
        
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
        public event Action<float> OnBallChargeProgress;
        public event Action<float> OnBoardTimerProgress;
        public event Action OnBoardTimerEnded;

        public BoardTimerManager BoardTimer => _boardTimer;
        public bool IsBoardTimerRunning => _boardTimer == null || _boardTimer.IsRunning;

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
            if (_boardTimer != null)
            {
                _boardTimer.OnTimerTick += HandleBoardTimerTick;
                _boardTimer.OnTimerEnded += HandleBoardTimerEnded;
            }
            
            // Initialize and start the first turn
            _stateMachine.Initialize(new PlayerTurnState(this, _stateMachine));
        }

        private void OnDestroy()
        {
            if (_ballLauncher != null)
            {
                _ballLauncher.OnLaunch -= HandleLaunch;
            }

            if (_boardTimer != null)
            {
                _boardTimer.OnTimerTick -= HandleBoardTimerTick;
                _boardTimer.OnTimerEnded -= HandleBoardTimerEnded;
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

            CleanupBoard();
            _currentBallsInHand = 0;
            OnBallsInHandChanged?.Invoke(_currentBallsInHand);
            _player.ResetDefense();
            _player.ResetAttackDamage();
            ResetEnemyDefenseStats();
            ResetEnemyAttackStats();

            _boardTimer?.ResetTimer();
            ResumeAfterPlayerAction();
            OnTurnStart?.Invoke();
            EnsureBallChargeRoutine();
        }

        /// <summary>
        /// Spawns a new ball at the spawn point if the player has balls in hand
        /// and no other ball is ready to be launched.
        /// </summary>
        public void PrepareNextBall(bool ignoreLaunchCheck = false)
        {
            if (!ignoreLaunchCheck && !CanLaunch())
            {
                return;
            }

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
            newBall.gameObject.layer = LayerMask.NameToLayer("ReadyBall"); // Set layer to ReadyBall
            GameBoard.AddBall(newBall);
        }

        /// <summary>
        /// Handles the launch event from the BallLauncher.
        /// </summary>
        private void HandleLaunch(Ball ball, Vector2 force)
        {
            if (ball != null && !ball.IsLaunched && CanLaunch())
            {
                _currentBallsInHand = Mathf.Max(0, _currentBallsInHand - 1);
                OnBallsInHandChanged?.Invoke(_currentBallsInHand);
                
                ball.Launch(force);
                OnBallLaunched?.Invoke(ball);

                if (_currentBallsInHand <= 0)
                {
                    RestartChargeTimer();
                }
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
                if (ball == null || !ball.IsLaunched) continue; // Don't count the un-launched ball

                foreach (Tile tile in ball.TilesInContact)
                {
                    if (tile == null || tile.CurrentTileEffect == null)
                    {
                        continue;
                    }

                    int effectValue = tile.ActivateTileEffect(ball.Level);
                    switch (tile.CurrentTileEffect.Type)
                    {
                        case EffectType.None:
                            break;
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
            int totalDamage = _player.CurrentAttackDamage;
            Debug.Log($"[TurnManager] Applying total damage: {totalDamage}");
            enemy.TakeDamage(totalDamage);

            _player.ResetAttackDamage();

            // 2. Clean up the board
            CleanupBoard();
            OnTurnEnd?.Invoke();

            // 3. Transition to the next state
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
                if (ball == null)
                {
                    continue;
                }

                ball.UseBall();
            }
        }

        private void Update()
        {
            if (_playerActionPause || !_isChargingBall || _ballChargeDuration <= 0f)
            {
                return;
            }

            _ballChargeTimer += Time.deltaTime;
            float progress = Mathf.Clamp01(_ballChargeTimer / _ballChargeDuration);
            OnBallChargeProgress?.Invoke(progress);

            if (_ballChargeTimer >= _ballChargeDuration)
            {
                _ballChargeTimer = 0f;
                GrantChargedBall();
            }
        }

        private void GrantChargedBall()
        {
            _currentBallsInHand++;
            OnBallsInHandChanged?.Invoke(_currentBallsInHand);
            Debug.Log("[TurnManager] Ball charge complete. Granted 1 ball.");
            StopChargeTimer();
            PauseForPlayerAction();
            PrepareNextBall(true);
        }

        private void EnsureBallChargeRoutine()
        {
            if (_playerActionPause)
            {
                StopChargeTimer();
                return;
            }

            if (!CanLaunch())
            {
                StopChargeTimer();
                return;
            }

            if (_currentBallsInHand > 0)
            {
                StopChargeTimer();
                return;
            }

            if (!_isChargingBall)
            {
                StartChargeTimer();
            }
        }

        private void StartChargeTimer()
        {
            if (_ballChargeDuration <= 0f)
            {
                Debug.LogWarning("[TurnManager] Ball charge duration must be greater than zero.");
                return;
            }

            if (_playerActionPause || !CanLaunch())
            {
                return;
            }

            _isChargingBall = true;
            OnBallChargeProgress?.Invoke(Mathf.Clamp01(_ballChargeTimer / _ballChargeDuration));
        }

        private void RestartChargeTimer()
        {
            if (_ballChargeDuration <= 0f)
            {
                Debug.LogWarning("[TurnManager] Ball charge duration must be greater than zero.");
                return;
            }

            if (_playerActionPause || !CanLaunch())
            {
                StopChargeTimer();
                return;
            }

            _ballChargeTimer = 0f;
            _isChargingBall = true;
            OnBallChargeProgress?.Invoke(0f);
        }

        private void StopChargeTimer()
        {
            _isChargingBall = false;
            _ballChargeTimer = 0f;
            OnBallChargeProgress?.Invoke(0f);
        }

        private void ResetEnemyAttackStats()
        {
            foreach (Enemy enemy in _enemyManager.GetAllActiveEnemies())
            {
                enemy?.ResetAttackDamage();
            }
        }

        private void ResetEnemyDefenseStats()
        {
            foreach (Enemy enemy in _enemyManager.GetAllActiveEnemies())
            {
                enemy?.ResetDefense();
            }
        }

        public bool CanPlayerLaunch()
        {
            return CanLaunch();
        }

        public bool PlayerHasReadyBall()
        {
            return GameBoard.ActiveBalls.Any(ball => ball != null && !ball.IsLaunched);
        }

        private bool CanLaunch()
        {
            if (_playerActionPause)
            {
                return true;
            }

            if (_boardTimer == null)
            {
                return true;
            }

            return _boardTimer.IsRunning;
        }

        private void HandleBoardTimerTick(float normalized)
        {
            OnBoardTimerProgress?.Invoke(normalized);
        }

        private void HandleBoardTimerEnded()
        {
            Debug.Log("[TurnManager] Board timer expired. Stopping all launches.");
            StopChargeTimer();
            PauseForPlayerAction();
            ForceStopAllBalls();
            CalculateBoardBuffsForBothSides();
            CleanupBoard();
            OnBoardTimerEnded?.Invoke();
        }

        public void PauseForPlayerAction()
        {
            if (_playerActionPause)
            {
                return;
            }

            _playerActionPause = true;
            StopChargeTimer();
            _boardTimer?.StopTimer();
            foreach (EnemyBallShooter shooter in FindObjectsByType<EnemyBallShooter>(FindObjectsSortMode.None))
            {
                shooter.PauseCharging();
            }
        }

        public void ResumeAfterPlayerAction()
        {
            if (!_playerActionPause)
            {
                return;
            }

            _playerActionPause = false;
            _boardTimer?.ResumeTimer();
            EnsureBallChargeRoutine();
            foreach (EnemyBallShooter shooter in FindObjectsByType<EnemyBallShooter>(FindObjectsSortMode.None))
            {
                shooter.ResumeCharging();
            }
        }

        private void ForceStopAllBalls()
        {
            foreach (Ball ball in GameBoard.ActiveBalls)
            {
                if (ball == null)
                {
                    continue;
                }

                if (!ball.TryGetComponent<Rigidbody2D>(out Rigidbody2D rb))
                {
                    continue;
                }

                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }

        private void CalculateBoardBuffsForBothSides()
        {
            int playerBuffAttack = 0;
            int playerBuffDefense = 0;
            int playerBuffHealth = 0;

            var enemyAttackBuffs = new Dictionary<Enemy, int>();
            var enemyDefenseBuffs = new Dictionary<Enemy, int>();
            var enemyHealthBuffs = new Dictionary<Enemy, int>();

            foreach (Ball ball in GameBoard.ActiveBalls)
            {
                if (ball == null || !ball.IsLaunched)
                {
                    continue;
                }

                foreach (Tile tile in ball.TilesInContact)
                {
                    if (tile == null || tile.CurrentTileEffect == null)
                    {
                        continue;
                    }

                    int effectValue = tile.ActivateTileEffect(ball.Level);
                    switch (tile.CurrentTileEffect.Type)
                    {
                        case EffectType.Attack:
                            if (ball.ballType == BallType.Player)
                            {
                                playerBuffAttack += effectValue;
                            }
                            else
                            {
                                AddEnemyBuff(enemyAttackBuffs, ball.OwnerEnemy, effectValue);
                            }
                            break;
                        case EffectType.Defense:
                            if (ball.ballType == BallType.Player)
                            {
                                playerBuffDefense += effectValue;
                            }
                            else
                            {
                                AddEnemyBuff(enemyDefenseBuffs, ball.OwnerEnemy, effectValue);
                            }
                            break;
                        case EffectType.Health:
                            if (ball.ballType == BallType.Player)
                            {
                                playerBuffHealth += effectValue;
                            }
                            else
                            {
                                AddEnemyBuff(enemyHealthBuffs, ball.OwnerEnemy, effectValue);
                            }
                            break;
                    }
                }
            }

            if (playerBuffDefense > 0)
            {
                _player.AddDefense(playerBuffDefense);
            }

            if (playerBuffHealth > 0)
            {
                _player.AddHealth(playerBuffHealth);
            }

            if (playerBuffAttack > 0)
            {
                _player.AddAttackDamage(playerBuffAttack);
            }

            _accumulatedAttack = 0;
            _accumulatedDefense = 0;
            _accumulatedHealth = 0;
            OnAttackAccumulated?.Invoke(_accumulatedAttack);
            OnDefenseAccumulated?.Invoke(_accumulatedDefense);
            OnHealthAccumulated?.Invoke(_accumulatedHealth);

            ApplyEnemyBuffs(enemyAttackBuffs, (enemy, value) => enemy.AddAttackDamage(value));
            ApplyEnemyBuffs(enemyDefenseBuffs, (enemy, value) => enemy.AddDefense(value));
            ApplyEnemyBuffs(enemyHealthBuffs, (enemy, value) => enemy.AddHealth(value));
        }

        private void AddEnemyBuff(Dictionary<Enemy, int> buffer, Enemy enemy, int amount)
        {
            if (enemy == null || amount == 0)
            {
                return;
            }

            if (buffer.TryGetValue(enemy, out int current))
            {
                buffer[enemy] = current + amount;
            }
            else
            {
                buffer[enemy] = amount;
            }
        }

        private void ApplyEnemyBuffs(Dictionary<Enemy, int> buffer, Action<Enemy, int> applyAction)
        {
            foreach (KeyValuePair<Enemy, int> kvp in buffer)
            {
                Enemy enemy = kvp.Key;
                if (enemy == null)
                {
                    continue;
                }

                applyAction(enemy, kvp.Value);
            }
        }
    }
}
