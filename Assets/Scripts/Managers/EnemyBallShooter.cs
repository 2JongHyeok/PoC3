using UnityEngine;
using PoC3.BallSystem;
using PoC3.BoardSystem;
using PoC3.ManagerSystem;
using UnityEngine.UI;

namespace PoC3.EnemySystem
{
    /// <summary>
    /// Handles enemy ball spawning and auto-launch logic driven by a charge timer.
    /// </summary>
    public class EnemyBallShooter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameBoard _gameBoard;
        [SerializeField] private Ball _ballPrefab;
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private Slider _chargeSlider;
        [SerializeField] private Enemy _ownerEnemy;

        [Header("Launch Settings")]
        [SerializeField] private float _chargeDuration = 5f;
        [SerializeField] private float _launchForce = 20f;
        [SerializeField] private Color _gizmoColor = Color.magenta;

        private float _chargeTimer;
        private bool _isPaused;

        private void Awake()
        {
            if (_spawnPoint == null)
            {
                _spawnPoint = transform;
            }

            if (_ownerEnemy == null)
            {
                _ownerEnemy = GetComponent<Enemy>();
            }
        }

        private void Start()
        {
            if (_gameBoard == null && TurnManager.Instance != null)
            {
                _gameBoard = TurnManager.Instance.GameBoard;
            }
        }

        private void Update()
        {
            if (_ballPrefab == null || _gameBoard == null || _chargeDuration <= 0f)
            {
                return;
            }

            bool boardRunning = TurnManager.Instance == null || TurnManager.Instance.IsBoardTimerRunning;
            if (_isPaused || !boardRunning)
            {
                return;
            }

            _chargeTimer += Time.deltaTime;
            if (_chargeTimer >= _chargeDuration)
            {
                _chargeTimer = 0f;
                UpdateChargeUI(0f);
                LaunchBall();
            }
            else
            {
                UpdateChargeUI(Mathf.Clamp01(_chargeTimer / _chargeDuration));
            }
        }

        private void LaunchBall()
        {
            Ball newBall = Instantiate(_ballPrefab, _spawnPoint.position, Quaternion.identity);
            newBall.ballType = BallType.Enemy;
            newBall.AssignOwnerEnemy(_ownerEnemy);
            if (_ownerEnemy != null)
            {
                newBall.SetOwnerColor(_ownerEnemy.BallColor);
            }
            _gameBoard.AddBall(newBall);

            Vector2 direction = GetTargetDirection();
            Vector2 force = direction * _launchForce;
            newBall.Launch(force);
        }

        private Vector2 GetTargetDirection()
        {
            Ball closestPlayerBall = null;
            float closestDistance = float.MaxValue;

            foreach (Ball ball in _gameBoard.ActiveBalls)
            {
                if (ball == null || ball.ballType != BallType.Player)
                {
                    continue;
                }

                float distance = ball.transform.position.sqrMagnitude;
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPlayerBall = ball;
                }
            }

            if (closestPlayerBall != null)
            {
                Vector2 directionToBall = (closestPlayerBall.transform.position - _spawnPoint.position).normalized;
                return directionToBall == Vector2.zero ? Vector2.up : directionToBall;
            }

            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            if (randomDirection == Vector2.zero)
            {
                randomDirection = Vector2.up;
            }
            return randomDirection;
        }

        private void UpdateChargeUI(float progress)
        {
            if (_chargeSlider != null)
            {
                _chargeSlider.value = progress;
            }
        }

        public void PauseCharging()
        {
            _isPaused = true;
        }

        public void ResumeCharging()
        {
            _isPaused = false;
        }

        public void ResetCharge()
        {
            _chargeTimer = 0f;
            UpdateChargeUI(0f);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = _gizmoColor;
            Vector3 spawnPosition = _spawnPoint != null ? _spawnPoint.position : transform.position;
            Gizmos.DrawWireSphere(spawnPosition, 0.2f);
            Gizmos.DrawLine(spawnPosition, spawnPosition + Vector3.up * 0.5f);
        }
#endif

        // Usage in Unity:
        // 1. Add this component to an Enemy GameObject.
        // 2. Assign GameBoard (or leave empty to auto-fetch from TurnManager), Ball prefab, and a spawn point transform.
        // 3. Adjust charge duration and launch force per enemy.
        // 4. Scene gizmo indicates the spawn position for quick layout tweaks.
    }
}
