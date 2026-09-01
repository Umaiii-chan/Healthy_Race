using UnityEngine;

namespace HealthyRace.Core
{
    [RequireComponent(typeof(Camera))]
    public sealed class SharedCameraController : MonoBehaviour
    {
        [SerializeField, Min(0f)]
        private float _positionSmoothTime = 0.18f;

        [SerializeField, Min(0f)]
        private float _minimumOrthographicSize = 5.5f;

        [SerializeField, Min(0f)]
        private float _maximumOrthographicSize = 8.5f;

        [SerializeField, Min(0f)]
        private float _horizontalZoomPadding = 0.3f;

        private Camera _camera;
        private Transform _firstTarget;
        private Transform _secondTarget;
        private Vector3 _positionVelocity;

        public void Configure(Transform firstTarget, Transform secondTarget)
        {
            _firstTarget = firstTarget;
            _secondTarget = secondTarget;
        }

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private void LateUpdate()
        {
            if (_firstTarget == null || _secondTarget == null)
            {
                return;
            }

            var firstPosition = _firstTarget.position;
            var secondPosition = _secondTarget.position;
            var midpoint = (firstPosition + secondPosition) * 0.5f;
            var desiredPosition = new Vector3(midpoint.x, Mathf.Max(midpoint.y, -0.3f), -10f);

            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref _positionVelocity,
                _positionSmoothTime
            );

            var horizontalDistance = Mathf.Abs(firstPosition.x - secondPosition.x);
            var desiredSize =
                _minimumOrthographicSize + horizontalDistance * _horizontalZoomPadding;
            _camera.orthographicSize = Mathf.Clamp(
                desiredSize,
                _minimumOrthographicSize,
                _maximumOrthographicSize
            );
        }
    }
}
