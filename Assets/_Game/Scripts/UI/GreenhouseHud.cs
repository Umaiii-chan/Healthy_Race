using HealthyRace.Visuals;
using HealthyRace.WellBeing;
using UnityEngine;
using UnityEngine.UI;

namespace HealthyRace.UI
{
    /// <summary>
    /// O HUD das fases como objeto da estufa (HUD 5): uma placa de madeira pendurada por
    /// jogador, com o retrato e o Bem-Estar em cinco folhas; no meio, o relógio de madeira
    /// e a trepadeira da corrida, com os dois retratos andando até a porta.
    ///
    /// É só uma vista: lê <see cref="PlayerHUD"/> e <see cref="MatchHUD"/> a cada quadro e
    /// não guarda regra nenhuma. Os dois continuam recebendo os eventos do jogo; o painel de
    /// texto antigo deles fica escondido enquanto esta vista existe.
    /// </summary>
    public sealed class GreenhouseHud : MonoBehaviour
    {
        public const int LeafCount = 5;

        /// <summary>Altura que o HUD ocupa no topo da tela, em pixels de referência (1080p).</summary>
        public const float BandPixels = 168f;

        private const float SignWidth = 482f;
        private const float SignHeight = 124f;
        private const float TrackHalfWidth = 378f;

        private static readonly Color Ink = new Color32(0x1E, 0x2A, 0x22, 255);
        private static readonly Color Rope = new Color32(0x6B, 0x4B, 0x2B, 255);
        private static readonly Color Alert = new Color32(0xC7, 0x33, 0x2E, 255);

        private sealed class PlayerView
        {
            public PlayerHUD Hud;
            public int Player;
            public Image Portrait;
            public Image MarkerPortrait;
            public Image[] Leaves;
            public GameObject Tag;
            public Text TagText;
            public RectTransform Marker;
            public int Leaf = -1;
            public int Appearance = -1;
        }

        private HudKit _kit;
        private MatchHUD _match;
        private PlayerView[] _players;
        private Text _clock;
        private Text _level;
        private Text _status;
        private GameObject _levelTag;
        private GameObject _statusTag;

        public string ClockText => _clock != null ? _clock.text : string.Empty;

        public string LevelText => _level != null ? _level.text : string.Empty;

        /// <summary>Texto de estado ao lado do relógio; vazio quando a etiqueta está recolhida.</summary>
        public string StatusText =>
            _statusTag != null && _statusTag.activeSelf ? _status.text : string.Empty;

        /// <summary>Quantas folhas estão acesas na placa do jogador (1 ou 2).</summary>
        public int LeavesLit(int player) => View(player)?.Leaf ?? 0;

        /// <summary>Retrato mostrado na placa do jogador.</summary>
        public Sprite PortraitShown(int player) => View(player)?.Portrait.sprite;

        /// <summary>Texto da etiqueta de aviso da placa; vazio quando não há aviso.</summary>
        public string AlertText(int player)
        {
            var view = View(player);
            return view != null && view.Tag.activeSelf ? view.TagText.text : string.Empty;
        }

        /// <summary>Onde o retrato do jogador está na trepadeira: 0 na largada, 1 na porta.</summary>
        public float MarkerProgress(int player)
        {
            var view = View(player);
            return view == null
                ? 0f
                : Mathf.InverseLerp(
                    -TrackHalfWidth,
                    TrackHalfWidth,
                    view.Marker.anchoredPosition.x
                );
        }

        /// <summary>
        /// Folhas acesas para um valor de Bem-Estar: cada folha vale 20 pontos, e qualquer
        /// ponto acima de zero ainda mantém uma folha.
        /// </summary>
        public static int LeavesFor(int wellbeing)
        {
            if (wellbeing <= 0)
            {
                return 0;
            }

            var step = WellbeingComponent.MaximumValue / (float)LeafCount;
            return Mathf.Clamp(Mathf.CeilToInt(wellbeing / step), 1, LeafCount);
        }

        /// <summary>
        /// Monta o HUD dentro do canvas das fases. Devolve nulo (e não toca em nada) se as
        /// peças não estiverem em Resources: aí o painel de texto antigo continua à vista.
        /// </summary>
        public static GreenhouseHud Create(
            Transform canvas,
            MatchHUD match,
            PlayerHUD playerOne,
            PlayerHUD playerTwo,
            Color colorOne,
            Color colorTwo
        )
        {
            var kit = HudKit.Load();
            if (kit == null || !kit.IsComplete || canvas == null)
            {
                return null;
            }

            var root = new GameObject("GreenhouseHud", typeof(RectTransform));
            root.transform.SetParent(canvas, false);
            var rect = (RectTransform)root.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var hud = root.AddComponent<GreenhouseHud>();
            hud._kit = kit;
            hud._match = match;
            hud.BuildCenter(rect);
            hud._players = new[]
            {
                hud.BuildSign(rect, playerOne, 1, colorOne),
                hud.BuildSign(rect, playerTwo, 2, colorTwo),
            };

            if (match != null)
            {
                match.SetLegacyPanelVisible(false);
            }

            foreach (var view in hud._players)
            {
                if (view.Hud != null)
                {
                    view.Hud.SetLegacyPanelVisible(false);
                }
            }

            hud.Refresh();
            return hud;
        }

