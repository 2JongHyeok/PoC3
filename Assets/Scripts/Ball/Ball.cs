using UnityEngine;
using System;
using System.Collections.Generic;
using PoC3.TileSystem;
using TMPro;
using PoC3.EnemySystem;

public enum BallType
{
    Player,
    Enemy
}

namespace PoC3.BallSystem
{
    /// <summary>
    /// Represents a ball in the game. Manages its level and collision logic.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class Ball : MonoBehaviour
    {
        [SerializeField]
        private int _level = 1; // Initial level of the ball
        public int Level => _level;
        public BallType ballType;
        public int maxHealth = 4;
        public TextMeshProUGUI textLevel;
        [SerializeField] private int _curHealth;

        public bool IsLaunched { get; private set; } = false;
        public Enemy OwnerEnemy { get; private set; }

        private Rigidbody2D _rb;
        private CircleCollider2D _circleCollider;
        public SpriteRenderer levelRenderer;
        public SpriteRenderer healthRenderer;

        private Material _healthMaterial;
        private Vector2 _lastVelocity;
        private readonly HashSet<Tile> _tilesInContact = new HashSet<Tile>();
         
        /// <summary>
        /// Event fired when the ball's level changes.
        /// </summary>
        public event Action<int> OnLevelChanged;

        /// <summary>
        /// Event fired when the ball is used (e.g., after settling on tiles).
        /// </summary>
        public event Action<Ball> OnBallUsed;

        /// <summary>
        /// Tiles the ball is currently overlapping via trigger.
        /// </summary>
        public IReadOnlyCollection<Tile> TilesInContact => _tilesInContact;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            if (_rb == null)
            {
                Debug.LogError("[Ball] Rigidbody2D component not found on Ball GameObject.");
            }

            _circleCollider = GetComponent<CircleCollider2D>();
            if (_circleCollider == null)
            {
                Debug.LogError("[Ball] CircleCollider2D component not found on Ball GameObject.");
            }

            healthRenderer = GetComponent<SpriteRenderer>();
            if (healthRenderer == null)
            {
                Debug.LogError("[Ball] SpriteRenderer component not found on Ball GameObject.");
            }

            _healthMaterial = healthRenderer.material;
            UpdateColorBasedOnLevel(); // Set initial color
        }

        private void Start()
        {
            SetShineEffect(ballType == BallType.Player);
            SetPlayer(ballType == BallType.Player);
        }

        private void FixedUpdate()
        {
            // Store the velocity of the previous frame to ensure accurate reflection calculation
            _lastVelocity = _rb.linearVelocity;
        }

        public float Radius
        {
            get
            {
                if (_circleCollider == null)
                {
                    Debug.LogError("[Ball] CircleCollider2D is null, cannot get radius.");
                    return 0f;
                }
                return _circleCollider.radius;
            }
        }

        /// <summary>
        /// Increases the ball's level by 1.
        /// </summary>
        public void IncreaseLevel()
        {
            _level++;
            UpdateColorBasedOnLevel(); // Update color when level changes
            OnLevelChanged?.Invoke(_level);
            Debug.Log($"[Ball] Ball {name} level increased to {_level}");
        }

