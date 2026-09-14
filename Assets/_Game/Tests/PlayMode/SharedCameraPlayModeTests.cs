using System.Collections;
using System.Collections.Generic;
using HealthyRace.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HealthyRace.Tests.PlayMode
{
    public sealed class SharedCameraPlayModeTests
    {
        private readonly List<GameObject> _createdObjects = new();
        private GameObject _cameraObject;
        private Camera _camera;
        private SharedCameraController _controller;
        private GameObject _player1;
        private GameObject _player2;

        [SetUp]
        public void SetUp()
        {
            _cameraObject = new GameObject("TestCamera");
            _camera = _cameraObject.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = 5f;
            _camera.transform.position = new Vector3(0f, 0f, -10f);

            _controller = _cameraObject.AddComponent<SharedCameraController>();
            _createdObjects.Add(_cameraObject);

            _player1 = new GameObject("Player1");
            _player1.transform.position = new Vector3(-2f, 0f, 0f);
            _createdObjects.Add(_player1);

            _player2 = new GameObject("Player2");
            _player2.transform.position = new Vector3(2f, 0f, 0f);
            _createdObjects.Add(_player2);

            _controller.Configure(_player1.transform, _player2.transform);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _createdObjects)
            {
                if (obj != null)
                {
                    Object.DestroyImmediate(obj);
                }
            }
            _createdObjects.Clear();
        }

        [UnityTest]
        public IEnumerator Camera_KeepsBothPlayersWithinViewport_WhenSeparating()
        {
            _controller.SetZoomLimits(4f, 10f);
            _controller.SetPadding(new Vector2(1.5f, 1.5f));
            _controller.SetSmoothTimes(0.05f, 0.05f);

            // Separate players moderately within allowed zoom range
            _player1.transform.position = new Vector3(-6f, 0f, 0f);
            _player2.transform.position = new Vector3(6f, 0f, 0f);

            yield return new WaitForSeconds(0.3f);

            var p1Viewport = _camera.WorldToViewportPoint(_player1.transform.position);
            var p2Viewport = _camera.WorldToViewportPoint(_player2.transform.position);

            Assert.That(
                p1Viewport.x,
                Is.GreaterThan(0f).And.LessThan(1f),
                "Player 1 should be inside horizontal viewport"
            );
            Assert.That(
                p2Viewport.x,
                Is.GreaterThan(0f).And.LessThan(1f),
                "Player 2 should be inside horizontal viewport"
            );
            Assert.That(
                p1Viewport.y,
                Is.GreaterThan(0f).And.LessThan(1f),
                "Player 1 should be inside vertical viewport"
            );
            Assert.That(
                p2Viewport.y,
                Is.GreaterThan(0f).And.LessThan(1f),
                "Player 2 should be inside vertical viewport"
            );
        }

        [UnityTest]
        public IEnumerator Camera_FollowsBothPlayersMovingTogether()
        {
            _controller.SetSmoothTimes(0.05f, 0.05f);

            var initialCamPos = _camera.transform.position;

            // Move both players together to the right
            _player1.transform.position = new Vector3(8f, 2f, 0f);
            _player2.transform.position = new Vector3(12f, 2f, 0f);

            yield return new WaitForSeconds(0.3f);

            Assert.That(_camera.transform.position.x, Is.GreaterThan(initialCamPos.x + 5f));
            Assert.That(_camera.transform.position.y, Is.GreaterThan(initialCamPos.y));
        }

        [UnityTest]
        public IEnumerator Camera_StopsAtPlayableBounds()
        {
            var bounds = new Bounds(Vector3.zero, new Vector3(20f, 12f, 0f));
            _controller.SetPlayableBounds(bounds, true);
            _controller.SetSmoothTimes(0.05f, 0.05f);

            // Move players way past right border
            _player1.transform.position = new Vector3(40f, 0f, 0f);
            _player2.transform.position = new Vector3(42f, 0f, 0f);

            yield return new WaitForSeconds(0.3f);

            var visibleHalfWidth = _camera.orthographicSize * _camera.aspect;
            var maxAllowedX = bounds.max.x - visibleHalfWidth;

            Assert.That(_camera.transform.position.x, Is.LessThanOrEqualTo(maxAllowedX + 0.1f));
        }
    }
}
