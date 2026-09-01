using HealthyRace.Core;
using HealthyRace.WellBeing;
using UnityEngine;

namespace HealthyRace.Player
{
    [RequireComponent(typeof(SpriteRenderer), typeof(WellbeingComponent))]
    public sealed class PlayerVisualController : MonoBehaviour
    {
        private const int EyeSortingOrder = 5;
        private const int GlowSortingOrder = 1;
        private const int LeafSortingOrder = 4;

        private GameObject _activeVisualEffect;
        private TextMesh _fatigueLabel;
        private FatigueComponent _fatigue;
        private SpriteRenderer _glowRenderer;
        private SpriteRenderer _leafRenderer;
        private SpriteRenderer _leftEyeRenderer;
        private SpriteRenderer _playerRenderer;
        private SpriteRenderer _rightEyeRenderer;
        private WellbeingComponent _wellbeing;

        private void Awake()
        {
            _fatigue = GetComponent<FatigueComponent>();
            _playerRenderer = GetComponent<SpriteRenderer>();
            _wellbeing = GetComponent<WellbeingComponent>();
            CreatePlaceholderVisuals();
            ApplyBand(_wellbeing.CurrentBand);
            ApplyFatigueState(_fatigue?.State ?? FatigueState.Normal);
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnEnable()
        {
            _wellbeing.WellbeingBandChanged += ApplyBand;

            if (_fatigue != null)
            {
                _fatigue.StateChanged += ApplyFatigueState;
            }
        }

        private void ApplyBand(WellbeingBandData band)
        {
            if (band == null)
            {
                return;
            }

            _playerRenderer.sprite =
                band.SpriteOverride != null ? band.SpriteOverride
                : band.VisualForm == PlayerVisualForm.Salo ? PlaceholderSpriteFactory.Circle
                : PlaceholderSpriteFactory.Square;
            _playerRenderer.color = band.CharacterColor;

            if (band.MaterialOverride != null)
            {
                _playerRenderer.material = band.MaterialOverride;
            }

            _glowRenderer.color = band.GlowColor;
            _glowRenderer.transform.localScale = Vector3.one * band.GlowScale;
            _leafRenderer.color = band.LeafColor;
            _leafRenderer.transform.localScale = new Vector3(
                0.33f * band.LeafScale,
                0.16f * band.LeafScale,
                1f
            );

            var showTiredEyes = band.ShowTiredEyes;
            _leftEyeRenderer.gameObject.SetActive(showTiredEyes);
            _rightEyeRenderer.gameObject.SetActive(showTiredEyes);
            ReplaceVisualEffect(band.VisualEffectPrefab);
        }

        private void ApplyFatigueState(FatigueState fatigueState)
        {
            _fatigueLabel.gameObject.SetActive(fatigueState == FatigueState.Fatigued);
        }

        private void CreatePlaceholderVisuals()
        {
            _glowRenderer = CreateSpriteChild(
                "Wellbeing Glow",
                PlaceholderSpriteFactory.Circle,
                Vector2.zero,
                Vector2.one,
                GlowSortingOrder
            );
            _leafRenderer = CreateSpriteChild(
                "Wellbeing Leaf",
                PlaceholderSpriteFactory.Square,
                new Vector2(0.28f, 0.57f),
                new Vector2(0.33f, 0.16f),
                LeafSortingOrder
            );
            _leafRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, -25f);
            _leftEyeRenderer = CreateSpriteChild(
                "Tired Left Eye",
                PlaceholderSpriteFactory.Circle,
                new Vector2(-0.2f, 0.08f),
                new Vector2(0.11f, 0.11f),
                EyeSortingOrder
            );
            _rightEyeRenderer = CreateSpriteChild(
                "Tired Right Eye",
                PlaceholderSpriteFactory.Circle,
                new Vector2(0.2f, 0.08f),
                new Vector2(0.11f, 0.11f),
                EyeSortingOrder
            );
            _leftEyeRenderer.color = new Color(0.12f, 0.12f, 0.16f);
            _rightEyeRenderer.color = new Color(0.12f, 0.12f, 0.16f);

            var labelObject = new GameObject("Fatigue Label");
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = new Vector2(0f, 1.18f);

            _fatigueLabel = labelObject.AddComponent<TextMesh>();
            _fatigueLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _fatigueLabel.text = "FADIGA!";
            _fatigueLabel.characterSize = 0.18f;
            _fatigueLabel.fontSize = 60;
            _fatigueLabel.anchor = TextAnchor.MiddleCenter;
            _fatigueLabel.alignment = TextAlignment.Center;
            _fatigueLabel.color = new Color(1f, 0.65f, 0.15f);
            _fatigueLabel.GetComponent<MeshRenderer>().sortingOrder = 7;
        }

        private SpriteRenderer CreateSpriteChild(
            string objectName,
            Sprite sprite,
            Vector2 localPosition,
            Vector2 localScale,
            int sortingOrder
        )
        {
            var child = new GameObject(objectName);
            child.transform.SetParent(transform, false);
            child.transform.localPosition = localPosition;
            child.transform.localScale = localScale;

            var spriteRenderer = child.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.sortingOrder = sortingOrder;
            return spriteRenderer;
        }

        private void ReplaceVisualEffect(GameObject visualEffectPrefab)
        {
            if (_activeVisualEffect != null)
            {
                Destroy(_activeVisualEffect);
                _activeVisualEffect = null;
            }

            if (visualEffectPrefab != null)
            {
                _activeVisualEffect = Instantiate(visualEffectPrefab, transform);
            }
        }

        private void Unsubscribe()
        {
            if (_wellbeing != null)
            {
                _wellbeing.WellbeingBandChanged -= ApplyBand;
            }

            if (_fatigue != null)
            {
                _fatigue.StateChanged -= ApplyFatigueState;
            }
        }
    }
}
