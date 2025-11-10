using UnityEngine;
using System;

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

        public bool IsLaunched { get; private set; } = false;

        private Rigidbody2D _rb;
        private CircleCollider2D _circleCollider;
        private SpriteRenderer _spriteRenderer;

        /// <summary>
        /// Event fired when the ball's level changes.
        /// </summary>
        public event Action<int> OnLevelChanged;

        /// <summary>
        /// Event fired when the ball is used (e.g., after settling on tiles).
        /// </summary>
        public event Action<Ball> OnBallUsed;

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

            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer == null)
            {
                Debug.LogError("[Ball] SpriteRenderer component not found on Ball GameObject.");
            }

            UpdateColorBasedOnLevel(); // Set initial color
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
            // In a real game, you might disable the GameObject or return it to a pool here.
            gameObject.SetActive(false); 
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            // Check for collision with walls (assuming walls have a specific tag or layer)
            // For now, let's assume any collision increases level as per requirement.
            // REQUIRE_GATHER.md: "공이 벽에 닿을 때마다 해당 공의 레벨이 1 증가합니다."
            // REQUIRE_GATHER.md: "공이 다른 공을 맞출 경우, 맞은 공과 맞춘 공 모두 레벨이 1씩 증가하며, 물리 충돌이 발생해야 합니다."

            // If colliding with another ball
            if (collision.gameObject.CompareTag("Ball")) // Assuming balls have "Ball" tag
            {
                Ball otherBall = collision.gameObject.GetComponent<Ball>();
                if (otherBall != null)
                {
                    IncreaseLevel(); // This ball's level increases
                    otherBall.IncreaseLevel(); // The other ball's level increases
                    Debug.Log($"[Ball] Ball {name} collided with another ball {otherBall.name}. Both levels increased.");
                }
            }
            // If colliding with a wall (assuming walls have "Wall" tag)
            else if (collision.gameObject.CompareTag("Wall")) // Assuming walls have "Wall" tag
            {
                IncreaseLevel();
                Debug.Log($"[Ball] Ball {name} collided with a wall. Level increased.");
            }
            // You might add more specific collision handling here for other objects
        }

        // You might add methods for launching the ball, checking if it has stopped, etc.
        public void Launch(Vector2 force)
        {
            if (_rb != null)
            {
                IsLaunched = true;
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

        private void UpdateColorBasedOnLevel()
        {
            if (_spriteRenderer == null) return;

            Color targetColor;
            switch (_level)
            {
                case 0: targetColor = Color.black; break;
                case 1: targetColor = Color.red; break;
                case 2: targetColor = new Color(1f, 0.5f, 0f); /* Orange */ break;
                case 3: targetColor = Color.yellow; break;
                case 4: targetColor = Color.green; break;
                case 5: targetColor = Color.blue; break;
                case 6: targetColor = new Color(0.29f, 0f, 0.51f); /* Indigo */ break;
                case 7: targetColor = new Color(0.5f, 0f, 0.5f); /* Purple */ break;
                default: targetColor = new Color(1f, 0.75f, 0.8f); /* Pink */ break; // Level 8 and above
            }
            _spriteRenderer.color = targetColor;
        }
    }
}