        private void LateUpdate()
        {
            Refresh();
        }

        /// <summary>Atualiza a vista a partir do estado atual do jogo.</summary>
        public void Refresh()
        {
            if (_players == null)
            {
                return;
            }

            foreach (var view in _players)
            {
                RefreshPlayer(view);
            }

            if (_match == null)
            {
                return;
            }

            _clock.text = _match.FormattedTime;
            var level = _match.LevelName;
            _level.text = level;
            _levelTag.SetActive(!string.IsNullOrEmpty(level));
            var status = _match.StatusText;
            _status.text = status;
            _status.color = _match.IsFinalWindowActive ? Alert : Ink;
            _statusTag.SetActive(!string.IsNullOrEmpty(status));
        }

        private void RefreshPlayer(PlayerView view)
        {
            if (view.Hud == null)
            {
                return;
            }

            var wellbeing =
                view.Hud.Wellbeing != null
                    ? view.Hud.Wellbeing.CurrentValue
                    : WellbeingComponent.MaximumValue;
            var leaves = LeavesFor(wellbeing);
            if (leaves != view.Leaf)
            {
                view.Leaf = leaves;
                for (var i = 0; i < view.Leaves.Length; i++)
                {
                    view.Leaves[i].sprite = i < leaves ? _kit.LeafOn : _kit.LeafOff;
                }
            }

            var appearance = (int)VisualCatalog.AppearanceFor(wellbeing);
            if (appearance != view.Appearance)
            {
                view.Appearance = appearance;
                var portrait = _kit.Portrait(view.Player, appearance);
                view.Portrait.sprite = portrait;
                view.MarkerPortrait.sprite = portrait;
                view.Portrait.enabled = portrait != null;
                view.MarkerPortrait.enabled = portrait != null;
            }

            var alert = AlertFor(view.Hud.Fatigue);
            view.TagText.text = alert;
            view.Tag.SetActive(!string.IsNullOrEmpty(alert));

            var marker = view.Marker.anchoredPosition;
            marker.x = Mathf.Lerp(-TrackHalfWidth, TrackHalfWidth, view.Hud.Progress);
            view.Marker.anchoredPosition = marker;
        }

        /// <summary>O único texto da placa: aparece só enquanto a fadiga avisa ou acontece.</summary>
        public static string AlertFor(FatigueComponent fatigue)
        {
            if (fatigue == null)
            {
                return string.Empty;
            }

            if (fatigue.State == FatigueState.Fatigued)
            {
                return "EM FADIGA!";
            }

            return fatigue.IsWarning ? "Cansando…" : string.Empty;
        }

        private PlayerView View(int player)
        {
            if (_players == null)
            {
                return null;
            }

            foreach (var view in _players)
            {
                if (view.Player == player)
                {
                    return view;
                }
            }

            return null;
        }

        // ------------------------------------------------------------------ montagem

        private PlayerView BuildSign(RectTransform parent, PlayerHUD hud, int player, Color color)
        {
            var left = player == 1;
            var sign = Node(parent, "Placa P" + player);
            var corner = new Vector2(left ? 0f : 1f, 1f);
            sign.anchorMin = corner;
            sign.anchorMax = corner;
            sign.pivot = corner;
            sign.anchoredPosition = new Vector2(left ? 24f : -24f, -22f);
            sign.sizeDelta = new Vector2(SignWidth, SignHeight);

            // posições dentro da placa: x a partir da borda do lado do retrato, y de cima
            Vector2 At(float x, float y) => new Vector2(left ? x : SignWidth - x, -y);

            Block(sign, "Corda A", Rope, At(90f, -11f), new Vector2(6f, 26f));
            Block(sign, "Corda B", Rope, At(392f, -11f), new Vector2(6f, 26f));
            Picture(sign, "Tabua", _kit.Sign, At(241f, 62f), new Vector2(SignWidth, SignHeight));

            var view = new PlayerView
            {
                Hud = hud,
                Player = player,
                Leaves = new Image[LeafCount],
            };
            view.Portrait = Badge(sign, "Retrato", At(64f, 59f), 92f, 132f, 108f, color);
            for (var i = 0; i < LeafCount; i++)
            {
                // a primeira folha é a mais próxima do retrato; a última é a primeira a cair
                view.Leaves[i] = Picture(
                    sign,
                    "Folha " + (i + 1),
                    _kit.LeafOn,
                    At(152f + i * 64f, 59f),
                    new Vector2(58f, 58f)
                );
            }

            view.Tag = Label(
                sign,
                "Aviso",
                At(290f, 120f),
                new Vector2(210f, 36f),
                18,
                out view.TagText
            );
            view.TagText.color = Alert;
            view.Tag.SetActive(false);

            // o retrato na trepadeira: jogador 1 por cima do caule, jogador 2 por baixo
            view.Marker = Node(parent, "Corredor P" + player);
            view.Marker.anchorMin = new Vector2(0.5f, 1f);
            view.Marker.anchorMax = new Vector2(0.5f, 1f);
            view.Marker.pivot = new Vector2(0.5f, 0.5f);
            view.Marker.sizeDelta = new Vector2(46f, 46f);
            view.Marker.anchoredPosition = new Vector2(-TrackHalfWidth, left ? -114f : -148f);
            view.MarkerPortrait = Badge(view.Marker, "Retrato", Vector2.zero, 38f, 56f, 46f, color);
            var badge = (RectTransform)view.MarkerPortrait.transform.parent.parent;
            badge.anchorMin = new Vector2(0.5f, 0.5f);
            badge.anchorMax = new Vector2(0.5f, 0.5f);
            return view;
        }

