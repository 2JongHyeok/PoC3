using UnityEngine;
using System;
using System.Collections.Generic;
using PoC3.TileSystem;
using PoC3.BallSystem;

namespace PoC3.BoardSystem
{
    /// <summary>
    /// Manages the game board, including tile and wall generation.
    /// Also keeps track of active balls on the board.
    /// </summary>
    public class GameBoard : MonoBehaviour
    {
        [Header("Board State")]
        private readonly List<Tile> _tiles = new List<Tile>();
        private readonly List<Ball> _activeBalls = new List<Ball>();

        public IReadOnlyList<Tile> Tiles => _tiles.AsReadOnly();
        public IReadOnlyList<Ball> ActiveBalls => _activeBalls.AsReadOnly();

        private void Start()
        {
            _tiles.AddRange(GetComponentsInChildren<Tile>());
            Debug.Log($"[GameBoard] Found {_tiles.Count} tiles in the scene.");
        }

        /// <summary>
        /// Adds a ball to the list of active balls on the board.
        /// </summary>
        public void AddBall(Ball ball)
        {
            if (!_activeBalls.Contains(ball))
            {
                _activeBalls.Add(ball);
                ball.OnBallUsed += HandleBallUsed;
                ball.OnBallDestroyed += HandleBallUsed;
            }
        }

        /// <summary>
        /// Removes a ball from the active list when it's used.
        /// </summary>
        private void HandleBallUsed(Ball ball)
        {
            if (_activeBalls.Contains(ball))
            {
                ball.OnBallUsed -= HandleBallUsed;
                ball.OnBallDestroyed -= HandleBallUsed;
                _activeBalls.Remove(ball);
                BallStateChanged?.Invoke();
            }
        }

        public event Action BallStateChanged;

        /// <summary>
        /// Checks if all active balls on the board have stopped moving.
        /// </summary>
        /// <returns>True if all balls are stopped, false otherwise.</returns>
        public bool AreAllBallsStopped()
        {
            if (_activeBalls.Count == 0) return true;

            foreach (Ball ball in _activeBalls)
            {
                if (!ball.IsStopped())
                {
                    return false;
                }
            }
            return true;
        }
    }
}
