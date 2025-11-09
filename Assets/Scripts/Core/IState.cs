using UnityEngine;

namespace PoC3.Core
{
    /// <summary>
    /// Defines a state for a state machine.
    /// All states (e.g., PlayerTurnState, EnemyTurnState) must implement this interface.
    /// </summary>
    public interface IState
    {
        /// <summary>
        /// Called once when entering this state.
        /// Use for initialization and setup.
        /// </summary>
        void OnEnter();

        /// <summary>
        /// Called every frame while this state is active.
        /// Use for continuous logic.
        /// </summary>
        void OnUpdate();

        /// <summary>
        /// Called once when exiting this state.
        /// Use for cleanup.
        /// </summary>
        void OnExit();
    }
}
