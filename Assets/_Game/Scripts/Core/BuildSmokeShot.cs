using System;
using System.Collections;
using System.Globalization;
using UnityEngine;

namespace HealthyRace.Core
{
    /// <summary>
    /// Foto de dentro da build, para conferir o que o jogador vê de fato: abre uma fase,
    /// espera, salva a tela e fecha o jogo. Só age quando a build é iniciada com
    /// <c>-smoke-shot &lt;fase|menu&gt; &lt;arquivo.png&gt; [segundos]</c>; sem o argumento não faz nada.
    ///
    /// Existe porque teste de Editor não vê diferença de shader entre Editor e build: os
    /// membros dos corredores já ficaram invisíveis na fase sem nenhum teste acusar.
    /// </summary>
    public static class BuildSmokeShot
    {
        public const string Argument = "-smoke-shot";
        public const float DefaultSeconds = 4.5f;
        public const string MenuKeyword = "menu";
        public const int MenuLevel = 0;

        /// <summary>O pedido de foto na linha de comando, se houver e estiver completo.</summary>
        public static bool TryParse(
            string[] args,
            out int level,
            out string path,
            out float seconds
        )
        {
            level = 0;
            path = null;
            seconds = DefaultSeconds;
            var index = args == null ? -1 : Array.IndexOf(args, Argument);
            if (index < 0 || index + 2 >= args.Length)
            {
                return false;
            }

            // "menu" no lugar do número fotografa o menu: vale como fase 0
            if (args[index + 1] == MenuKeyword)
            {
                level = MenuLevel;
            }
            else if (
                !int.TryParse(args[index + 1], out level)
                || level < 1
                || level > GameFlow.LevelCount
            )
            {
                level = 0;
                return false;
            }

            path = args[index + 2];
            if (
                index + 3 < args.Length
                && float.TryParse(
                    args[index + 3],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var wait
                )
                && wait > 0f
            )
            {
                seconds = wait;
            }

            return !string.IsNullOrWhiteSpace(path);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Start()
        {
            if (Application.isEditor)
            {
                return;
            }

            if (
                !TryParse(
                    Environment.GetCommandLineArgs(),
                    out var level,
                    out var path,
                    out var seconds
                )
            )
            {
                return;
            }

            var host = new GameObject("BuildSmokeShot");
            UnityEngine.Object.DontDestroyOnLoad(host);
            host.AddComponent<Shooter>().Begin(level, path, seconds);
        }

        private sealed class Shooter : MonoBehaviour
        {
            public void Begin(int level, string path, float seconds)
            {
                StartCoroutine(Shoot(level, path, seconds));
            }

            private static IEnumerator Shoot(int level, string path, float seconds)
            {
                if (level == MenuLevel)
                {
                    GameFlow.LoadMenu();
                }
                else
                {
                    GameFlow.LoadLevel(level);
                }

                yield return new WaitForSecondsRealtime(seconds);
                ScreenCapture.CaptureScreenshot(path);
                // a captura é gravada no fim do quadro seguinte
                yield return new WaitForSecondsRealtime(1f);
                Application.Quit();
            }
        }
    }
}
