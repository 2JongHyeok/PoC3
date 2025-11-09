using UnityEngine;
using System;

namespace PoC3.Core
{
    /// <summary>
    /// Manages states for a MonoBehaviour, allowing for state transitions and updates.
    /// This component can be added to any GameObject that needs stateful behavior.
    /// </summary>
    public class StateMachine : MonoBehaviour
    {
        /// <summary>
        /// Event fired when the state changes.
        /// Provides the new state type.
        /// </summary>
        public event Action<Type> OnStateChanged;

        private IState _currentState;

        /// <summary>
        /// Initializes the state machine with a starting state.
        /// </summary>
        /// <param name="startingState">The initial state to activate.</param>
        public void Initialize(IState startingState)
        {
            _currentState = startingState;
            _currentState?.OnEnter();
            OnStateChanged?.Invoke(_currentState.GetType());
            
            Debug.Log($"[StateMachine] Initialized with state: {_currentState.GetType().Name}");
        }

        /// <summary>
        /// Transitions to a new state.
        /// It calls OnExit on the current state and OnEnter on the new state.
        /// </summary>
        /// <param name="newState">The state to transition to.</param>
        public void ChangeState(IState newState)
        {
            if (newState == null || newState.GetType() == _currentState?.GetType())
            {
                return;
            }

            _currentState?.OnExit();
            _currentState = newState;
            _currentState.OnEnter();
            OnStateChanged?.Invoke(_currentState.GetType());

            Debug.Log($"[StateMachine] Changed state to: {_currentState.GetType().Name}");
        }

        private void Update()
        {
            // Continuously update the current state
            _currentState?.OnUpdate();
        }
    }
}
