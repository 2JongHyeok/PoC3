using UnityEngine;
using TMPro;
using UnityEngine.UI;
using PoC3.PlayerSystem;
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
        [SerializeField] private TextMeshProUGUI _playerAttackText;

        [Header("Turn Bonus UI")]
        [SerializeField] private TextMeshProUGUI _turnBonusAttackText;
        [SerializeField] private TextMeshProUGUI _turnBonusDefenseText;
        [SerializeField] private TextMeshProUGUI _turnBonusHealthText;

        [Header("Turn Info UI")]
        [SerializeField] private TextMeshProUGUI _ballsInHandText;
        [SerializeField] private Slider _ballChargeSlider;
        [SerializeField] private Slider _boardTimerSlider;

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
                Player.Instance.OnAttackDamageChanged += UpdatePlayerAttack;
                // Initial UI update
                UpdatePlayerHealth(Player.Instance.CurrentHealth, Player.Instance.MaxHealth);
                UpdatePlayerDefense(Player.Instance.CurrentDefense);
                UpdatePlayerAttack(Player.Instance.CurrentAttackDamage);
            }

            // Subscribe to TurnManager events
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnAttackAccumulated += UpdateTurnBonusAttack;
                TurnManager.Instance.OnDefenseAccumulated += UpdateTurnBonusDefense;
                TurnManager.Instance.OnHealthAccumulated += UpdateTurnBonusHealth;
                TurnManager.Instance.OnBallsInHandChanged += UpdateBallsInHand;
                TurnManager.Instance.OnBallChargeProgress += UpdateBallChargeSlider;
                TurnManager.Instance.OnBoardTimerProgress += UpdateBoardTimerSlider;
                // Initial UI update
                UpdateTurnBonusAttack(0);
                UpdateTurnBonusDefense(0);
                UpdateTurnBonusHealth(0);
                UpdateBallChargeSlider(0f);
                UpdateBoardTimerSlider(1f);
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from events to prevent memory leaks
            if (Player.Instance != null)
            {
                Player.Instance.OnHealthChanged -= UpdatePlayerHealth;
                Player.Instance.OnDefenseChanged -= UpdatePlayerDefense;
                Player.Instance.OnAttackDamageChanged -= UpdatePlayerAttack;
            }
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnAttackAccumulated -= UpdateTurnBonusAttack;
                TurnManager.Instance.OnDefenseAccumulated -= UpdateTurnBonusDefense;
                TurnManager.Instance.OnHealthAccumulated -= UpdateTurnBonusHealth;
                TurnManager.Instance.OnBallsInHandChanged -= UpdateBallsInHand;
                TurnManager.Instance.OnBallChargeProgress -= UpdateBallChargeSlider;
                TurnManager.Instance.OnBoardTimerProgress -= UpdateBoardTimerSlider;
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

        private void UpdatePlayerAttack(int amount)
        {
            if (_playerAttackText != null)
                _playerAttackText.text = $"Player ATK: {amount}";
        }

        // --- Turn Bonus UI Methods ---
        private void UpdateTurnBonusAttack(int amount)
        {
            if (_turnBonusAttackText != null)
                _turnBonusAttackText.text = $"Bonus ATK: +{amount}";
        }

        private void UpdateTurnBonusDefense(int amount)
        {
            if (_turnBonusDefenseText != null)
                _turnBonusDefenseText.text = $"Bonus DEF: +{amount}";
        }

        private void UpdateTurnBonusHealth(int amount)
        {
            if (_turnBonusHealthText != null)
                _turnBonusHealthText.text = $"Bonus HEAL: +{amount}";
        }

        // --- Turn Info UI Methods ---
        private void UpdateBallsInHand(int amount)
        {
            if (_ballsInHandText != null)
                _ballsInHandText.text = $"Balls Left: {amount}";
        }

        private void UpdateBallChargeSlider(float progress)
        {
            if (_ballChargeSlider != null)
            {
                _ballChargeSlider.value = progress;
            }
        }

        private void UpdateBoardTimerSlider(float progress)
        {
            if (_boardTimerSlider != null)
            {
                _boardTimerSlider.value = progress;
            }
        }
    }
}
