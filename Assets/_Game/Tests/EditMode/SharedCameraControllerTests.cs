using HealthyRace.Core;
using NUnit.Framework;
using UnityEngine;

namespace HealthyRace.Tests.EditMode
{
    public sealed class SharedCameraControllerTests
    {
        private GameObject _cameraObject;
        private Camera _camera;
        private SharedCameraController _controller;
        private GameObject _target1;
        private GameObject _target2;

        [SetUp]
        public void SetUp()
        {
            _cameraObject = new GameObject("TestCamera");
            _camera = _cameraObject.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = 5f;

            _controller = _cameraObject.AddComponent<SharedCameraController>();

            _target1 = new GameObject("Target1");
            _target2 = new GameObject("Target2");

            _controller.Configure(_target1.transform, _target2.transform);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_cameraObject);
            Object.DestroyImmediate(_target1);
            Object.DestroyImmediate(_target2);
        }

        [Test]
        public void DesiredPosition_TracksMidpointOfTargets()
        {
            _target1.transform.position = new Vector3(-4f, 2f, 0f);
            _target2.transform.position = new Vector3(6f, 6f, 0f);

            var desired = _controller.CalculateDesiredPosition();

            Assert.That(desired.x, Is.EqualTo(1f).Within(0.001f));
            Assert.That(desired.y, Is.EqualTo(4f).Within(0.001f));
        }

        [Test]
        public void DesiredSize_IncreasesWhenTargetsSeparate()
        {
            _controller.SetZoomLimits(4f, 12f);
            _controller.SetPadding(new Vector2(1f, 1f));

            _target1.transform.position = new Vector3(-2f, 0f, 0f);
            _target2.transform.position = new Vector3(2f, 0f, 0f);
            var closeSize = _controller.CalculateDesiredOrthographicSize();

            _target1.transform.position = new Vector3(-8f, 0f, 0f);
            _target2.transform.position = new Vector3(8f, 0f, 0f);
            var farSize = _controller.CalculateDesiredOrthographicSize();

            Assert.That(farSize, Is.GreaterThan(closeSize));
        }

        [Test]
        public void DesiredSize_ClampedToMinAndMaxLimits()
        {
            _controller.SetZoomLimits(5f, 9f);
            _controller.SetPadding(new Vector2(0.5f, 0.5f));

            _target1.transform.position = Vector3.zero;
            _target2.transform.position = Vector3.zero;
            Assert.That(_controller.CalculateDesiredOrthographicSize(), Is.EqualTo(5f));

            _target1.transform.position = new Vector3(-100f, 0f, 0f);
            _target2.transform.position = new Vector3(100f, 0f, 0f);
            Assert.That(_controller.CalculateDesiredOrthographicSize(), Is.EqualTo(9f));
        }

        [Test]
        public void DesiredPosition_ClampedToPlayableBounds()
        {
            var bounds = new Bounds(Vector3.zero, new Vector3(20f, 10f, 0f));
            _controller.SetPlayableBounds(bounds, true);

            // Place targets far to the right beyond bounds
            _target1.transform.position = new Vector3(50f, 0f, 0f);
            _target2.transform.position = new Vector3(50f, 0f, 0f);

            var desired = _controller.CalculateDesiredPosition();

            var visibleHalfWidth = _camera.orthographicSize * _camera.aspect;
            var maxAllowedX = bounds.max.x - visibleHalfWidth;

            Assert.That(desired.x, Is.LessThanOrEqualTo(maxAllowedX + 0.001f));
        }

        [Test]
        public void VerticalSeparation_ExpandsOrthographicSize()
        {
            _controller.SetZoomLimits(3f, 15f);
            _controller.SetPadding(new Vector2(1f, 1f));

            _target1.transform.position = new Vector3(0f, -5f, 0f);
            _target2.transform.position = new Vector3(0f, 5f, 0f);

            var size = _controller.CalculateDesiredOrthographicSize();

            // half height = 5 + 1 = 6
            Assert.That(size, Is.GreaterThanOrEqualTo(6f));
        }

        [Test]
        public void AutoFindTargets_FindsPlayer1AndPlayer2()
        {
            var p1 = new GameObject("Player1");
            var p2 = new GameObject("Player2");
            var camObj = new GameObject("AutoFindCamera");

            try
            {
                var unconfiguredController = camObj.AddComponent<SharedCameraController>();
                unconfiguredController.AutoFindTargets();

                Assert.That(unconfiguredController.FirstTarget, Is.EqualTo(p1.transform));
                Assert.That(unconfiguredController.SecondTarget, Is.EqualTo(p2.transform));
            }
            finally
            {
                Object.DestroyImmediate(p1);
                Object.DestroyImmediate(p2);
                Object.DestroyImmediate(camObj);
            }
        }
    }
}
