using UnityEngine;
using UnityEngine.SceneManagement;
using PoC3.ManagerSystem;
using System.Linq;

namespace PoC3.Level
{
    /// <summary>
    /// Handles scene progression when all enemies in the current stage are defeated.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        [SerializeField] private string[] _sceneOrder = { "Scene1", "Scene2", "Scene3" };
        [SerializeField] private string _victoryScene;
        [SerializeField] private string _gameOverScene;
        [SerializeField] private EnemyManager _enemyManager;
        [SerializeField] private PlayerSystem.Player _player;

        private int _currentSceneIndex;

        private void Awake()
        {
            if (_enemyManager == null)
            {
                _enemyManager = FindFirstObjectByType<EnemyManager>();
            }

            if (_player == null)
            {
                _player = FindFirstObjectByType<PlayerSystem.Player>();
            }

            _currentSceneIndex = System.Array.IndexOf(_sceneOrder, SceneManager.GetActiveScene().name);

            if (_enemyManager != null)
            {
                _enemyManager.OnEnemyUnregistered += HandleEnemyUnregistered;
            }

            if (_player != null)
            {
                _player.OnDied += HandlePlayerDied;
            }
        }

        private void OnDestroy()
        {
            if (_enemyManager != null)
            {
                _enemyManager.OnEnemyUnregistered -= HandleEnemyUnregistered;
            }

            if (_player != null)
            {
                _player.OnDied -= HandlePlayerDied;
            }
        }

        private void HandleEnemyUnregistered(EnemySystem.Enemy enemy)
        {
            if (_enemyManager.GetAllActiveEnemies().Count == 0)
            {
                LoadNextScene();
            }
        }

        private void HandlePlayerDied()
        {
            if (!string.IsNullOrEmpty(_gameOverScene))
            {
                SceneManager.LoadScene(_gameOverScene);
            }
        }

        private void LoadNextScene()
        {
            _currentSceneIndex++;

            if (_currentSceneIndex >= 0 && _currentSceneIndex < _sceneOrder.Length)
            {
                SceneManager.LoadScene(_sceneOrder[_currentSceneIndex]);
                return;
            }

            if (!string.IsNullOrEmpty(_victoryScene))
            {
                SceneManager.LoadScene(_victoryScene);
                return;
            }

            Debug.Log("[LevelManager] All scenes cleared.");
        }

        // Usage in Unity:
        // 1. Add LevelManager to a persistent GameObject in each scene.
        // 2. Fill _sceneOrder with the exact scene names in build order.
        // 3. Assign EnemyManager and Player references (auto-detected if left empty).
        // 4. Optional _victoryScene for final clear, _gameOverScene for player death.
    }
}