        /// <summary>
        /// Marks the ball as used and triggers the OnBallUsed event.
        /// This typically leads to the ball being removed from the game.
        /// </summary>
        public void UseBall()
        {
            OnBallUsed?.Invoke(this);
            Debug.Log($"[Ball] Ball {name} used.");
            _tilesInContact.Clear();
            OwnerEnemy = null;
            // In a real game, you might disable the GameObject or return it to a pool here.
            gameObject.SetActive(false);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            // If colliding with another ball, use default physics but increase levels
            if (collision.gameObject.CompareTag("Ball"))
            {
                Ball otherBall = collision.gameObject.GetComponent<Ball>();
                if (otherBall != null)
                {
                    // Same player ball
                    if (ballType == otherBall.ballType)
                    {
                        IncreaseLevel(); // This ball's level increases
                        otherBall.IncreaseLevel(); // The other ball's level increases
                        Debug.Log($"[Ball] Ball {name} collided with another ball {otherBall.name}. Both levels increased.");
                    }
                    // Other player ball
                    else
                    {
                        TakeDamage(1);
                    }
                }
            }
            // If colliding with a wall, manually calculate reflection for perfect bounce
            else if (collision.gameObject.CompareTag("Wall"))
            {
                IncreaseLevel();

                // Manually calculate and apply reflection
                Vector2 inNormal = collision.contacts[0].normal;
                Vector2 reflectedVelocity = Vector2.Reflect(_lastVelocity, inNormal);
                _rb.linearVelocity = reflectedVelocity;

                Debug.Log($"[Ball] Ball {name} collided with a wall. Reflected velocity.");
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            Tile tile = other.GetComponent<Tile>();
            if (tile == null)
            {
                return;
            }

            if (_tilesInContact.Add(tile))
            {
                Debug.Log($"[Ball] {name} entered tile {tile.name}.");
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            Tile tile = other.GetComponent<Tile>();
            if (tile == null)
            {
                return;
            }

            if (_tilesInContact.Remove(tile))
            {
                Debug.Log($"[Ball] {name} exited tile {tile.name}.");
            }
        }

        // You might add methods for launching the ball, checking if it has stopped, etc.
        public void Launch(Vector2 force)
        {
            if (_rb != null)
            {
                IsLaunched = true;
                gameObject.layer = LayerMask.NameToLayer("Ball"); // Change layer back to "Ball"
                _rb.AddForce(force, ForceMode2D.Impulse);
                Debug.Log($"[Ball] Ball {name} launched with force: {force}");
            }
        }

        /// <summary>
        /// Checks if the ball has come to a stop.
        /// </summary>
        /// <param name="threshold">Velocity magnitude below which the ball is considered stopped.</param>
        /// <returns>True if the ball is stopped, false otherwise.</returns>
        public bool IsStopped(float threshold = 0.1f)
        {
            return _rb != null && _rb.linearVelocity.magnitude < threshold;
        }

        public void AssignOwnerEnemy(Enemy enemy)
        {
            OwnerEnemy = enemy;
        }

        private void UpdateColorBasedOnLevel()
        {
            textLevel.text = _level.ToString();

            //if (levelRenderer == null) return;

            //Color targetColor;
            //switch (_level)
            //{
            //    case 0: targetColor = Color.black; break;
            //    case 1: targetColor = Color.red; break;
            //    case 2: targetColor = new Color(1f, 0.5f, 0f); /* Orange */ break;
            //    case 3: targetColor = Color.yellow; break;
            //    case 4: targetColor = Color.green; break;
            //    case 5: targetColor = Color.blue; break;
            //    case 6: targetColor = new Color(0.29f, 0f, 0.51f); /* Indigo */ break;
            //    case 7: targetColor = new Color(0.5f, 0f, 0.5f); /* Purple */ break;
            //    default: targetColor = new Color(1f, 0.75f, 0.8f); /* Pink */ break; // Level 8 and above
            //}
            //levelRenderer.color = targetColor;
        }
        
        private void TakeDamage(int amount)
        {
            _curHealth -= amount;
            float fillAmount = (float)_curHealth / maxHealth;
            _healthMaterial.SetFloat("_FillAmount", fillAmount);

            if (_curHealth <= 0)
                Destroy(gameObject);
        }

        public void SetShineEffect(bool isOn)
        {
            float toggle = (isOn ? 1.0f : 0.0f);
            _healthMaterial.SetFloat("_PulseToggle", toggle);
        }

        public void SetPlayer(bool isPlayer)
        {
            levelRenderer.color = isPlayer ? Color.black : Color.blue;
            maxHealth = isPlayer ? 10 : 3;
            _curHealth = maxHealth;
        }
    }
}
