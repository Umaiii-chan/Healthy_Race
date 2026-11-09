using HealthyRace.Audio;
using UnityEngine;

namespace HealthyRace.UI
{
    /// <summary>Cena de entrada do jogo: câmera, telas no menu principal e música.</summary>
    [DisallowMultipleComponent]
    public sealed class MainMenuBootstrap : MonoBehaviour
    {
        [SerializeField]
        private GameObject _screensPrefab;

        private GameScreens _screens;

        public GameScreens Screens => _screens;

        private void Awake()
        {
            if (Camera.main == null)
            {
                var cameraObject = new GameObject(
                    "Menu Camera",
                    typeof(Camera),
                    typeof(AudioListener)
                );
                cameraObject.tag = "MainCamera";
                cameraObject.transform.SetParent(transform, false);
                var menuCamera = cameraObject.GetComponent<Camera>();
                menuCamera.orthographic = true;
                menuCamera.clearFlags = CameraClearFlags.SolidColor;
                menuCamera.backgroundColor = new Color32(21, 40, 35, 255);
            }

            if (_screensPrefab == null)
            {
                Debug.LogError("MainMenuBootstrap: prefab de telas não atribuído.", this);
                return;
            }

            _screens = GameScreens.Create(_screensPrefab, null, false, transform);
        }

        private void Start()
        {
            AudioService.Instance.PlayMusic(AudioClipId.Music_Menu);
        }
    }
}
