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

        [Header("Visuals")]
        [SerializeField] private GameObject _highlightIndicator;

        public event Action<int, int> OnHealthChanged; // currentHealth, maxHealth
        public event Action<int> OnDefenseChanged;
        public event Action OnDied;

        public int CurrentHealth => _currentHealth;
        public int MaxHealth => _maxHealth;
        public int CurrentDefense => _currentDefense;
        public int BaseAttackDamage => _baseAttackDamage;

        private void Start()
        {
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            OnDefenseChanged?.Invoke(_currentDefense);
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

        // Usage in Unity:
        // 1. Create an empty GameObject in the Hierarchy, name it "Enemy".
        // 2. Add a SpriteRenderer component and assign a sprite.
        // 3. Add a Collider2D component (e.g., BoxCollider2D) and ensure it's NOT a trigger.
        // 4. Add this Enemy.cs script as a component.
        // 5. Set the GameObject's Tag to "Enemy" (create this tag in Unity if it doesn't exist).
        // 6. Adjust stats in the Inspector.
        // 7. Create a child GameObject (e.g., a Square sprite) for the highlight, and assign it to _highlightIndicator.
    }
}