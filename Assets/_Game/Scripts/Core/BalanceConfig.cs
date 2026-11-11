using UnityEngine;

namespace HealthyRace.Core
{
    /// <summary>
    /// Números de balanceamento num asset só (<c>Resources/SO_Balance</c>), para ajustar
    /// entre partidas de playtest sem recompilar. Os valores padrão são os da v1.0.0;
    /// sem o asset, o jogo usa esses padrões.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_Balance", menuName = "Healthy Race/Balance")]
    public sealed class BalanceConfig : ScriptableObject
    {
        public const string ResourcePath = "SO_Balance";

        private static BalanceConfig _current;

        [Header("Corrida")]
        [
            SerializeField,
            Min(1f),
            Tooltip("Prazo do segundo jogador depois da primeira chegada (s).")
        ]
        private float _partnerWindowSeconds = 45f;

        [SerializeField, Min(1f), Tooltip("Duração máxima de uma partida (s).")]
        private float _matchTimeLimitSeconds = 300f;

        [Header("Pontos")]
        [SerializeField, Min(0)]
        private int _pointsPerLever = 150;

        [SerializeField, Min(0)]
        private int _maxTimeBonus = 600;

        [SerializeField, Min(0)]
        private int _firstPlaceArrivalBonus = 500;

        [SerializeField, Min(0)]
        private int _secondPlaceArrivalBonus = 250;

        [SerializeField, Min(0)]
        private int _wellbeingMultiplier = 5;

        [SerializeField, Min(0)]
        private int _ultraPenaltyPerItem = 100;

        [SerializeField, Min(0)]
        private int _fatiguePenaltyPerEpisode = 200;

        [SerializeField, Min(0)]
        private int _dnfPenalty = 500;

        [Header("Lanche rápido: pico e queda")]
        [SerializeField, Range(1f, 2f), Tooltip("Velocidade × este valor no pico.")]
        private float _rushMultiplier = 1.3f;

        [SerializeField, Min(0f), Tooltip("Duração do pico (s); outro lanche renova.")]
        private float _rushSeconds = 2f;

        [SerializeField, Range(0.1f, 1f), Tooltip("Velocidade × este valor na queda.")]
        private float _crashMultiplier = 0.6f;

        [SerializeField, Min(0f), Tooltip("Duração da queda depois de um lanche (s).")]
        private float _crashSeconds = 4f;

        [SerializeField, Min(0f), Tooltip("Queda extra por lanche comido ainda no pico (s).")]
        private float _crashExtraPerChain = 2f;

        [SerializeField, Min(0f), Tooltip("Queda máxima, mesmo encadeando lanches (s).")]
        private float _crashMaxSeconds = 8f;

        [Header("Fadiga")]
        [SerializeField, Min(0f)]
        private float _fatigueDurationSeconds = 2f;

        [SerializeField, Min(0)]
        private int _fatigueRecoveryWellbeing = 15;

        [SerializeField, Range(0, 100)]
        private int _fatigueTriggerThreshold = 20;

        [SerializeField, Range(0, 100)]
        private int _fatigueRearmThreshold = 25;

        /// <summary>O asset de <c>Resources</c>, ou os padrões se ele não existir.</summary>
        public static BalanceConfig Current
        {
            get
            {
                if (_current == null)
                {
                    _current = Resources.Load<BalanceConfig>(ResourcePath);
                    if (_current == null)
                    {
                        _current = CreateInstance<BalanceConfig>();
                    }
                }

                return _current;
            }
        }

        public float PartnerWindowSeconds => _partnerWindowSeconds;
        public float MatchTimeLimitSeconds => _matchTimeLimitSeconds;
        public int PointsPerLever => _pointsPerLever;
        public int MaxTimeBonus => _maxTimeBonus;
        public int FirstPlaceArrivalBonus => _firstPlaceArrivalBonus;
        public int SecondPlaceArrivalBonus => _secondPlaceArrivalBonus;
        public int WellbeingMultiplier => _wellbeingMultiplier;
        public int UltraPenaltyPerItem => _ultraPenaltyPerItem;
        public int FatiguePenaltyPerEpisode => _fatiguePenaltyPerEpisode;
        public int DnfPenalty => _dnfPenalty;
        public float RushMultiplier => _rushMultiplier;
        public float RushSeconds => _rushSeconds;
        public float CrashMultiplier => _crashMultiplier;
        public float CrashSeconds => _crashSeconds;
        public float CrashExtraPerChain => _crashExtraPerChain;
        public float CrashMaxSeconds => _crashMaxSeconds;
        public float FatigueDurationSeconds => _fatigueDurationSeconds;
        public int FatigueRecoveryWellbeing => _fatigueRecoveryWellbeing;
        public int FatigueTriggerThreshold => _fatigueTriggerThreshold;
        public int FatigueRearmThreshold => _fatigueRearmThreshold;

        /// <summary>Troca o balanceamento em uso (testes); <c>null</c> volta ao asset.</summary>
        public static void Use(BalanceConfig config)
        {
            _current = config;
        }
    }
}
