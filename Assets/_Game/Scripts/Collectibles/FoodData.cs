using UnityEngine;

namespace HealthyRace.Collectibles
{
    public enum FoodCategory
    {
        Healthy,
        UltraProcessed,
    }

    /// <summary>
    /// Immutable design data for a food pickup. Runtime state belongs to the pickup
    /// component, never to this shared asset.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_Food_New", menuName = "Healthy Race/Food Data")]
    public sealed class FoodData : ScriptableObject
    {
        [SerializeField]
        private string _id;

        [SerializeField]
        private string _displayName;

        [SerializeField]
        private FoodCategory _category;

        [SerializeField]
        private int _basePoints;

        [SerializeField]
        private int _wellbeingDelta;

        public int BasePoints => _basePoints;

        public FoodCategory Category => _category;

        public string DisplayName => _displayName;

        public string Id => _id;

        public int WellbeingDelta => _wellbeingDelta;
    }
}
