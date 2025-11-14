using UnityEngine;
using System;

namespace PoC3.BallSystem
{
    /// <summary>
    /// Handles player input for aiming and launching a ball.
    /// Displays a trajectory indicator.
    /// </summary>
    public class BallLauncher : MonoBehaviour
    {
        [Header("Aiming Parameters")]
        [SerializeField] private float _maxLaunchForce = 10f;
        [SerializeField] private float _minDragDistance = 0.5f;
        [SerializeField] private float _maxPowerDragDistance = 10f;

        [Header("Placement Parameters")]
        [Tooltip("The area where the player can place the ball.")]
        [SerializeField] private Rect _placementArea = new Rect(-4.3f, -6.3f, 0.6f, 8.6f);
        [Tooltip("The layer mask to detect other balls when checking for valid placement.")]
        [SerializeField] private LayerMask _ballCollisionLayer;

        [Header("Trajectory Indicator")]
        [SerializeField] private Transform _trajectoryIndicator; // Assign a simple Square sprite transform

        /// <summary>
        /// Event fired when the player releases the mouse to launch the ball.
        /// Passes the target ball and the calculated launch force vector.
        /// </summary>
        public event Action<Ball, Vector2> OnLaunch;

        private Camera _mainCamera;
        private Ball _targetBall;
        private Ball _currentBall;
        private Vector3 _startDragPosition;
        private bool _isAiming = false;
        private bool _isPlacing = false; // Flag for the new placement phase
        private Color _originalBallColor;

