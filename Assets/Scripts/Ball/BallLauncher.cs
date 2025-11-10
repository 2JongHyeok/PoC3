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
            float power = Mathf.Clamp(dragDistance, 0, _maxLaunchForce);

            _trajectoryIndicator.position = _targetBall.transform.position;
            
            float angle = Mathf.Atan2(launchDirection.y, launchDirection.x) * Mathf.Rad2Deg;
            _trajectoryIndicator.rotation = Quaternion.Euler(0, 0, angle);

            _trajectoryIndicator.localScale = new Vector3(power, _trajectoryIndicator.localScale.y, _trajectoryIndicator.localScale.z);
        }

        private void LaunchBall()
        {
            Vector3 currentDragPosition = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector3 dragVector = currentDragPosition - _startDragPosition;
            
            Vector2 launchDirection = -dragVector.normalized;
            float dragDistance = dragVector.magnitude;
            float power = Mathf.Clamp(dragDistance, 0, _maxLaunchForce);
            
            Vector2 launchForce = launchDirection * power;

            Debug.Log($"[BallLauncher] Launching ball with force: {launchForce}");
            OnLaunch?.Invoke(_targetBall, launchForce);

            // Reset aiming state
            _isAiming = false;
            _targetBall = null;
            if(_trajectoryIndicator) _trajectoryIndicator.gameObject.SetActive(false);
        }
    }
}
