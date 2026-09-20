using System;
using System.Collections;
using HealthyRace.Player;
using HealthyRace.Visuals;
using UnityEngine;

namespace HealthyRace.Obstacles
{
    public enum FallingPlatformState
    {
        Idle,
        Shaking,
        Fallen,
    }

    /// <summary>
    /// Crumbling / falling platform. When stepped on by a player from above, shakes during a 0.5s anticipation window,
    /// then collapses/disappears for 3.0s before respawning back in place.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    [DisallowMultipleComponent]
    public sealed class FallingPlatform : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField, Min(0.1f)]
        private float _anticipationDuration = 0.5f;

        [SerializeField, Min(0.5f)]
        private float _respawnDelay = 3f;

        [Header("Shake")]
        [SerializeField, Min(0.01f)]
        private float _shakeIntensity = 0.05f;

        [Header("References")]
        [SerializeField]
        private SpriteRenderer _visualRenderer;

        [SerializeField]
        private HazardPresentation _hazardPresentation;

        private Vector3 _initialPosition;
        private BoxCollider2D _collider;
        private FallingPlatformState _state = FallingPlatformState.Idle;
        private Coroutine _cycleRoutine;

        public FallingPlatformState State => _state;
        public float AnticipationDuration => _anticipationDuration;
        public float RespawnDelay => _respawnDelay;
        public Vector3 InitialPosition => _initialPosition;

        public event Action PlatformTriggered;
        public event Action PlatformFell;
        public event Action PlatformRespawned;

        private void Awake()
        {
            _collider = GetComponent<BoxCollider2D>();
            _initialPosition = transform.position;

            if (_visualRenderer == null)
            {
                _visualRenderer = GetComponent<SpriteRenderer>();
            }

            if (_hazardPresentation == null)
            {
                _hazardPresentation = GetComponent<HazardPresentation>();
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandleCollision(collision);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            HandleCollision(collision);
        }

        private void HandleCollision(Collision2D collision)
        {
            if (_state != FallingPlatformState.Idle)
            {
                return;
            }

            var playerController = collision.gameObject.GetComponentInParent<PlayerController>();
            var playerMovement = collision.gameObject.GetComponentInParent<PlayerMovement>();

            if (playerController == null && playerMovement == null)
            {
                return;
            }

            // Check if player lands from above
            for (var i = 0; i < collision.contactCount; i++)
            {
                var contact = collision.GetContact(i);
                // Normal pointing up indicates contact is on top surface
                if (contact.normal.y < -0.3f || contact.point.y >= transform.position.y)
                {
                    TriggerFall();
                    return;
                }
            }
        }

        public void TriggerFall()
        {
            if (_state != FallingPlatformState.Idle)
            {
                return;
            }

            if (_cycleRoutine != null)
            {
                StopCoroutine(_cycleRoutine);
            }

            _cycleRoutine = StartCoroutine(FallCycleRoutine());
        }

        private IEnumerator FallCycleRoutine()
        {
            _state = FallingPlatformState.Shaking;
            PlatformTriggered?.Invoke();

            var elapsed = 0f;
            while (elapsed < _anticipationDuration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / _anticipationDuration);

                if (_hazardPresentation != null)
                {
                    _hazardPresentation.PresentWarning(progress);
                }
                else
                {
                    var shakeOffset = Mathf.Sin(Time.time * 50f) * _shakeIntensity;
                    transform.position = _initialPosition + new Vector3(shakeOffset, 0f, 0f);
                }

                yield return null;
            }

            // Collapse phase
            _state = FallingPlatformState.Fallen;
            transform.position = _initialPosition;

            if (_collider != null)
            {
                _collider.enabled = false;
            }

            if (_visualRenderer != null)
            {
                _visualRenderer.enabled = false;
            }

            if (_hazardPresentation != null)
            {
                _hazardPresentation.PresentWarning(0f);
            }

            PlatformFell?.Invoke();

            // Wait before respawning
            yield return new WaitForSeconds(_respawnDelay);

            // Respawn phase
            ResetPlatform();
            PlatformRespawned?.Invoke();
        }

        public void ResetPlatform()
        {
            if (_cycleRoutine != null)
            {
                StopCoroutine(_cycleRoutine);
                _cycleRoutine = null;
            }

            _state = FallingPlatformState.Idle;
            transform.position = _initialPosition;

            if (_collider != null)
            {
                _collider.enabled = true;
            }

            if (_visualRenderer != null)
            {
                _visualRenderer.enabled = true;
            }

            if (_hazardPresentation != null)
            {
                _hazardPresentation.PresentWarning(0f);
            }
        }

        public void Configure(
            float anticipationDuration,
            float respawnDelay,
            float shakeIntensity = 0.05f
        )
        {
            _anticipationDuration = Mathf.Max(0.05f, anticipationDuration);
            _respawnDelay = Mathf.Max(0.1f, respawnDelay);
            _shakeIntensity = Mathf.Max(0.001f, shakeIntensity);
            _initialPosition = transform.position;
        }

        private void OnDisable()
        {
            ResetPlatform();
        }
    }
}
