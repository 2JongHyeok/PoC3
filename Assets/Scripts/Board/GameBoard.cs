using UnityEngine;
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
        [Header("Board Dimensions")]
        [SerializeField] private int _width = 5;
        [SerializeField] private int _height = 5;

        [Header("Board Scale")]
        [SerializeField] private float _boardScale = 1f;

        [Header("Board Offset")]
        [SerializeField] private Vector2 _boardOffset = Vector2.zero;

        [Header("Wall Settings")]
        [SerializeField] private float _wallOffset = 0.1f;

        [Header("Prefabs")]
        [SerializeField] private Tile _tilePrefab;
        [SerializeField] private GameObject _wallPrefab; // Assuming a simple wall prefab

        [Header("Board State")]
        private readonly List<Tile> _tiles = new List<Tile>();
        private readonly List<Ball> _activeBalls = new List<Ball>();

        public IReadOnlyList<Tile> Tiles => _tiles.AsReadOnly();
        public IReadOnlyList<Ball> ActiveBalls => _activeBalls.AsReadOnly();

        private void Start()
        {
            GenerateBoard();
        }

        /// <summary>
        /// Generates the 5x5 tile grid and surrounding walls.
        /// </summary>
        [ContextMenu("Generate Board")] // Allows running this from the component's context menu in the editor
        private void GenerateBoard()
        {
            // Clear existing board if any
            while (transform.childCount > 0)
            {
                DestroyImmediate(transform.GetChild(0).gameObject);
            }
            _tiles.Clear();

            if (_tilePrefab == null)
            {
                Debug.LogError("[GameBoard] Tile Prefab is not assigned!");
                return;
            }

            // Generate Tiles
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    Vector3 position = new Vector3(x * _boardScale + _boardOffset.x, y * _boardScale + _boardOffset.y, 0);
                    Tile newTile = Instantiate(_tilePrefab, position, Quaternion.identity, transform);
                    newTile.transform.localScale = Vector3.one * _boardScale;
                    newTile.name = $"Tile_{x}_{y}";
                    _tiles.Add(newTile);
                }
            }

            // Generate Walls
            if (_wallPrefab != null)
            {
                // Assuming tile size is 1x1 before scaling
                float tileEdgeOffset = 0.5f * _boardScale;
                float totalWallOffset = tileEdgeOffset + _wallOffset;
                Vector3 wallScale = new Vector3(1f * _boardScale, 0.2f * _boardScale, 1f);

                // Bottom and Top walls
                for (int x = 0; x < _width; x++)
                {
                    Vector3 bottomWallPos = new Vector3(x * _boardScale + _boardOffset.x, -totalWallOffset + _boardOffset.y, 0);
                    GameObject bottomWall = Instantiate(_wallPrefab, bottomWallPos, Quaternion.identity, transform);
                    bottomWall.transform.localScale = wallScale;
                    bottomWall.name = $"Wall_Bottom_{x}";

                    Vector3 topWallPos = new Vector3(x * _boardScale + _boardOffset.x, (_height - 1) * _boardScale + totalWallOffset + _boardOffset.y, 0);
                    GameObject topWall = Instantiate(_wallPrefab, topWallPos, Quaternion.identity, transform);
                    topWall.transform.localScale = wallScale;
                    topWall.name = $"Wall_Top_{x}";
                }
                // Left and Right walls
                for (int y = 0; y < _height; y++)
                {
                    Vector3 leftWallPos = new Vector3(-totalWallOffset + _boardOffset.x, y * _boardScale + _boardOffset.y, 0);
                    GameObject leftWall = Instantiate(_wallPrefab, leftWallPos, Quaternion.Euler(0,0,90), transform);
                    leftWall.transform.localScale = wallScale;
                    leftWall.name = $"Wall_Left_{y}";

                    Vector3 rightWallPos = new Vector3((_width - 1) * _boardScale + totalWallOffset + _boardOffset.x, y * _boardScale + _boardOffset.y, 0);
                    GameObject rightWall = Instantiate(_wallPrefab, rightWallPos, Quaternion.Euler(0,0,90), transform);
                    rightWall.transform.localScale = wallScale;
                    rightWall.name = $"Wall_Right_{y}";
                }
            }
            else
            {
                Debug.LogWarning("[GameBoard] Wall Prefab is not assigned. Walls will not be generated.");
            }

            Debug.Log($"[GameBoard] Generated a {_width}x{_height} board with {_tiles.Count} tiles.");
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
                _activeBalls.Remove(ball);
            }
        }

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