        private void BuildCenter(RectTransform parent)
        {
            var center = Node(parent, "Centro");
            center.anchorMin = new Vector2(0.5f, 1f);
            center.anchorMax = new Vector2(0.5f, 1f);
            center.pivot = new Vector2(0.5f, 1f);
            center.anchoredPosition = Vector2.zero;
            center.sizeDelta = new Vector2(860f, BandPixels);

            Vector2 At(float x, float y) => new Vector2(430f + x, -y);

            Picture(center, "Trepadeira", _kit.Vine, At(0f, 132f), new Vector2(800f, 65f));
            Picture(
                center,
                "Porta",
                _kit.Goal,
                At(TrackHalfWidth + 34f, 130f),
                new Vector2(40f, 52f)
            );
            Block(center, "Corda", Rope, At(0f, 3f), new Vector2(6f, 10f));
            Picture(center, "Relogio", _kit.Clock, At(0f, 60f), new Vector2(112f, 112f));
            _clock = Words(center, "Tempo", At(0f, 60f), new Vector2(84f, 40f), 27);
            _levelTag = Label(
                center,
                "Fase",
                At(-232f, 44f),
                new Vector2(300f, 38f),
                17,
                out _level
            );
            _statusTag = Label(
                center,
                "Estado",
                At(232f, 44f),
                new Vector2(300f, 38f),
                17,
                out _status
            );
            _statusTag.SetActive(false);
        }

        /// <summary>
        /// Retrato redondo: disco creme que recorta a cabeça do personagem, com aro de tinta
        /// e aro na cor do jogador por cima. Devolve a imagem do retrato.
        /// </summary>
        private Image Badge(
            RectTransform parent,
            string name,
            Vector2 position,
            float disk,
            float portrait,
            float ring,
            Color color
        )
        {
            var root = Node(parent, name);
            Place(root, position, new Vector2(ring, ring));
            var mask = Picture(root, "Disco", _kit.Disk, Vector2.zero, new Vector2(disk, disk));
            Center((RectTransform)mask.transform);
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var face = Picture(
                (RectTransform)mask.transform,
                "Cabeca",
                null,
                Vector2.zero,
                new Vector2(portrait, portrait)
            );
            face.enabled = false;
            // a cabeça fica um pouco à direita e abaixo do centro da célula do retrato
            Center((RectTransform)face.transform);
            ((RectTransform)face.transform).anchoredPosition = new Vector2(
                -portrait * 0.1f,
                portrait * 0.045f
            );
            Center(
                (RectTransform)
                    Picture(
                        root,
                        "AroTinta",
                        _kit.RingInk,
                        Vector2.zero,
                        new Vector2(ring, ring)
                    ).transform
            );
            var fill = Picture(
                root,
                "AroCor",
                _kit.RingFill,
                Vector2.zero,
                new Vector2(ring, ring)
            );
            Center((RectTransform)fill.transform);
            fill.color = color;
            return face;
        }

        private GameObject Label(
            RectTransform parent,
            string name,
            Vector2 position,
            Vector2 size,
            int fontSize,
            out Text text
        )
        {
            var tag = Picture(parent, name, _kit.Tag, position, size);
            tag.type = Image.Type.Sliced;
            text = Words((RectTransform)tag.transform, "Texto", Vector2.zero, size, fontSize);
            var rect = (RectTransform)text.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(10f, 2f);
            rect.offsetMax = new Vector2(-10f, 2f);
            return tag.gameObject;
        }

        private static Text Words(
            RectTransform parent,
            string name,
            Vector2 position,
            Vector2 size,
            int fontSize
        )
        {
            var node = Node(parent, name);
            Place(node, position, size);
            var text = node.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.color = Ink;
            text.raycastTarget = false;
            return text;
        }

        private static Image Picture(
            RectTransform parent,
            string name,
            Sprite sprite,
            Vector2 position,
            Vector2 size
        )
        {
            var node = Node(parent, name);
            Place(node, position, size);
            var image = node.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        private static void Block(
            RectTransform parent,
            string name,
            Color color,
            Vector2 position,
            Vector2 size
        )
        {
            Picture(parent, name, null, position, size).color = color;
        }

        private static RectTransform Node(Transform parent, string name)
        {
            var node = new GameObject(name, typeof(RectTransform));
            node.transform.SetParent(parent, false);
            return (RectTransform)node.transform;
        }

        /// <summary>Posiciona pelo centro, a partir do canto de cima, à esquerda, do pai.</summary>
        private static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Center(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
        }
    }
}
