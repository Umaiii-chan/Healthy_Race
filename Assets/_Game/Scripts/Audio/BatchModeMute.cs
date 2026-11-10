using UnityEngine;

namespace HealthyRace.Audio
{
    /// <summary>
    /// Em modo batch (testes automáticos, CI, build por linha de comando) o jogo não
    /// toca som nenhum. Só a saída é zerada: volumes, fontes e clipes continuam
    /// configurados, então os testes de áudio seguem valendo.
    /// </summary>
    public static class BatchModeMute
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void MuteWhenUnattended()
        {
            if (Application.isBatchMode)
            {
                AudioListener.volume = 0f;
            }
        }
    }
}
