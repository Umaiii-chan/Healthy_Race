using System.Globalization;
using UnityEngine;

namespace HealthyRace.Scoring
{
    public enum RaceVerdict
    {
        /// <summary>Os dois chegaram; venceu quem chegou antes.</summary>
        Arrival,

        /// <summary>Só um chegou à porta.</summary>
        OnlyFinisher,

        /// <summary>Chegaram no mesmo instante; decidiu o Bem-Estar final.</summary>
        WellbeingTieBreak,

        /// <summary>Chegaram no mesmo instante e com o mesmo Bem-Estar; decidiu ter menos fadigas.</summary>
        FatigueTieBreak,

        /// <summary>Chegaram no mesmo instante e empataram em tudo.</summary>
        Tie,

        NobodyFinished,
    }

    public readonly struct RaceResult
    {
        public RaceResult(int winner, RaceVerdict verdict, float marginSeconds)
        {
            Winner = winner;
            Verdict = verdict;
            MarginSeconds = marginSeconds;
        }

        /// <summary>1, 2 ou 0 (empate).</summary>
        public int Winner { get; }

        public RaceVerdict Verdict { get; }

        /// <summary>Diferença de chegada em segundos (0 se alguém não chegou).</summary>
        public float MarginSeconds { get; }
    }

    /// <summary>
    /// Quem vence a corrida (decisão D1): <b>quem chega primeiro à própria porta</b>.
    /// Os pontos continuam sendo calculados por <see cref="ScoreService"/>, mas não
    /// decidem a fase. Cada jogador ganha até três estrelas, independentes do rival.
    /// </summary>
    public static class RaceOutcome
    {
        /// <summary>Chegadas a menos disto uma da outra contam como simultâneas (meio passo de física).</summary>
        public const float SameInstantSeconds = 0.011f;

        public const int MaxStars = 3;

        public static RaceResult Decide(ScoreInput p1, ScoreInput p2)
        {
            if (!p1.Finished && !p2.Finished)
            {
                return new RaceResult(0, RaceVerdict.NobodyFinished, 0f);
            }

            if (p1.Finished != p2.Finished)
            {
                return new RaceResult(
                    p1.Finished ? p1.PlayerIndex : p2.PlayerIndex,
                    RaceVerdict.OnlyFinisher,
                    0f
                );
            }

            var margin = Mathf.Abs(p1.FinishTime - p2.FinishTime);
            if (margin > SameInstantSeconds)
            {
                return new RaceResult(
                    p1.FinishTime < p2.FinishTime ? p1.PlayerIndex : p2.PlayerIndex,
                    RaceVerdict.Arrival,
                    margin
                );
            }

            if (p1.FinalWellbeing != p2.FinalWellbeing)
            {
                return new RaceResult(
                    p1.FinalWellbeing > p2.FinalWellbeing ? p1.PlayerIndex : p2.PlayerIndex,
                    RaceVerdict.WellbeingTieBreak,
                    margin
                );
            }

            if (p1.FatigueEpisodesCount != p2.FatigueEpisodesCount)
            {
                return new RaceResult(
                    p1.FatigueEpisodesCount < p2.FatigueEpisodesCount
                        ? p1.PlayerIndex
                        : p2.PlayerIndex,
                    RaceVerdict.FatigueTieBreak,
                    margin
                );
            }

            return new RaceResult(0, RaceVerdict.Tie, margin);
        }

        /// <summary>
        /// Estrelas do jogador: chegou · terminou com Bem-Estar a partir de
        /// <paramref name="wellbeingThreshold"/> · chegou até o tempo-alvo da fase.
        /// Quem não chega não ganha nenhuma.
        /// </summary>
        public static int Stars(ScoreInput player, float targetTimeSeconds, int wellbeingThreshold)
        {
            if (!player.Finished)
            {
                return 0;
            }

            var stars = 1;
            if (player.FinalWellbeing >= wellbeingThreshold)
            {
                stars++;
            }

            if (targetTimeSeconds > 0f && player.FinishTime <= targetTimeSeconds)
            {
                stars++;
            }

            return stars;
        }

        /// <summary>Uma frase que diz por que a corrida terminou assim.</summary>
        public static string Describe(RaceResult result)
        {
            var winner = $"P{result.Winner}";
            return result.Verdict switch
            {
                RaceVerdict.Arrival => $"{winner} chegou {Seconds(result.MarginSeconds)} antes.",
                RaceVerdict.OnlyFinisher => $"Só {winner} chegou à porta.",
                RaceVerdict.WellbeingTieBreak =>
                    $"Chegaram juntos; {winner} venceu por terminar com mais Bem-Estar.",
                RaceVerdict.FatigueTieBreak =>
                    $"Chegaram juntos; {winner} venceu por ter tido menos fadigas.",
                RaceVerdict.Tie => "Chegaram juntos e empataram em tudo.",
                _ => "Ninguém chegou à porta.",
            };
        }

        private static string Seconds(float seconds) =>
            seconds.ToString("0.0", CultureInfo.GetCultureInfo("pt-BR")) + " s";
    }
}
