using UnityEngine;
using UnityEngine.EventSystems;

namespace HealthyRace.Visuals
{
    /// <summary>Cresce o botão sob o mouse ou selecionado pelo teclado.</summary>
    [DisallowMultipleComponent]
    public sealed class UiHoverScale
        : MonoBehaviour,
            IPointerEnterHandler,
            IPointerExitHandler,
            ISelectHandler,
            IDeselectHandler
    {
        [SerializeField, Min(1f)]
        private float _scale = 1.06f;

        [SerializeField, Min(1f)]
        private float _speed = 14f;

        private bool _hovered;
        private bool _selected;

        public void OnPointerEnter(PointerEventData eventData) => _hovered = true;

        public void OnPointerExit(PointerEventData eventData) => _hovered = false;

        public void OnSelect(BaseEventData eventData) => _selected = true;

        public void OnDeselect(BaseEventData eventData) => _selected = false;

        private void OnDisable()
        {
            _hovered = false;
            _selected = false;
            transform.localScale = Vector3.one;
        }

        private void Update()
        {
            var target = Vector3.one * (_hovered || _selected ? _scale : 1f);
            transform.localScale = Vector3.Lerp(
                transform.localScale,
                target,
                1f - Mathf.Exp(-_speed * Time.unscaledDeltaTime)
            );
        }
    }
}
