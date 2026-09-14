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

        [Header("Optional Presentation")]
        [SerializeField]
        private Sprite _sprite;

        [SerializeField]
        private AudioClip _collectSound;

        public int BasePoints => _basePoints;

        public FoodCategory Category => _category;

        public string DisplayName => _displayName;

        public string Id => _id;

        public int WellbeingDelta => _wellbeingDelta;

        public Sprite Sprite => _sprite;

        public AudioClip CollectSound => _collectSound;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_id))
            {
                _id = name.ToLowerInvariant();
            }

            if (string.IsNullOrWhiteSpace(_displayName))
            {
                _displayName = name;
            }
        }

        public static FoodData Create(
            string id,
            string displayName,
            FoodCategory category,
            int basePoints,
            int wellbeingDelta,
            Sprite sprite = null,
            AudioClip collectSound = null
        )
        {
            var data = CreateInstance<FoodData>();
            data._id = id;
            data._displayName = displayName;
            data._category = category;
            data._basePoints = basePoints;
            data._wellbeingDelta = wellbeingDelta;
            data._sprite = sprite;
            data._collectSound = collectSound;
            return data;
        }
    }
}
