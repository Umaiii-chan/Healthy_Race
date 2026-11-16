using System.Collections.Generic;
using UnityEngine;

namespace HealthyRace.Scoring
{
    /// <summary>
    /// Quem estava na frente ao longo da corrida. Guarda amostras do progresso dos dois
    /// jogadores e responde o que a tela de resultado mostra: a liderança em cada trecho
    /// e o momento em que ela mudou de mão pela última vez.
    /// </summary>
    public sealed class RaceTrace
    {
        /// <summary>Intervalo mínimo entre duas amostras.</summary>
        public const float SampleSeconds = 0.25f;

        /// <summary>Diferença de progresso abaixo da qual os dois estão juntos.</summary>
        public const float TogetherMargin = 0.004f;

        private readonly struct Sample
        {
            public Sample(float time, float one, float two, bool rushOne, bool rushTwo)
            {
                Time = time;
                One = one;
                Two = two;
                RushOne = rushOne;
                RushTwo = rushTwo;
            }

            public float Time { get; }
            public float One { get; }
            public float Two { get; }
            public bool RushOne { get; }
            public bool RushTwo { get; }

            /// <summary>Positivo com o jogador 1 na frente; negativo com o jogador 2.</summary>
            public float Lead => One - Two;

            /// <summary>Onde a corrida está: o progresso de quem vai na frente.</summary>
            public float Front => Mathf.Max(One, Two);
        }

        private readonly List<Sample> _samples = new();

        public int Count => _samples.Count;

        /// <summary>
        /// Registra o progresso (0 a 1) dos dois jogadores. Amostras mais próximas que
        /// <see cref="SampleSeconds"/> da anterior são ignoradas.
        /// </summary>
        public void Record(float time, float one, float two, bool rushOne, bool rushTwo)
        {
            if (_samples.Count > 0 && time - _samples[_samples.Count - 1].Time < SampleSeconds)
            {
                return;
            }

            _samples.Add(
                new Sample(time, Mathf.Clamp01(one), Mathf.Clamp01(two), rushOne, rushTwo)
            );
        }

        /// <summary>
        /// A liderança ao longo do percurso, em <paramref name="points"/> pontos igualmente
        /// espaçados da largada à chegada: positivo com o jogador 1 na frente.
        /// </summary>
        public float[] Leads(int points)
        {
            points = Mathf.Max(2, points);
            var leads = new float[points];
            if (_samples.Count == 0)
            {
                return leads;
            }

            var end = Mathf.Max(0.0001f, _samples[_samples.Count - 1].Front);
            var cursor = 0;
            for (var i = 0; i < points; i++)
            {
                var position = end * i / (points - 1);
                while (cursor < _samples.Count - 1 && _samples[cursor].Front < position)
                {
                    cursor++;
                }

                leads[i] = _samples[cursor].Lead;
            }

            return leads;
        }

        /// <summary>
        /// A última troca de liderança: em que ponto do percurso (0 a 1) aconteceu, quem
        /// passou à frente e se estava no impulso do lanche. Falso se a frente nunca mudou.
        /// </summary>
        public bool TryLastLeadChange(out float position, out int newLeader, out bool withRush)
        {
            position = 0f;
            newLeader = 0;
            withRush = false;
            var found = false;
            var leader = 0;
            var end = _samples.Count > 0 ? Mathf.Max(0.0001f, _samples[^1].Front) : 1f;
            foreach (var sample in _samples)
            {
                var now =
                    sample.Lead > TogetherMargin ? 1
                    : sample.Lead < -TogetherMargin ? 2
                    : 0;
                if (now == 0 || now == leader)
                {
                    continue;
                }

                // a primeira vez que alguém abre vantagem não é troca: é a largada
                if (leader != 0)
                {
                    found = true;
                    position = Mathf.Clamp01(sample.Front / end);
                    newLeader = now;
                    withRush = now == 1 ? sample.RushOne : sample.RushTwo;
                }

                leader = now;
            }

            return found;
        }

        /// <summary>
        /// Frase que explica a corrida a partir da liderança, para ancorar no gráfico.
        /// <paramref name="floors"/> é o número de andares do percurso.
        /// </summary>
        public string Describe(int winner, int floors)
        {
            if (TryLastLeadChange(out var position, out var leader, out var withRush))
            {
                var floor = Mathf.Clamp(Mathf.FloorToInt(position * floors) + 1, 1, floors);
                return withRush
                    ? $"O impulso do lanche no {floor}º andar colocou P{leader} na frente."
                    : $"P{leader} assumiu a frente no {floor}º andar.";
            }

            return winner == 0
                ? "Ninguém abriu vantagem de verdade."
                : $"P{winner} liderou do começo ao fim.";
        }
    }
}
