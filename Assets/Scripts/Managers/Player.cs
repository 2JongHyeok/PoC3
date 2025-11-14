using UnityEngine;
using System;

namespace PoC3.PlayerSystem
{
    /// <summary>
    /// Manages the player's stats, such as health.
    /// </summary>
    public class Player : MonoBehaviour
    {
        public static Player Instance { get; private set; }

        [SerializeField]
        private int _currentHealth = 200;
        [SerializeField]
        private int _maxHealth = 200;
        [SerializeField]
        private int _currentDefense = 0;
        [SerializeField]
        private int _currentAttackDamage = 10;
        private int _baseAttackDamage;

        public event Action<int, int> OnHealthChanged; // currentHealth, maxHealth
        public event Action<int> OnDefenseChanged;
        public event Action<int> OnAttackDamageChanged;
        public event Action OnDied;

        public int CurrentHealth => _currentHealth;
        public int MaxHealth => _maxHealth;
        public int CurrentDefense => _currentDefense;
        public int CurrentAttackDamage => _currentAttackDamage;

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
            _baseAttackDamage = _currentAttackDamage;
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            OnDefenseChanged?.Invoke(_currentDefense);
            OnAttackDamageChanged?.Invoke(_currentAttackDamage);
        }

        /// <summary>
        /// Applies damage to the player, first to defense, then to health.
        /// </summary>
        /// <param name="amount">The amount of damage to apply.</param>
        public void TakeDamage(int amount)
        {
            int damageToDefense = Mathf.Min(amount, _currentDefense);
            _currentDefense -= damageToDefense;

            int remainingDamage = amount - damageToDefense;

            _currentHealth -= remainingDamage;
            _currentHealth = Mathf.Max(0, _currentHealth);

            Debug.Log($"[Player] Player took {amount} total damage. {damageToDefense} to defense, {remainingDamage} to health. " +
                      $"Current Stats: HP: {_currentHealth}/{_maxHealth}, DEF: {_currentDefense}");

            OnDefenseChanged?.Invoke(_currentDefense);
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

            if (_currentHealth <= 0)
            {
                Die();
            }
        }

        /// <summary>
        /// Adds health points to the player.
        /// </summary>
        public void AddHealth(int amount)
        {
            if (amount <= 0) return;
            _currentHealth = Mathf.Min(_currentHealth + amount, _maxHealth);
            Debug.Log($"[Player] Healed {amount}. Total health: {_currentHealth}");
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        /// <summary>
        /// Adds defense points to the player.
        /// </summary>
        public void AddDefense(int amount)
        {
            if (amount <= 0) return;
            _currentDefense += amount;
            Debug.Log($"[Player] Gained {amount} defense. Total defense: {_currentDefense}");
            OnDefenseChanged?.Invoke(_currentDefense);
        }

        public void ResetDefense()
        {
            _currentDefense = 0;
            OnDefenseChanged?.Invoke(_currentDefense);
        }

        /// <summary>
        /// Adds attack damage to the player.
        /// </summary>
        public void AddAttackDamage(int amount)
        {
            if (amount <= 0) return;
            _currentAttackDamage += amount;
            Debug.Log($"[Player] Gained {amount} attack damage. Total attack: {_currentAttackDamage}");
            OnAttackDamageChanged?.Invoke(_currentAttackDamage);
        }

        public void ResetAttackDamage()
        {
            _currentAttackDamage = _baseAttackDamage;
            OnAttackDamageChanged?.Invoke(_currentAttackDamage);
        }

        private void Die()
        {
            Debug.Log("[Player] Player has been defeated! Game Over.");
            OnDied?.Invoke();
            // In a real game, you might show a game over screen, etc.
            // For now, just disable the player object.
            gameObject.SetActive(false);
        }

        // Usage in Unity:
        // 1. Create an empty GameObject in the Hierarchy, name it "Player".
        // 2. Add this Player.cs script as a component.
        // 3. This object will now be accessible via Player.Instance.
        // 4. You can add other player-related components here, like attack stats.
    }
}
