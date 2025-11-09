using UnityEngine;
using TMPro;
using PoC3.PlayerSystem;
using PoC3.EnemySystem;
using PoC3.ManagerSystem;

namespace PoC3.UISystem
{
    /// <summary>
    /// Manages all UI elements in the game, such as text displays for stats.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Player Stats UI")]
        [SerializeField] private TextMeshProUGUI _playerHealthText;
        [SerializeField] private TextMeshProUGUI _playerDefenseText;
        // TODO: Add Player Attack Text when player attack stat is implemented

        [Header("Turn Bonus UI")]
        [SerializeField] private TextMeshProUGUI _turnBonusDamageText;
        // TODO: Add Turn Bonus Health/Defense Text when those tile effects are implemented

        [Header("Enemy Stats UI")]
        [SerializeField] private GameObject _enemyStatsPanel;
        [SerializeField] private TextMeshProUGUI _enemyHealthText;
        [SerializeField] private TextMeshProUGUI _enemyDefenseText;
        [SerializeField] private TextMeshProUGUI _enemyAttackText;

        private Enemy _currentTargetedEnemy;

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
            // Subscribe to Player events
            if (Player.Instance != null)
            {
                Player.Instance.OnHealthChanged += UpdatePlayerHealth;
                Player.Instance.OnDefenseChanged += UpdatePlayerDefense;
                // Initial UI update
                UpdatePlayerHealth(Player.Instance.CurrentHealth, Player.Instance.MaxHealth);
                UpdatePlayerDefense(Player.Instance.CurrentDefense);
            }

            // Subscribe to TurnManager events
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnDamageAccumulated += UpdateTurnBonusDamage;
                // Initial UI update
                UpdateTurnBonusDamage(0);
            }

            if (_enemyStatsPanel != null)
            {
                _enemyStatsPanel.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from events to prevent memory leaks
            if (Player.Instance != null)
            {
                Player.Instance.OnHealthChanged -= UpdatePlayerHealth;
                Player.Instance.OnDefenseChanged -= UpdatePlayerDefense;
            }
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnDamageAccumulated -= UpdateTurnBonusDamage;
            }
            if (_currentTargetedEnemy != null)
            {
                UnsubscribeFromEnemyEvents(_currentTargetedEnemy);
            }
        }

        // --- Player UI Methods ---
        private void UpdatePlayerHealth(int current, int max)
        {
            if (_playerHealthText != null)
                _playerHealthText.text = $"Player HP: {current} / {max}";
        }

        private void UpdatePlayerDefense(int current)
        {
            if (_playerDefenseText != null)
                _playerDefenseText.text = $"Player DEF: {current}";
        }

        // --- Turn Bonus UI Methods ---
        private void UpdateTurnBonusDamage(int amount)
        {
            if (_turnBonusDamageText != null)
                _turnBonusDamageText.text = $"Bonus DMG: +{amount}";
        }

        // --- Enemy UI Methods ---
        public void ShowEnemyStats(Enemy enemy)
        {
            if (enemy == null)
            {
                HideEnemyStats();
                return;
            }

            if (_currentTargetedEnemy != enemy)
            {
                if (_currentTargetedEnemy != null)
                {
                    UnsubscribeFromEnemyEvents(_currentTargetedEnemy);
                }
                _currentTargetedEnemy = enemy;
                SubscribeToEnemyEvents(_currentTargetedEnemy);
            }
            
            if (_enemyStatsPanel != null) _enemyStatsPanel.SetActive(true);
            UpdateEnemyHealth(enemy.CurrentHealth, enemy.MaxHealth);
            UpdateEnemyDefense(enemy.CurrentDefense);
            UpdateEnemyAttack(enemy.BaseAttackDamage);
        }

        public void HideEnemyStats()
        {
            if (_enemyStatsPanel != null) _enemyStatsPanel.SetActive(false);
            if (_currentTargetedEnemy != null)
            {
                UnsubscribeFromEnemyEvents(_currentTargetedEnemy);
                _currentTargetedEnemy = null;
            }
        }

        private void SubscribeToEnemyEvents(Enemy enemy)
        {
            enemy.OnHealthChanged += UpdateEnemyHealth;
            enemy.OnDefenseChanged += UpdateEnemyDefense;
            enemy.OnDied += OnTargetEnemyDied;
        }

        private void UnsubscribeFromEnemyEvents(Enemy enemy)
        {
            enemy.OnHealthChanged -= UpdateEnemyHealth;
            enemy.OnDefenseChanged -= UpdateEnemyDefense;
            enemy.OnDied -= OnTargetEnemyDied;
        }

        private void UpdateEnemyHealth(int current, int max)
        {
            if (_enemyHealthText != null)
                _enemyHealthText.text = $"Enemy HP: {current} / {max}";
        }

        private void UpdateEnemyDefense(int current)
        {
            if (_enemyDefenseText != null)
                _enemyDefenseText.text = $"Enemy DEF: {current}";
        }

        private void UpdateEnemyAttack(int amount)
        {
            if (_enemyAttackText != null)
                _enemyAttackText.text = $"Enemy ATK: {amount}";
        }

        private void OnTargetEnemyDied()
        {
            HideEnemyStats();
        }
    }
}
