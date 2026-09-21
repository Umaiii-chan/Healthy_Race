using UnityEngine;

namespace HealthyRace.Scoring
{
    /// <summary>
    /// Configurable balancing parameters for the scoring system.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_ScoreConfig", menuName = "Healthy Race/Score Config")]
    public class ScoreConfig : ScriptableObject
    {
        [Header("Bonuses")]
        [SerializeField, Min(0)]
        private int _pointsPerPuzzle = 150;

        [SerializeField, Min(0)]
        private int _maxTimeBonus = 600;

        [SerializeField, Min(0)]
        private float _targetTimeSeconds = 180f;

        [SerializeField, Min(0)]
        private int _firstPlaceArrivalBonus = 200;

        [SerializeField, Min(0)]
        private int _secondPlaceArrivalBonus = 100;

        [SerializeField, Min(0)]
        private int _wellbeingMultiplier = 5;

        [Header("Penalties")]
        [SerializeField, Min(0)]
        private int _ultraPenaltyPerItem = 100;

        [SerializeField, Min(0)]
        private int _fatiguePenaltyPerEpisode = 200;

        [SerializeField, Min(0)]
        private int _dnfPenalty = 500;

        public int PointsPerPuzzle => _pointsPerPuzzle;
        public int MaxTimeBonus => _maxTimeBonus;
        public float TargetTimeSeconds => _targetTimeSeconds;
        public int FirstPlaceArrivalBonus => _firstPlaceArrivalBonus;
        public int SecondPlaceArrivalBonus => _secondPlaceArrivalBonus;
        public int WellbeingMultiplier => _wellbeingMultiplier;
        public int UltraPenaltyPerItem => _ultraPenaltyPerItem;
        public int FatiguePenaltyPerEpisode => _fatiguePenaltyPerEpisode;
        public int DnfPenalty => _dnfPenalty;

        public static ScoreConfig CreateDefault()
        {
            var config = CreateInstance<ScoreConfig>();
            config._pointsPerPuzzle = 150;
            config._maxTimeBonus = 600;
            config._targetTimeSeconds = 180f;
            config._firstPlaceArrivalBonus = 200;
            config._secondPlaceArrivalBonus = 100;
            config._wellbeingMultiplier = 5;
            config._ultraPenaltyPerItem = 100;
            config._fatiguePenaltyPerEpisode = 200;
            config._dnfPenalty = 500;
            return config;
        }
    }
}
