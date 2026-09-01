using HealthyRace.Collectibles;
using HealthyRace.Player;
using HealthyRace.UI;
using HealthyRace.WellBeing;
using UnityEngine;
using UnityEngine.UI;

namespace HealthyRace.Core
{
    /// <summary>
    /// Composes the first vertical slice from small reusable prefabs. It is intentionally
    /// scene-local while the player, collectible, wellbeing, HUD, and camera behaviours
    /// stay reusable for future levels.
    /// </summary>
    public sealed class VerticalSliceBootstrap : MonoBehaviour
    {
        private const int GroundLayer = 6;

        [Header("Reusable prefabs")]
        [SerializeField]
        private GameObject _collectiblePrefab;

        [SerializeField]
        private GameObject _playerPrefab;

        [SerializeField]
        private GameObject _pushableBoxPrefab;

        [Header("Food data")]
        [SerializeField]
        private FoodData _appleData;

        [SerializeField]
        private FoodData _burgerData;

        private Font _worldFont;

        private void Awake()
        {
            if (!HasRequiredReferences())
            {
                return;
            }

            Application.targetFrameRate = 60;
            Physics2D.gravity = new Vector2(0f, -18f);

            CreateBackground();
            CreateLevelGeometry();

            var playerOne = CreatePlayer("P1", new Vector2(-6.5f, -1.4f), 1);
            var playerTwo = CreatePlayer("P2", new Vector2(-4.4f, -1.4f), 2);

            CreatePushableBox(new Vector2(3.3f, -2.25f));
            CreateApple(new Vector2(-0.8f, 0.35f));
            CreateBurger(new Vector2(-2.8f, -2.25f));
            CreateBurger(new Vector2(-0.8f, -2.25f));
            CreateBurger(new Vector2(0.8f, 0.45f));
            CreateBurger(new Vector2(1.2f, -2.25f));
            CreateBurger(new Vector2(4.8f, -2.25f));
            CreateBurger(new Vector2(7f, -2.25f));
            CreateBurger(new Vector2(9.2f, -2.25f));

            CreateSharedCamera(playerOne.transform, playerTwo.transform);
            CreateHud(
                playerOne.GetComponent<WellbeingComponent>(),
                playerOne.GetComponent<FatigueComponent>(),
                playerTwo.GetComponent<WellbeingComponent>(),
                playerTwo.GetComponent<FatigueComponent>()
            );
        }

        private void CreateApple(Vector2 position)
        {
            var apple = CreateFood("Apple (+10)", position, _appleData);
            var spriteRenderer = apple.GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = PlaceholderSpriteFactory.Circle;
            spriteRenderer.color = new Color(0.25f, 0.86f, 0.3f);
            spriteRenderer.sortingOrder = 2;
            apple.transform.localScale = new Vector3(0.72f, 0.72f, 1f);

            var leaf = CreateSpriteChild(
                "Leaf",
                apple.transform,
                PlaceholderSpriteFactory.Square,
                new Color(0.08f, 0.42f, 0.15f),
                new Vector2(0.18f, 0.44f),
                new Vector2(0.25f, 0.13f),
                3
            );
            leaf.transform.localRotation = Quaternion.Euler(0f, 0f, -28f);

            CreateWorldLabel(
                "+10",
                apple.transform,
                new Vector2(0f, 0.85f),
                new Color(0.78f, 1f, 0.8f)
            );
        }

        private void CreateBackground()
        {
            var background = new GameObject("Background");
            background.transform.SetParent(transform, false);
            background.transform.position = new Vector3(0f, 0f, 2f);
            background.transform.localScale = new Vector3(28f, 16f, 1f);

            var spriteRenderer = background.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = PlaceholderSpriteFactory.Square;
            spriteRenderer.color = new Color(0.07f, 0.17f, 0.29f);
            spriteRenderer.sortingOrder = -20;
        }

