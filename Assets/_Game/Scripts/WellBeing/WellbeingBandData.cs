using UnityEngine;

namespace HealthyRace.WellBeing
{
    public enum PlayerVisualForm
    {
        Vita,
        Salo,
    }

    /// <summary>
    /// Designer-editable definition of one wellbeing range, including its gameplay
    /// multipliers and placeholder-ready visual presentation.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_Wellbeing_NewBand", menuName = "Healthy Race/Wellbeing Band")]
    public sealed class WellbeingBandData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField]
        private string _id;

        [SerializeField]
        private string _displayName;

        [SerializeField, Range(WellbeingComponent.MinimumValue, WellbeingComponent.MaximumValue)]
        private int _minValue;

        [SerializeField, Range(WellbeingComponent.MinimumValue, WellbeingComponent.MaximumValue)]
        private int _maxValue;

        [Header("Gameplay")]
        [SerializeField, Min(0f)]
        private float _movementMultiplier = 1f;

        [SerializeField, Min(0f)]
        private float _jumpMultiplier = 1f;

        [Header("Visuals")]
        [SerializeField]
        private PlayerVisualForm _visualForm;

        [SerializeField]
        private Color _characterColor = Color.white;

        [SerializeField]
        private Color _leafColor = Color.white;

        [SerializeField]
        private Color _glowColor = Color.clear;

        [SerializeField, Min(0f)]
        private float _glowScale = 1f;

        [SerializeField, Min(0f)]
        private float _leafScale = 1f;

        [SerializeField]
        private bool _showTiredEyes;

        [SerializeField]
        private Sprite _spriteOverride;

        [SerializeField]
        private Material _materialOverride;

        [SerializeField]
        private GameObject _visualEffectPrefab;

        public Color CharacterColor => _characterColor;

        public string DisplayName => _displayName;

        public float GlowScale => _glowScale;

        public Color GlowColor => _glowColor;

        public string Id => _id;

        public float JumpMultiplier => _jumpMultiplier;

        public Color LeafColor => _leafColor;

        public float LeafScale => _leafScale;

        public Material MaterialOverride => _materialOverride;

        public int MaxValue => _maxValue;

        public int MinValue => _minValue;

        public float MovementMultiplier => _movementMultiplier;

        public bool ShowTiredEyes => _showTiredEyes;

        public Sprite SpriteOverride => _spriteOverride;

        public GameObject VisualEffectPrefab => _visualEffectPrefab;

        public PlayerVisualForm VisualForm => _visualForm;

        public bool Contains(int wellbeingValue)
        {
            return wellbeingValue >= _minValue && wellbeingValue <= _maxValue;
        }
    }
}
