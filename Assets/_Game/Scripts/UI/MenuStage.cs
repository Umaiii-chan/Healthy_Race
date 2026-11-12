using System;
using System.Collections.Generic;
using HealthyRace.Core;
using HealthyRace.Player;
using HealthyRace.Visuals;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace HealthyRace.UI
{
    /// <summary>
    /// Menu jogável (Menu 2): um trecho da estufa em que as opções são placas penduradas.
    /// O jogador anda até a placa e pula para escolher; em "Prontos?", cada um pula no
    /// próprio vaso para confirmar. Ensina a andar e a pular antes da corrida.
    ///
    /// As placas são botões de verdade: clique do mouse e setas + Enter continuam
    /// funcionando para quem não quiser, ou não puder, usar o pulo.
    /// </summary>
    public sealed class MenuStage : MonoBehaviour
    {
        public enum Mode
        {
            Choosing,
            Ready,
            Launching,
        }

        /// <summary>Altura da base das placas acima do piso: só o pulo alcança.</summary>
        public const float SignHeight = 2.75f;

        /// <summary>Distância de cada vaso ao centro do palco.</summary>
        public const float PadOffset = 5.5f;

        /// <summary>Tempo com os dois nos vasos até a corrida começar.</summary>
        public const float ReadyHoldSeconds = 0.7f;

        private const float PixelsPerUnit = 100f;
        private const float BumpCooldown = 0.6f;
        private const float PadReach = 1.1f;

        private static readonly Color Ink = new Color32(0x1E, 0x2A, 0x22, 255);
        private static readonly Color Cream = new Color32(0xF8, 0xF3, 0xDC, 255);
        private static readonly Color Rope = new Color32(0x6B, 0x4B, 0x2B, 255);
        private static readonly Color Idle = new Color32(0xB9, 0xC4, 0xBC, 255);
        private static readonly Color Chosen = new Color(1f, 0.86f, 0.3f);

        private readonly List<Button> _signs = new();
        private readonly bool[] _ready = new bool[2];
        private readonly Image[] _pads = new Image[2];
        private readonly Text[] _padTexts = new Text[2];
        private readonly GameObject[] _players = new GameObject[2];
        private readonly Color[] _colors = new Color[2];

        private GameScreens _screens;
        private HudKit _kit;
        private RectTransform _canvas;
        private GameObject _choices;
        private GameObject _readyRoot;
        private Text _levelLabel;
        private Vector2 _floor;
        private float _lastBump = -10f;
        private float _readySince;
        private float _bothReadyFor;

        public Mode State { get; private set; }

        /// <summary>Fase que "Prontos?" vai abrir.</summary>
        public int SelectedLevel { get; private set; } = 1;

        /// <summary>As placas de escolha, na ordem em que aparecem.</summary>
        public IReadOnlyList<Button> Signs => _signs;

        public bool IsReady(int player) => player >= 1 && player <= 2 && _ready[player - 1];

        /// <summary>Centro do vaso do jogador, sobre o piso.</summary>
        public Vector2 PadPosition(int player) =>
            new(_floor.x + (player == 1 ? -PadOffset : PadOffset), _floor.y);

        /// <summary>Ponto embaixo da placa: quem pula dali, bate nela.</summary>
        public Vector2 SignPosition(int index) => new(_signs[index].transform.position.x, _floor.y);

        /// <summary>
        /// Monta o palco. <paramref name="floor"/> é o centro do piso (x) e o topo dele (y).
        /// Devolve nulo sem as peças do kit ou sem telas.
        /// </summary>
        public static MenuStage Create(
            Transform parent,
            GameScreens screens,
            GameObject playerOne,
            GameObject playerTwo,
            Vector2 floor,
            Color colorOne,
            Color colorTwo
        )
        {
            var kit = HudKit.Load();
            if (kit == null || !kit.IsComplete || screens == null)
            {
                return null;
            }

            var host = new GameObject("MenuStage");
            host.transform.SetParent(parent, false);
            var stage = host.AddComponent<MenuStage>();
            stage._screens = screens;
            stage._kit = kit;
            stage._floor = floor;
            stage._players[0] = playerOne;
            stage._players[1] = playerTwo;
            stage._colors[0] = colorOne;
            stage._colors[1] = colorTwo;
            stage.SelectedLevel = Mathf.Clamp(
                SaveGame.HighestUnlocked(GameFlow.LevelCount),
                1,
                GameFlow.LevelCount
            );
            stage.Build();
            screens.PlayOverride = stage.EnterReady;
            stage.EnterChoosing();
            return stage;
        }

        private void OnDestroy()
        {
            if (_screens != null && _screens.PlayOverride == EnterReady)
            {
                _screens.PlayOverride = null;
            }
        }

        // ------------------------------------------------------------------ estados

        /// <summary>Volta às placas de escolha.</summary>
        public void EnterChoosing()
        {
            State = Mode.Choosing;
            _choices.SetActive(true);
            _readyRoot.SetActive(false);
            _screens.ShowMainMenu();
            Select(_signs[0]);
        }

        /// <summary>"Jogar": troca as placas pelos vasos e pela escolha de fase.</summary>
        public void EnterReady()
        {
            State = Mode.Ready;
            _readySince = Time.unscaledTime;
            _bothReadyFor = 0f;
            _ready[0] = false;
            _ready[1] = false;
            _choices.SetActive(false);
            _readyRoot.SetActive(true);
            _screens.HideScreens();
            Select(null);
            RefreshLevel();
            RefreshPads();
        }

        /// <summary>Muda a fase escolhida, dentro das que já estão liberadas.</summary>
        public void StepLevel(int delta)
        {
            var unlocked = Mathf.Clamp(
                SaveGame.HighestUnlocked(GameFlow.LevelCount),
                1,
                GameFlow.LevelCount
            );
            SelectedLevel = Mathf.Clamp(SelectedLevel + delta, 1, unlocked);
            RefreshLevel();
        }

        /// <summary>Começa a corrida na fase escolhida.</summary>
        public void Launch()
        {
            if (State != Mode.Ready)
            {
                return;
            }

            State = Mode.Launching;
            GameFlow.LoadLevel(SelectedLevel);
        }

        private void Update()
        {
            if (State == Mode.Choosing)
            {
                // sem nada selecionado o teclado não navega: devolve a seleção à primeira placa
                var events = EventSystem.current;
                if (
                    events != null
                    && events.currentSelectedGameObject == null
                    && _screens.View.Screen == VisualScreen.MainMenu
                )
                {
                    Select(_signs[0]);
                }

                return;
            }

            if (State != Mode.Ready)
            {
                return;
            }

            for (var i = 0; i < 2; i++)
            {
                _ready[i] = IsOnPad(_players[i], PadPosition(i + 1));
            }

            RefreshPads();
            _bothReadyFor = _ready[0] && _ready[1] ? _bothReadyFor + Time.deltaTime : 0f;
            if (_bothReadyFor >= ReadyHoldSeconds)
            {
                Launch();
                return;
            }

            // atalhos de teclado, para quem não usa o pulo; a folga evita que o Enter que
            // escolheu "Jogar" já comece a corrida
            var keyboard = Keyboard.current;
            if (keyboard == null || Time.unscaledTime - _readySince < 0.35f)
            {
                return;
            }

            if (
                keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame
            )
            {
                Launch();
            }
            else if (keyboard.escapeKey.wasPressedThisFrame)
            {
                EnterChoosing();
            }
        }

        private bool IsOnPad(GameObject player, Vector2 pad)
        {
            if (player == null)
            {
                return false;
            }

            var movement = player.GetComponent<PlayerMovement>();
            Vector2 position = player.transform.position;
            return movement != null
                && movement.IsGrounded
                && Mathf.Abs(position.x - pad.x) <= PadReach
                && position.y < pad.y + 1.5f;
        }

        private void RefreshPads()
        {
            for (var i = 0; i < 2; i++)
            {
                _pads[i].color = _ready[i] ? _colors[i] : Color.Lerp(Idle, _colors[i], 0.35f);
                _padTexts[i].text = _ready[i] ? "PRONTO!" : "pule no vaso";
            }
        }

        private void RefreshLevel()
        {
            var best = SaveGame.BestTime(SelectedLevel);
            _levelLabel.text = best.HasValue
                ? $"Fase {SelectedLevel}\nmelhor {VisualUiFormat.Clock(best.Value)}"
                : $"Fase {SelectedLevel}";
        }

        private static void Select(Button button)
        {
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(
                    button != null ? button.gameObject : null
                );
            }
        }

        /// <summary>Um jogador pulou e bateu na placa: vale como clique.</summary>
        private void Bump(Button button)
        {
            if (
                Time.unscaledTime - _lastBump < BumpCooldown
                || State == Mode.Launching
                || !button.isActiveAndEnabled
            )
            {
                return;
            }

            // com "Como jogar" ou "Áudio" abertos por cima, as placas não respondem
            if (State == Mode.Choosing && _screens.View.Screen != VisualScreen.MainMenu)
            {
                return;
            }

            _lastBump = Time.unscaledTime;
            Select(button);
            button.onClick.Invoke();
        }

        // ------------------------------------------------------------------ montagem

        private void Build()
        {
            var canvasObject = new GameObject(
                "Placas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)
            );
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            canvas.sortingOrder = 4;
            canvasObject.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 3f;
            _canvas = (RectTransform)canvasObject.transform;
            _canvas.pivot = new Vector2(0.5f, 0f);
            _canvas.sizeDelta = new Vector2(3200f, 1800f);
            _canvas.localScale = Vector3.one / PixelsPerUnit;
            _canvas.position = new Vector3(_floor.x, _floor.y, 0f);

            _choices = Group("Escolha");
            var labels = new[] { "Jogar", "Como jogar", "Áudio", "Sair" };
            var commands = new[]
            {
                VisualCommand.Play,
                VisualCommand.Controls,
                VisualCommand.Audio,
                VisualCommand.Quit,
            };
            for (var i = 0; i < labels.Length; i++)
            {
                var command = commands[i];
                var sign = Sign(
                    _choices.transform,
                    labels[i],
                    (i - 1.5f) * 4.6f,
                    new Vector2(3.3f, 1.15f),
                    44,
                    () => _screens.View.Request(command)
                );
                _signs.Add(sign);
            }

            _readyRoot = Group("Prontos");
            var title = Words(_readyRoot.transform, "Titulo", new Vector2(0f, 12.6f), 1400f, 150);
            title.text = "Prontos?";
            title.color = Cream;
            var outline = title.gameObject.AddComponent<Outline>();
            outline.effectColor = Ink;
            outline.effectDistance = new Vector2(5f, -5f);

            var picker = Sign(_readyRoot.transform, "Fase", 0f, new Vector2(3.4f, 1.3f), 38, null);
            picker.interactable = false;
            _levelLabel = picker.GetComponentInChildren<Text>();
            Sign(_readyRoot.transform, "←", -2.9f, new Vector2(1.2f, 1.15f), 56, () =>
                StepLevel(-1)
            );
            Sign(_readyRoot.transform, "→", 2.9f, new Vector2(1.2f, 1.15f), 56, () => StepLevel(1));
            Sign(
                _readyRoot.transform,
                "Voltar",
                -12.5f,
                new Vector2(2.4f, 1.0f),
                34,
                EnterChoosing
            );

            var keys = new[] { "A   D   W", "←   →   ↑" };
            for (var i = 0; i < 2; i++)
            {
                var x = i == 0 ? -PadOffset : PadOffset;
                _pads[i] = Picture(
                    _readyRoot.transform,
                    "Vaso P" + (i + 1),
                    _kit.Pad,
                    new Vector2(x, 0.16f),
                    new Vector2(2.6f, 0.6f)
                );
                var caps = Words(_readyRoot.transform, "Teclas", new Vector2(x, 4.7f), 500f, 44);
                caps.text = keys[i];
                caps.color = Ink;
                _padTexts[i] = Words(
                    _readyRoot.transform,
                    "Estado",
                    new Vector2(x, 3.9f),
                    500f,
                    46
                );
                _padTexts[i].color = _colors[i];
                var stroke = _padTexts[i].gameObject.AddComponent<Outline>();
                stroke.effectColor = Ink;
                stroke.effectDistance = new Vector2(2.5f, -2.5f);
            }

            var hint = Words(_readyRoot.transform, "Dica", new Vector2(0f, -1.1f), 1600f, 36);
            hint.text = "Cada um pula no seu vaso  ·  Enter começa já  ·  Esc volta";
            hint.color = Cream;
        }

        private GameObject Group(string name)
        {
            var node = new GameObject(name, typeof(RectTransform));
            node.transform.SetParent(_canvas, false);
            var rect = (RectTransform)node.transform;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            return node;
        }

        /// <summary>
        /// Placa pendurada que é botão (mouse e teclado) e gatilho (pulo). O x é contado do
        /// centro do palco; a base fica a <see cref="SignHeight"/> do piso.
        /// </summary>
        private Button Sign(
            Transform parent,
            string label,
            float x,
            Vector2 size,
            int fontSize,
            Action onClick
        )
        {
            var board = Picture(
                parent,
                "Placa " + label,
                _kit.Sign,
                new Vector2(x, SignHeight + size.y * 0.5f),
                size
            );
            board.raycastTarget = true;
            var rect = (RectTransform)board.transform;
            foreach (var side in new[] { -0.32f, 0.32f })
            {
                var rope = Picture(
                    rect,
                    "Corda",
                    null,
                    new Vector2(side * size.x, size.y * 0.5f + 0.45f),
                    new Vector2(0.06f, 0.9f)
                );
                rope.color = Rope;
                rope.transform.SetAsFirstSibling();
            }

            var text = Words(rect, "Texto", Vector2.zero, size.x * PixelsPerUnit, fontSize);
            text.text = label;
            text.color = Ink;

            var button = board.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Chosen;
            colors.selectedColor = Chosen;
            colors.pressedColor = new Color(0.9f, 0.72f, 0.2f);
            colors.disabledColor = Color.white;
            button.colors = colors;
            if (onClick == null)
            {
                return button;
            }

            button.onClick.AddListener(() => onClick());

            // o gatilho fica na base da placa: só a cabeça de quem pula chega lá
            var trigger = new GameObject("Gatilho", typeof(RectTransform));
            trigger.transform.SetParent(rect, false);
            ((RectTransform)trigger.transform).anchoredPosition = new Vector2(
                0f,
                -size.y * 0.5f * PixelsPerUnit
            );
            var box = trigger.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(size.x * 0.82f, 0.7f) * PixelsPerUnit;
            trigger.AddComponent<MenuBump>().Bumped = () => Bump(button);
            return button;
        }

        /// <summary>Imagem posicionada em unidades do jogo, a partir do centro do piso.</summary>
        private static Image Picture(
            Transform parent,
            string name,
            Sprite sprite,
            Vector2 position,
            Vector2 size
        )
        {
            var node = new GameObject(name, typeof(RectTransform));
            node.transform.SetParent(parent, false);
            var rect = (RectTransform)node.transform;
            rect.anchoredPosition = position * PixelsPerUnit;
            rect.sizeDelta = size * PixelsPerUnit;
            var image = node.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        private static Text Words(
            Transform parent,
            string name,
            Vector2 position,
            float width,
            int fontSize
        )
        {
            var node = new GameObject(name, typeof(RectTransform));
            node.transform.SetParent(parent, false);
            var rect = (RectTransform)node.transform;
            rect.anchoredPosition = position * PixelsPerUnit;
            rect.sizeDelta = new Vector2(width, fontSize * 2.6f);
            var text = node.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }
    }

    /// <summary>Gatilho da placa do menu: avisa quando um jogador sobe e encosta nela.</summary>
    public sealed class MenuBump : MonoBehaviour
    {
        public Action Bumped { get; set; }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var movement = other.GetComponentInParent<PlayerMovement>();
            if (movement == null)
            {
                return;
            }

            var body = movement.GetComponent<Rigidbody2D>();
            if (body != null && body.linearVelocity.y > 0.1f)
            {
                Bumped?.Invoke();
            }
        }
    }
}