        private void CreateBurger(Vector2 position)
        {
            var burger = CreateFood("Burger (-18)", position, _burgerData);
            burger.GetComponent<SpriteRenderer>().enabled = false;

            CreateSpriteChild(
                "BottomBun",
                burger.transform,
                PlaceholderSpriteFactory.Square,
                new Color(0.95f, 0.58f, 0.19f),
                new Vector2(0f, -0.29f),
                new Vector2(1f, 0.25f),
                2
            );
            CreateSpriteChild(
                "Patty",
                burger.transform,
                PlaceholderSpriteFactory.Square,
                new Color(0.36f, 0.12f, 0.08f),
                new Vector2(0f, -0.08f),
                new Vector2(1.02f, 0.2f),
                3
            );
            CreateSpriteChild(
                "Lettuce",
                burger.transform,
                PlaceholderSpriteFactory.Square,
                new Color(0.25f, 0.82f, 0.24f),
                new Vector2(0f, 0.1f),
                new Vector2(1.1f, 0.12f),
                4
            );
            CreateSpriteChild(
                "TopBun",
                burger.transform,
                PlaceholderSpriteFactory.Circle,
                new Color(1f, 0.67f, 0.24f),
                new Vector2(0f, 0.26f),
                new Vector2(1.05f, 0.55f),
                5
            );

            CreateWorldLabel(
                "-18",
                burger.transform,
                new Vector2(0f, 0.8f),
                new Color(1f, 0.72f, 0.72f)
            );
        }

        private GameObject CreateFood(string objectName, Vector2 position, FoodData foodData)
        {
            var food = Instantiate(_collectiblePrefab, position, Quaternion.identity, transform);
            food.name = objectName;
            food.GetComponent<FoodCollectible>().Configure(foodData);
            return food;
        }

        private void CreateHud(
            WellbeingComponent playerOneWellbeing,
            FatigueComponent playerOneFatigue,
            WellbeingComponent playerTwoWellbeing,
            FatigueComponent playerTwoFatigue
        )
        {
            var canvasObject = new GameObject(
                "HUD",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)
            );
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var canvasScaler = canvasObject.GetComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920f, 1080f);

