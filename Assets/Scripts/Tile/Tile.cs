using UnityEngine;

namespace PoC3.TileSystem
{
    /// <summary>
    /// Represents a single tile on the game board.
    /// This MonoBehaviour will be attached to a GameObject in the Unity scene.
    /// </summary>
    public class Tile : MonoBehaviour
    {
        [SerializeField]
        private TileEffect _tileEffect; // The ScriptableObject defining this tile's effect
        public TileEffect CurrentTileEffect => _tileEffect;

        // Optional: Visual representation of the tile
        [SerializeField]
        private SpriteRenderer _spriteRenderer;

        private void Start()
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (_spriteRenderer != null && _tileEffect != null)
            {
                _spriteRenderer.color = _tileEffect.TileColor;
            }
        }

        /// <summary>
        /// Applies the tile's effect using the provided ball's level.
        /// </summary>
        /// <param name="ballLevel">The level of the ball that activated this tile.</param>
        /// <returns>The total calculated effect value from this tile.</returns>
        public int ActivateTileEffect(int ballLevel)
        {
            if (_tileEffect == null)
            {
                Debug.LogWarning($"[Tile] Tile {name} at {transform.position} has no TileEffect assigned.");
                return 0;
            }

            // Apply the effect and return the calculated value
            return _tileEffect.ApplyEffect(ballLevel);
        }

        // You might want to add methods for visual feedback (e.g., highlight when active)
        public void Highlight(bool isHighlighted)
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = isHighlighted ? Color.yellow : _tileEffect.TileColor; // Revert to original tile color
            }
        }
    }
}
