using UnityEngine;
using TMPro;
using PoC3.EnemySystem;

namespace PoC3.UISystem
{
    /// <summary>
    /// Manages the UI elements for a single enemy, such as health and defense text.
    /// This component should be on a Canvas that is a child of an Enemy GameObject.
    /// </summary>
    public class EnemyUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI _healthText;
        [SerializeField] private TextMeshProUGUI _defenseText;
        [SerializeField] private TextMeshProUGUI _attackText;

        private Enemy _enemy;

        private void Awake()
        {
            // Find the Enemy component in the parent object
            _enemy = GetComponentInParent<Enemy>();
            if (_enemy == null)
            {
                Debug.LogError("[EnemyUI] Could not find Enemy component in parent objects.", this);
                gameObject.SetActive(false);
                return;
            }
        }

        private void OnEnable()
        {
            // Subscribe to events
            _enemy.OnHealthChanged += UpdateHealth;
            _enemy.OnDefenseChanged += UpdateDefense;
            _enemy.OnAttackChanged += UpdateAttack;
            _enemy.OnDied += HandleEnemyDied;

            // Initial UI update
            UpdateHealth(_enemy.CurrentHealth, _enemy.MaxHealth);
            UpdateDefense(_enemy.CurrentDefense);
            UpdateAttack(_enemy.CurrentAttackDamage);
        }

        private void OnDisable()
        {
            // Unsubscribe from events to prevent memory leaks
            if (_enemy != null)
            {
                _enemy.OnHealthChanged -= UpdateHealth;
                _enemy.OnDefenseChanged -= UpdateDefense;
                _enemy.OnAttackChanged -= UpdateAttack;
                _enemy.OnDied -= HandleEnemyDied;
            }
        }

        private void UpdateHealth(int current, int max)
        {
            if (_healthText != null)
                _healthText.text = $"HP: {current} / {max}";
        }

        private void UpdateDefense(int current)
        {
            if (_defenseText != null)
                _defenseText.text = $"DEF: {current}";
        }

        private void UpdateAttack(int current)
        {
            if (_attackText != null)
                _attackText.text = $"ATK: {current}";
        }

        private void HandleEnemyDied()
        {
            // Hide the UI when the enemy dies
            gameObject.SetActive(false);
        }
    }
}