            CreateCanvasText(
                "SliceTitle",
                canvasObject.transform,
                "HEALTHY RACE — VERTICAL SLICE",
                26,
                FontStyle.Bold,
                Color.white,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -20f),
                new Vector2(520f, 36f),
                TextAnchor.UpperCenter
            );

            CreateCanvasText(
                "Controls",
                canvasObject.transform,
                "P1: A / D + W ou Espaço     |     P2: ← / → + ↑",
                16,
                FontStyle.Normal,
                new Color(0.82f, 0.9f, 1f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 24f),
                new Vector2(560f, 28f),
                TextAnchor.LowerCenter
            );

            var playerOneHud = new GameObject(
                "P1 Wellbeing HUD",
                typeof(RectTransform)
            ).AddComponent<PlayerHUD>();
            playerOneHud.Initialize(
                canvasObject.transform,
                "P1",
                new Color(0.15f, 0.67f, 1f),
                playerOneWellbeing,
                playerOneFatigue,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(22f, -72f)
            );

            var playerTwoHud = new GameObject(
                "P2 Wellbeing HUD",
                typeof(RectTransform)
            ).AddComponent<PlayerHUD>();
            playerTwoHud.Initialize(
                canvasObject.transform,
                "P2",
                new Color(1f, 0.35f, 0.55f),
                playerTwoWellbeing,
                playerTwoFatigue,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-22f, -72f)
            );
        }

        private void CreateLevelGeometry()
        {
            CreateStaticBlock(
                "Ground",
                new Vector2(0f, -3.4f),
                new Vector2(26f, 0.7f),
                new Color(0.12f, 0.48f, 0.36f)
            );
            CreateStaticBlock(
                "Platform",
                new Vector2(-0.8f, -0.55f),
                new Vector2(4.8f, 0.45f),
                new Color(0.16f, 0.63f, 0.47f)
            );
            CreateStaticBlock(
                "LeftMarker",
                new Vector2(-10.8f, -1.7f),
                new Vector2(0.25f, 2.7f),
                new Color(0.09f, 0.34f, 0.31f)
            );
            CreateStaticBlock(
                "RightMarker",
                new Vector2(10.8f, -1.7f),
                new Vector2(0.25f, 2.7f),
                new Color(0.09f, 0.34f, 0.31f)
            );
        }

        private GameObject CreatePlayer(string playerName, Vector2 position, int playerIndex)
        {
            var player = Instantiate(_playerPrefab, position, Quaternion.identity, transform);
            player.name = playerName;

            var rigidbody = player.GetComponent<Rigidbody2D>();
            rigidbody.gravityScale = 2f;
            rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
            rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            player.GetComponent<SpriteRenderer>().sortingOrder = 2;
            player.transform.localScale = new Vector3(0.85f, 1.25f, 1f);

            player.GetComponent<PlayerController>().Configure(playerIndex);
            CreateWorldLabel(playerName, player.transform, new Vector2(0f, 0.86f), Color.white);
            return player;
        }

        private void CreatePushableBox(Vector2 position)
        {
            var box = Instantiate(_pushableBoxPrefab, position, Quaternion.identity, transform);
            box.name = "Pushable Box";
            box.transform.localScale = new Vector3(1.22f, 1.22f, 1f);

            var rigidbody = box.GetComponent<Rigidbody2D>();
            rigidbody.mass = 1.2f;
            rigidbody.gravityScale = 2.4f;
            rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
            rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var spriteRenderer = box.GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = PlaceholderSpriteFactory.Square;
            spriteRenderer.color = new Color(0.72f, 0.39f, 0.13f);
            spriteRenderer.sortingOrder = 1;

            CreateWorldLabel("CAIXA", box.transform, new Vector2(0f, 0.72f), Color.white);
        }

        private void CreateSharedCamera(Transform playerOne, Transform playerTwo)
        {
            var cameraObject = new GameObject(
                "Shared Camera",
                typeof(Camera),
                typeof(AudioListener)
            );
            cameraObject.transform.SetParent(transform, false);
            cameraObject.transform.position = new Vector3(-5.5f, -0.3f, -10f);
            cameraObject.tag = "MainCamera";

            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6.3f;
            camera.backgroundColor = new Color(0.07f, 0.17f, 0.29f);
            camera.clearFlags = CameraClearFlags.SolidColor;

            var controller = cameraObject.AddComponent<SharedCameraController>();
            controller.Configure(playerOne, playerTwo);
        }

        private void CreateStaticBlock(
            string blockName,
            Vector2 position,
            Vector2 size,
            Color color
        )
        {
            var block = new GameObject(blockName);
            block.transform.SetParent(transform, false);
            block.transform.position = position;
            block.transform.localScale = new Vector3(size.x, size.y, 1f);
            block.layer = GroundLayer;

            var spriteRenderer = block.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = PlaceholderSpriteFactory.Square;
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = 0;

            block.AddComponent<BoxCollider2D>();
        }

        private Text CreateCanvasText(
            string objectName,
            Transform parent,
            string value,
            int fontSize,
            FontStyle fontStyle,
            Color color,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 position,
            Vector2 size,
            TextAnchor alignment
        )
        {
            var textObject = new GameObject(objectName, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);

            var rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = anchor;
            rectTransform.anchorMax = anchor;
            rectTransform.pivot = pivot;
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = size;

            var text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = color;
            text.alignment = alignment;
            return text;
        }

        private GameObject CreateSpriteChild(
            string objectName,
            Transform parent,
            Sprite sprite,
            Color color,
            Vector2 localPosition,
            Vector2 localScale,
            int sortingOrder
        )
        {
            var child = new GameObject(objectName);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            child.transform.localScale = localScale;

            var spriteRenderer = child.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = sortingOrder;
            return child;
        }

        private void CreateWorldLabel(
            string label,
            Transform parent,
            Vector2 localPosition,
            Color color
        )
        {
            _worldFont ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var labelObject = new GameObject("Label");
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = localPosition;

            var textMesh = labelObject.AddComponent<TextMesh>();
            textMesh.font = _worldFont;
            textMesh.text = label;
            textMesh.characterSize = 0.18f;
            textMesh.fontSize = 52;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = color;
            textMesh.GetComponent<MeshRenderer>().sortingOrder = 6;
        }

        private bool HasRequiredReferences()
        {
            if (
                _playerPrefab != null
                && _collectiblePrefab != null
                && _pushableBoxPrefab != null
                && _appleData != null
                && _burgerData != null
            )
            {
                return true;
            }

            Debug.LogError(
                "Vertical Slice Bootstrap requires all prefabs and food data references.",
                this
            );
            return false;
        }
    }
}