        private void Awake()
        {
            _mainCamera = Camera.main;
            if (_trajectoryIndicator != null)
            {
                _trajectoryIndicator.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Starts the ball placement process. The ball will follow the mouse.
        /// </summary>
        public void AttachBallToMouse(Ball ball)
        {
            if (_isAiming || _isPlacing) return;

            _isPlacing = true;
            _targetBall = ball;
            _currentBall = ball;
            _targetBall.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic; // Disable physics while placing
            
            // Store original color to revert back to after placement
            SpriteRenderer ballSprite = _targetBall.GetComponent<SpriteRenderer>();
            if (ballSprite != null)
            {
                _originalBallColor = ballSprite.color;
            }
        }

        private void Update()
        {
            if (_isPlacing)
            {
                HandleBallPlacement();
            }
            // If we are aiming, handle aiming logic and release
            else if (_isAiming)
            {
                if (Input.GetMouseButton(0))
                {
                    UpdateAimVisuals();
                }
                if (Input.GetMouseButtonUp(0))
                {
                    LaunchBall();
                }
            }
            // If not aiming or placing, check for input to start aiming (original behavior)
            else
            {
                if (Input.GetMouseButtonDown(0))
                {
                    TryStartAiming();
                }
                else if(Input.GetMouseButtonDown(1))
                {
                    RetryBallPlacement();
                }
            }
        }

        private void RetryBallPlacement()
        {
            Debug.Log("[BallLauncher] Ball placement cancelled.");
            AttachBallToMouse(_currentBall);
        }

        /// <summary>
        /// Handles the logic for when the ball is following the mouse.
        /// </summary>
        private void HandleBallPlacement()
        {
            // Ball follows the mouse cursor, clamped within the placement area
            Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector2 placementPos = new Vector2(
                Mathf.Clamp(mouseWorldPos.x, _placementArea.xMin, _placementArea.xMax),
                Mathf.Clamp(mouseWorldPos.y, _placementArea.yMin, _placementArea.yMax)
            );
            _targetBall.transform.position = placementPos;

            // Check if the current position is valid and provide visual feedback
            bool canPlace = !IsBallAtPosition(placementPos);
            _targetBall.GetComponent<SpriteRenderer>().color = canPlace ? Color.green : Color.red;

            // If the player clicks, attempt to place the ball
            if (Input.GetMouseButtonDown(0))
            {
                if (canPlace)
                {
                    // Placement is valid, finalize position and transition to aiming
                    _isPlacing = false;
                    _targetBall.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic; // Re-enable physics
                    _targetBall.GetComponent<SpriteRenderer>().color = _originalBallColor; // Restore original color
                    
                    Debug.Log($"[BallLauncher] Ball placed at {placementPos}.");
                    
                    // Call TryStartAiming with the ball that was just placed
                    TryStartAiming(_targetBall);
                }
                else
                {
                    Debug.LogWarning("[BallLauncher] Cannot place ball here, another ball is in the way.");
                }
            }
        }

        /// <summary>
        /// Checks if another ball is at the given position, ignoring the ball being placed.
        /// </summary>
        private bool IsBallAtPosition(Vector2 position)
        {
            // Use a small overlap circle to check for other colliders on the ball layer
            Collider2D[] colliders = Physics2D.OverlapCircleAll(position, 0.5f, _ballCollisionLayer);
            foreach (var col in colliders)
            {
                if (col.gameObject != _targetBall.gameObject)
                {
                    return true; // Found another ball
                }
            }
            return false;
        }

        private void TryStartAiming(Ball ballToAim = null)
        {
            Ball ball = ballToAim;

            // If no ball was passed in, use the original raycast method to find one
            if (ball == null)
            {
                Vector3 mousePosition = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
                int layerMask = LayerMask.GetMask("ReadyBall");
                RaycastHit2D hit = Physics2D.Raycast(new Vector2(mousePosition.x, mousePosition.y), Vector2.zero, 0f, layerMask);

                if (hit.collider != null)
                {
                    ball = hit.collider.GetComponent<Ball>();
                }
            }

            // Only start aiming if the ball exists and has not been launched yet.
            if (ball != null && !ball.IsLaunched)
            {
                _isAiming = true;
                _targetBall = ball;
                _startDragPosition = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
                if(_trajectoryIndicator) _trajectoryIndicator.gameObject.SetActive(true);
                
                // Store original color if it hasn't been stored already
                SpriteRenderer ballSprite = _targetBall.GetComponent<SpriteRenderer>();
                if (ballSprite != null)
                {
                    _originalBallColor = ballSprite.color;
                }
                Debug.Log($"[BallLauncher] Started aiming with ball: {_targetBall.name}");
            }
        }

        private void UpdateAimVisuals()
        {
            if (_trajectoryIndicator == null || _targetBall == null) return;

            Vector3 currentDragPosition = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector3 dragVector = currentDragPosition - _startDragPosition;
            
            // Launch direction is opposite to drag direction
            Vector2 launchDirection = -dragVector.normalized;
            float dragDistance = dragVector.magnitude;
            float power = CalculatePowerFromDrag(dragDistance);

            _trajectoryIndicator.position = _targetBall.transform.position;
            
            float angle = Mathf.Atan2(launchDirection.y, launchDirection.x) * Mathf.Rad2Deg;
            _trajectoryIndicator.rotation = Quaternion.Euler(0, 0, angle);

            float visualLength = Mathf.Clamp(dragDistance, 0f, _maxPowerDragDistance);
            _trajectoryIndicator.localScale = new Vector3(visualLength, _trajectoryIndicator.localScale.y, _trajectoryIndicator.localScale.z);

            // Visual feedback for launch readiness
            SpriteRenderer ballSprite = _targetBall.GetComponent<SpriteRenderer>();
            if (ballSprite != null)
            {
                if (dragDistance > _minDragDistance)
                {
                    ballSprite.color = Color.white;
                }
                else
                {
                    ballSprite.color = _originalBallColor;
                }
            }
        }

        private void LaunchBall()
        {
            Vector3 currentDragPosition = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector3 dragVector = currentDragPosition - _startDragPosition;
            
            float dragDistance = dragVector.magnitude;

            if (dragDistance > _minDragDistance)
            {
                Vector2 launchDirection = -dragVector.normalized;
                float power = CalculatePowerFromDrag(dragDistance);
                Vector2 launchForce = launchDirection * power;

                Debug.Log($"[BallLauncher] Launching ball with force: {launchForce}");
                OnLaunch?.Invoke(_targetBall, launchForce);
            }
            else
            {
                Debug.Log("[BallLauncher] Drag distance too short. Launch cancelled.");
            }

            // Reset ball color and aiming state
            if (_targetBall != null)
            {
                SpriteRenderer ballSprite = _targetBall.GetComponent<SpriteRenderer>();
                if (ballSprite != null)
                {
                    ballSprite.color = _originalBallColor;
                }
            }
            _isAiming = false;
            _targetBall = null;
            if(_trajectoryIndicator) _trajectoryIndicator.gameObject.SetActive(false);
        }

        private float CalculatePowerFromDrag(float dragDistance)
        {
            if (_maxPowerDragDistance <= 0f)
            {
                return 0f;
            }

            float charge = Mathf.Clamp01(dragDistance / _maxPowerDragDistance);
            return charge * _maxLaunchForce;
        }
    }
}
