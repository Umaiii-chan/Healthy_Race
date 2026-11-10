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

        /// <summary>As 10 fases em ordem (mapas em Levels/FaseNN.txt).</summary>
        private static readonly string[] LevelScenes =
        {
            "Phase1_CorridaDoEquilibrio",
            "Fase02",
            "Fase03",
            "Fase04",
            "Fase05",
            "Fase06",
            "Fase07",
            "Fase08",
            "Fase09",
            "Fase10",
        };

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

        public static bool LoadMenu()
        {
            return TryLoad(MenuScene);
        }

        /// <summary>
        /// Carrega a fase pedida. <see cref="CurrentLevel"/> só muda se a cena existir
        /// na build: uma carga recusada não deixa o jogo apontando para outra fase.
        /// </summary>
        public static bool LoadLevel(int level)
        {
            var target = Mathf.Clamp(level, 1, LevelCount);
            if (!TryLoad(SceneForLevel(target)))
            {
                return false;
            }

            CurrentLevel = target;
            return true;
        }

        public static bool LoadNextLevel()
        {
            return LoadLevel(HasNextLevel ? CurrentLevel + 1 : CurrentLevel);
        }

        /// <summary>Recarrega a cena ativa: é o que garante estado inicial limpo.</summary>
        public static bool RestartLevel()
        {
            return TryLoad(SceneManager.GetActiveScene().name);
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

        /// <summary>
        /// Carrega uma cena pelo nome. Se ela não estiver na build, registra o erro e
        /// não mexe em nada: nem pausa, nem cena ativa.
        /// </summary>
        public static bool TryLoad(string sceneName)
        {
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError(
                    $"GameFlow: a cena '{sceneName}' não está na build; nada foi carregado."
                );
                return false;
            }

            // Uma cena carregada com a partida pausada nasceria congelada.
            SetPaused(false);
            SceneManager.LoadScene(sceneName);
            return true;
        }
    }
}
