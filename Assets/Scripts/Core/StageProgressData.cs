using UnityEngine;

namespace PoC3.Progression
{
    /// <summary>
    /// Tracks cross-scene progression bonuses such as player ball max health scaling.
    /// </summary>
    public static class StageProgressData
    {
        /// <summary>
        /// Additional health granted to player balls based on cleared stages.
        /// </summary>
        public static int PlayerBallHealthBonus { get; private set; }

        public static void ResetProgress()
        {
            PlayerBallHealthBonus = 0;
            Debug.Log("[StageProgress] Player ball bonus reset.");
        }

        public static void AdvanceStage()
        {
            PlayerBallHealthBonus++;
            Debug.Log($"[StageProgress] Player ball bonus increased to {PlayerBallHealthBonus}.");
        }
    }
}
