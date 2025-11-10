using UnityEngine;

namespace PoC3.TileSystem
{
    public enum EffectType
    {
        None,
        Attack,
        Defense,
        Health
    }

    /// <summary>
    /// Base class for all tile effects.
    /// Inherit from this to create specific tile effects (e.g., DefenseTileEffect, HealthTileEffect).
    /// ScriptableObjects allow for easy creation and management of tile effect data assets in the Unity Editor.
    /// </summary>
    [CreateAssetMenu(fileName = "NewTileEffect", menuName = "PoC3/Tile Effect")]
    public class TileEffect : ScriptableObject
    {
        [Header("Effect Properties")]
        public EffectType Type;
        public Color TileColor = Color.white;

        [Header("Effect Values")]
        [SerializeField]
        private string _effectName = "New Effect";
        public string EffectName => _effectName;

        [SerializeField]
        private int _baseEffectValue = 1;
        public int BaseEffectValue => _baseEffectValue;

        /// <summary>
        /// Applies the effect to a target, considering the ball's level.
        /// This method should be overridden by derived classes to implement specific effects.
        /// </summary>
        /// <param name="ballLevel">The current level of the ball that activated the tile.</param>
        /// <returns>The total calculated effect value.</returns>
        public virtual int ApplyEffect(int ballLevel)
        {
            // As per REQUIRE_GATHER.md, ball level is added to the base effect value.
            int totalEffect = _baseEffectValue + ballLevel;
            Debug.Log($"[TileEffect] Applying {EffectName} ({Type}) with base value {_baseEffectValue} and ball level {ballLevel}. Total effect: {totalEffect}");
            return totalEffect;
        }
    }
}
