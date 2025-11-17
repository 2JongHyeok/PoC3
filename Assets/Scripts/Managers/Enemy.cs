using UnityEngine;
using System;

namespace PoC3.EnemySystem
{
    /// <summary>
    /// Represents an enemy in the game.
    /// </summary>
    public class Enemy : MonoBehaviour
    {
        [SerializeField]
        private int _currentHealth = 100;
        [SerializeField]
        private int _maxHealth = 100;
        [SerializeField]
        private int _currentDefense = 0;
        [SerializeField]
        private int _baseAttackDamage = 5; // Base damage this enemy deals
        [SerializeField]
        private int _currentAttackDamage = 5;

        [Header("Visuals")]
        [SerializeField] private GameObject _highlightIndicator;
        [SerializeField] private Color _ballColor = Color.red;

        public event Action<int, int> OnHealthChanged; // currentHealth, maxHealth
        public event Action<int> OnDefenseChanged;
        public event Action<int> OnAttackChanged;
        public event Action OnDied;

        public int CurrentHealth => _currentHealth;
        public int MaxHealth => _maxHealth;
        public int CurrentDefense => _currentDefense;
        public int BaseAttackDamage => _baseAttackDamage;
        public int CurrentAttackDamage => _currentAttackDamage;
        public Color BallColor => _ballColor;

        public void AddDefense(int amount)
        {
            if (amount <= 0) return;
            _currentDefense += amount;
            Debug.Log($"[Enemy] {name} gained {amount} defense. Total defense: {_currentDefense}");
            OnDefenseChanged?.Invoke(_currentDefense);
        }

        public void ResetDefense()
        {
            _currentDefense = 0;
            OnDefenseChanged?.Invoke(_currentDefense);
        }

        public void AddHealth(int amount)
        {
            if (amount <= 0) return;
            _currentHealth = Mathf.Min(_currentHealth + amount, _maxHealth);
            Debug.Log($"[Enemy] {name} healed {amount}. Total HP: {_currentHealth}/{_maxHealth}");
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        private void Start()
        {
            _currentAttackDamage = _baseAttackDamage;
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            OnDefenseChanged?.Invoke(_currentDefense);
            OnAttackChanged?.Invoke(_currentAttackDamage);
            if (_highlightIndicator != null)
            {
                _highlightIndicator.SetActive(false);
            }
        }

        /// <summary>
        /// Applies damage to the enemy, first to defense, then to health.
        /// </summary>
        /// <param name="amount">The amount of damage to apply.</param>
        public void TakeDamage(int amount)
        {
            int damageToDefense = Mathf.Min(amount, _currentDefense);
            _currentDefense -= damageToDefense;

            int remainingDamage = amount - damageToDefense;

            _currentHealth -= remainingDamage;
            _currentHealth = Mathf.Max(0, _currentHealth);

            Debug.Log($"[Enemy] {name} took {amount} total damage. {damageToDefense} to defense, {remainingDamage} to health. " +
                      $"Current Stats: HP: {_currentHealth}/{_maxHealth}, DEF: {_currentDefense}");

            OnDefenseChanged?.Invoke(_currentDefense);
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

            if (_currentHealth <= 0)
            {
                Die();
            }
        }

        public void AddAttackDamage(int amount)
        {
            if (amount <= 0) return;
            _currentAttackDamage += amount;
            Debug.Log($"[Enemy] {name} gained {amount} attack. Total attack: {_currentAttackDamage}");
            OnAttackChanged?.Invoke(_currentAttackDamage);
        }

        public void ResetAttackDamage()
        {
            _currentAttackDamage = _baseAttackDamage;
            OnAttackChanged?.Invoke(_currentAttackDamage);
        }

        private void Die()
        {
            Debug.Log($"[Enemy] {name} has been defeated!");
            OnDied?.Invoke();
            // In a real game, you might disable the GameObject, play death animation, etc.
            gameObject.SetActive(false);
        }

        private void OnMouseEnter()
        {
            if (_highlightIndicator != null)
            {
                _highlightIndicator.SetActive(true);
            }
        }

        private void OnMouseExit()
        {
            if (_highlightIndicator != null)
            {
                _highlightIndicator.SetActive(false);
            }
        }

        private void OnEnable()
        {
            ManagerSystem.EnemyManager.Instance?.RegisterEnemy(this);
        }
    }
}
