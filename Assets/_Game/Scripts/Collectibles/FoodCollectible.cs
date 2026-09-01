using HealthyRace.WellBeing;
using UnityEngine;

namespace HealthyRace.Collectibles
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class FoodCollectible : MonoBehaviour
    {
        [SerializeField]
        private FoodData _foodData;

        private bool _isCollected;

        public FoodData FoodData => _foodData;

        public void Configure(FoodData foodData)
        {
            _foodData = foodData;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isCollected || _foodData == null)
            {
                return;
            }

            var wellbeing = other.GetComponentInParent<WellbeingComponent>();

            if (wellbeing == null)
            {
                return;
            }

            _isCollected = true;
            wellbeing.ApplyDelta(_foodData.WellbeingDelta);
            gameObject.SetActive(false);
        }
    }
}
