using UnityEngine;
using System;

namespace PoC3.ManagerSystem
{
    /// <summary>
    /// Global countdown that limits how long balls can be launched before resolving buffs.
    /// </summary>
    public class BoardTimerManager : MonoBehaviour
    {
        [SerializeField] private float _durationSeconds = 30f;
        private float _remainingTime;
        private bool _isRunning;

        public event Action<float> OnTimerTick;
        public event Action OnTimerEnded;

        private void OnEnable()
        {
            ResetTimer();
        }

        private void Update()
        {
            if (!_isRunning)
            {
                return;
            }

            _remainingTime -= Time.deltaTime;
            if (_remainingTime <= 0f)
            {
                _remainingTime = 0f;
                _isRunning = false;
                OnTimerTick?.Invoke(0f);
                OnTimerEnded?.Invoke();
            }
            else
            {
                OnTimerTick?.Invoke(_remainingTime / _durationSeconds);
            }
        }

        public void ResetTimer()
        {
            _remainingTime = _durationSeconds;
            _isRunning = true;
            OnTimerTick?.Invoke(1f);
        }

        public void StopTimer()
        {
            _isRunning = false;
        }

        public void ResumeTimer()
        {
            if (_remainingTime > 0f)
            {
                _isRunning = true;
            }
        }

        public bool IsRunning => _isRunning;
        public float RemainingTime => _remainingTime;

        // Usage in Unity:
        // 1. Add this component to a persistent GameObject (e.g., GameManager).
        // 2. Set Duration Seconds to 30 (or desired limit).
        // 3. Assign this component to TurnManager's BoardTimer field for automatic control.
    }
}
