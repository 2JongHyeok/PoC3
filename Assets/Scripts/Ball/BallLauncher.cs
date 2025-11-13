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

        [Header("Trajectory Indicator")]
        [SerializeField] private Transform _trajectoryIndicator; // Assign a simple Square sprite transform

        /// <summary>
        /// Event fired when the player releases the mouse to launch the ball.
        /// Passes the target ball and the calculated launch force vector.
        /// </summary>
        public event Action<Ball, Vector2> OnLaunch;

        private Camera _mainCamera;
        private Ball _targetBall;
        private Vector3 _startDragPosition;
        private bool _isAiming = false;
        private Color _originalBallColor;

        private void Awake()
        {
            _mainCamera = Camera.main;
            if (_trajectoryIndicator != null)
            {
                _trajectoryIndicator.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            // If we are aiming, handle aiming logic and release
            if (_isAiming)
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
            // If not aiming, check for input to start aiming
            else
            {
                if (Input.GetMouseButtonDown(0))
                {
                    TryStartAiming();
                }
            }
        }

        private void TryStartAiming()
        {
            Vector3 mousePosition = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
            int layerMask = LayerMask.GetMask("ReadyBall");
            RaycastHit2D hit = Physics2D.Raycast(new Vector2(mousePosition.x, mousePosition.y), Vector2.zero, 0f, layerMask);

            if (hit.collider != null)
            {
                Ball ball = hit.collider.GetComponent<Ball>();
                // Only start aiming if the ball exists and has not been launched yet.
                if (ball != null && !ball.IsLaunched)
                {
                    _isAiming = true;
                    _targetBall = ball;
                    _startDragPosition = mousePosition;
                    if(_trajectoryIndicator) _trajectoryIndicator.gameObject.SetActive(true);
                    
                    // Store original color
                    SpriteRenderer ballSprite = _targetBall.GetComponent<SpriteRenderer>();
                    if (ballSprite != null)
                    {
                        _originalBallColor = ballSprite.color;
                    }
                    Debug.Log($"[BallLauncher] Started aiming with ball: {_targetBall.name}");
                }
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
