using UnityEngine;
using UnityEngine.SceneManagement;

namespace HealthyRace.Core
{
    /// <summary>
    /// Navegação entre cenas e estado de pausa. É o único lugar que carrega cenas
    /// ou mexe em <see cref="Time.timeScale"/>; menus e fases só pedem a intenção.
    /// </summary>
    public static class GameFlow
    {
        public const string MenuScene = "MainMenu";

        private static readonly string[] LevelScenes = { "Phase1_CorridaDoEquilibrio" };

        /// <summary>Fase em andamento (1-based). Fora de uma fase vale a última jogada.</summary>
        public static int CurrentLevel { get; private set; } = 1;

        public static int LevelCount => LevelScenes.Length;

        public static bool HasNextLevel => CurrentLevel < LevelCount;

        public static bool IsPaused { get; private set; }

        public static string SceneForLevel(int level)
        {
            return LevelScenes[Mathf.Clamp(level, 1, LevelCount) - 1];
        }

        public static void SetPaused(bool paused)
        {
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
        }

        public static void LoadMenu()
        {
            Load(MenuScene);
        }

        public static void LoadLevel(int level)
        {
            CurrentLevel = Mathf.Clamp(level, 1, LevelCount);
            Load(SceneForLevel(CurrentLevel));
        }

        public static void LoadNextLevel()
        {
            LoadLevel(HasNextLevel ? CurrentLevel + 1 : CurrentLevel);
        }

        /// <summary>Recarrega a cena ativa: é o que garante estado inicial limpo.</summary>
        public static void RestartLevel()
        {
            Load(SceneManager.GetActiveScene().name);
        }

        public static void Quit()
        {
            SetPaused(false);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static void Load(string sceneName)
        {
            // Uma cena carregada com a partida pausada nasceria congelada.
            SetPaused(false);
            SceneManager.LoadScene(sceneName);
        }
    }
}
