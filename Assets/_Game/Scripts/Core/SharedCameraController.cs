using UnityEngine;

namespace HealthyRace.Core
{
    /// <summary>
    /// Smoothly frames two player targets with dynamic orthographic zoom and playable area bounds confinement.
    /// Completely decoupled from player movement mechanics.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    public sealed class SharedCameraController : MonoBehaviour
    {
        [Header("Targets")]
        [SerializeField]
        private Transform _firstTarget;

        [SerializeField]
        private Transform _secondTarget;

        [Header("Smoothing")]
        [SerializeField, Min(0f)]
        private float _positionSmoothTime = 0.18f;

        [SerializeField, Min(0f)]
        private float _zoomSmoothTime = 0.15f;

        [Header("Zoom Limits & Padding")]
        [SerializeField, Min(1f)]
        private float _minimumOrthographicSize = 5f;

        [SerializeField, Min(1f)]
        private float _maximumOrthographicSize = 9f;

        [SerializeField]
        private Vector2 _padding = new Vector2(2f, 1.5f);

        [Header("Playable Area Bounds")]
        [SerializeField]
        private bool _usePlayableBounds;

        [SerializeField]
        private Bounds _playableBounds;

        private Camera _camera;
        private Vector3 _positionVelocity;
        private float _zoomVelocity;

        public Camera CameraComponent
        {
            get
            {
                if (_camera == null)
                {
                    _camera = GetComponent<Camera>();
                }
                return _camera;
            }
        }

        public Transform FirstTarget => _firstTarget;
        public Transform SecondTarget => _secondTarget;
        public float MinimumOrthographicSize => _minimumOrthographicSize;
        public float MaximumOrthographicSize => _maximumOrthographicSize;
        public Vector2 Padding => _padding;
        public bool UsePlayableBounds => _usePlayableBounds;
        public Bounds PlayableBounds => _playableBounds;

        public void Configure(Transform firstTarget, Transform secondTarget)
        {
            _firstTarget = firstTarget;
            _secondTarget = secondTarget;
        }

        public void SetPlayableBounds(Bounds bounds, bool enableBounds = true)
        {
            _playableBounds = bounds;
            _usePlayableBounds = enableBounds;
        }

        public void SetZoomLimits(float minSize, float maxSize)
        {
            _minimumOrthographicSize = Mathf.Max(0.1f, minSize);
            _maximumOrthographicSize = Mathf.Max(_minimumOrthographicSize, maxSize);
        }

        public void SetPadding(Vector2 padding)
        {
            _padding = padding;
        }

        public void SetSmoothTimes(float positionSmoothTime, float zoomSmoothTime)
        {
            _positionSmoothTime = Mathf.Max(0f, positionSmoothTime);
            _zoomSmoothTime = Mathf.Max(0f, zoomSmoothTime);
        }

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private void Start()
        {
            if (_firstTarget == null || _secondTarget == null)
            {
                AutoFindTargets();
            }
        }

        public void AutoFindTargets()
        {
            if (_firstTarget == null)
            {
                var p1 = GameObject.Find("Player1");
                if (p1 != null)
                {
                    _firstTarget = p1.transform;
                }
            }

            if (_secondTarget == null)
            {
                var p2 = GameObject.Find("Player2");
                if (p2 != null)
                {
                    _secondTarget = p2.transform;
                }
            }
        }

        private void LateUpdate()
        {
            ExecuteCameraUpdate(Time.deltaTime);
        }

        public void ExecuteCameraUpdate(float deltaTime)
        {
            if (_firstTarget == null || _secondTarget == null)
            {
                AutoFindTargets();
            }

            var cam = CameraComponent;
            if (_firstTarget == null || _secondTarget == null || cam == null)
            {
                return;
            }

            UpdateZoom(deltaTime);
            UpdatePosition(deltaTime);
        }

        public Vector3 CalculateDesiredPosition()
        {
            if (_firstTarget == null || _secondTarget == null)
            {
                return transform.position;
            }

            var midpoint = (_firstTarget.position + _secondTarget.position) * 0.5f;
            var targetPosition = new Vector3(midpoint.x, midpoint.y, transform.position.z);

            if (_usePlayableBounds)
            {
                var cam = CameraComponent;
                var orthoSize = cam != null ? cam.orthographicSize : _minimumOrthographicSize;
                targetPosition = ClampPositionToBounds(targetPosition, orthoSize);
            }

            return targetPosition;
        }

        public float CalculateDesiredOrthographicSize()
        {
            var cam = CameraComponent;
            if (_firstTarget == null || _secondTarget == null || cam == null)
            {
                return _minimumOrthographicSize;
            }

            var p1 = _firstTarget.position;
            var p2 = _secondTarget.position;

            var halfDistX = Mathf.Abs(p1.x - p2.x) * 0.5f + _padding.x;
            var halfDistY = Mathf.Abs(p1.y - p2.y) * 0.5f + _padding.y;

            var aspect = cam.aspect > 0.01f ? cam.aspect : (16f / 9f);
            var requiredSizeByWidth = halfDistX / aspect;
            var requiredSizeByHeight = halfDistY;

            var desiredSize = Mathf.Max(
                requiredSizeByWidth,
                requiredSizeByHeight,
                _minimumOrthographicSize
            );
            return Mathf.Clamp(desiredSize, _minimumOrthographicSize, _maximumOrthographicSize);
        }

        private void UpdateZoom(float deltaTime)
        {
            var cam = CameraComponent;
            if (cam == null)
            {
                return;
            }

            var desiredSize = CalculateDesiredOrthographicSize();

            if (deltaTime <= 0f || _zoomSmoothTime <= 0f)
            {
                cam.orthographicSize = desiredSize;
                _zoomVelocity = 0f;
            }
            else
            {
                cam.orthographicSize = Mathf.SmoothDamp(
                    cam.orthographicSize,
                    desiredSize,
                    ref _zoomVelocity,
                    _zoomSmoothTime,
                    Mathf.Infinity,
                    deltaTime
                );
            }
        }

        private void UpdatePosition(float deltaTime)
        {
            var desiredPosition = CalculateDesiredPosition();

            if (deltaTime <= 0f || _positionSmoothTime <= 0f)
            {
                transform.position = desiredPosition;
                _positionVelocity = Vector3.zero;
            }
            else
            {
                transform.position = Vector3.SmoothDamp(
                    transform.position,
                    desiredPosition,
                    ref _positionVelocity,
                    _positionSmoothTime,
                    Mathf.Infinity,
                    deltaTime
                );
            }
        }

        public Vector3 ClampPositionToBounds(Vector3 position, float orthoSize)
        {
            var cam = CameraComponent;
            var aspect = cam != null && cam.aspect > 0.01f ? cam.aspect : (16f / 9f);
            var visibleHalfWidth = orthoSize * aspect;
            var visibleHalfHeight = orthoSize;

            float minX = _playableBounds.min.x + visibleHalfWidth;
            float maxX = _playableBounds.max.x - visibleHalfWidth;
            float clampedX;

            if (minX > maxX)
            {
                clampedX = _playableBounds.center.x;
            }
            else
            {
                clampedX = Mathf.Clamp(position.x, minX, maxX);
            }

            float minY = _playableBounds.min.y + visibleHalfHeight;
            float maxY = _playableBounds.max.y - visibleHalfHeight;
            float clampedY;

            if (minY > maxY)
            {
                clampedY = _playableBounds.center.y;
            }
            else
            {
                clampedY = Mathf.Clamp(position.y, minY, maxY);
            }

            return new Vector3(clampedX, clampedY, position.z);
        }

        private void OnDrawGizmosSelected()
        {
            if (_usePlayableBounds)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(_playableBounds.center, _playableBounds.size);
            }

            if (_firstTarget != null && _secondTarget != null)
            {
                Gizmos.color = Color.yellow;
                var midpoint = (_firstTarget.position + _secondTarget.position) * 0.5f;
                Gizmos.DrawSphere(midpoint, 0.2f);
                Gizmos.DrawLine(_firstTarget.position, _secondTarget.position);
            }
        }
    }
}
