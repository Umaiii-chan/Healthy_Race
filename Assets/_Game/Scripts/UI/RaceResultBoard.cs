using System.Collections.Generic;
using HealthyRace.Visuals;
using UnityEngine;
using UnityEngine.UI;

namespace HealthyRace.UI
{
    /// <summary>
    /// Tela de resultado que explica a corrida (Resultado 3): o vencedor à esquerda, com as
    /// estrelas dos dois, e à direita o gráfico de quem estava na frente em cada andar, com
    /// a frase que aponta a virada.
    ///
    /// É montada por cima da tela de resultado do prefab. O que já existia ali (título,
    /// motivo e os dois painéis de números) vira a vista "Detalhes"; os botões ficam.
    /// </summary>
    public sealed class RaceResultBoard : MonoBehaviour
    {
        public const int MaxStars = 3;

        public struct Data
        {
            /// <summary>0 em empate; 1 ou 2.</summary>
            public int Winner;

            /// <summary>Frase do desfecho ("P1 chegou 3,2 s antes.").</summary>
            public string Outcome;

            /// <summary>Frase da liderança, ancorada no gráfico.</summary>
            public string Story;
            public int StarsOne;
            public int StarsTwo;
            public float[] Leads;
            public bool HasTurn;
            public float Turn;
            public bool TurnWithRush;
            public int Floors;
            public Color ColorOne;
            public Color ColorTwo;
            public Sprite WinnerPose;
        }

        private static readonly Color Ink = new Color32(0x1E, 0x2A, 0x22, 255);
        private static readonly Color Cream = new Color32(0xF8, 0xF3, 0xDC, 255);
        private static readonly Color Backdrop = new Color32(0x1F, 0x3A, 0x2C, 255);

        private static readonly Color32[] Confetti =
        {
            new(0x2F, 0x80, 0xED, 255),
            new(0xF2, 0x99, 0x4A, 255),
            new(0x5B, 0xC2, 0x4A, 255),
            new(0xFF, 0xD2, 0x3F, 255),
            new(0xE8, 0x42, 0x3A, 255),
        };

        private GameObject _summary;
        private GameObject _details;
        private Text _toggleText;
        private LeadChart _chart;
        private Text _title;
        private Text _outcome;
        private Text _story;
        private readonly int[] _stars = new int[2];

        public string Title => _title != null ? _title.text : string.Empty;

        public string Outcome => _outcome != null ? _outcome.text : string.Empty;

        public string Story => _story != null ? _story.text : string.Empty;

        public LeadChart Chart => _chart;

        public bool DetailsVisible => _details != null && _details.activeSelf;

        public int StarsShown(int player) => player >= 1 && player <= 2 ? _stars[player - 1] : 0;

        /// <summary>
        /// Monta o quadro na tela de resultado. Devolve nulo sem as peças do kit: aí a tela
        /// antiga continua como estava.
        /// </summary>
        public static RaceResultBoard Show(GameObject screen, Data data)
        {
            var kit = HudKit.Load();
            if (screen == null || kit == null || !kit.IsComplete)
            {
                return null;
            }

            var existing = screen.GetComponentInChildren<RaceResultBoard>(true);
            if (existing != null)
            {
                Destroy(existing.gameObject);
            }

            var background = screen.GetComponent<Image>();
            if (background != null)
            {
                background.sprite = null;
                background.color = Backdrop;
            }

            // tudo o que não é botão vira a vista de detalhes
            var details = Stretch(screen.transform, "Detalhes");
            var legacy = new List<Transform>();
            foreach (Transform child in screen.transform)
            {
                // um filho com botão dentro (a fileira de botões do prefab) continua à vista
                if (child != details && child.GetComponentInChildren<Button>(true) == null)
                {
                    legacy.Add(child);
                }
            }

            foreach (var child in legacy)
            {
                child.SetParent(details, false);
            }

            var root = Stretch(screen.transform, "Resumo");
            root.SetAsFirstSibling();
            details.SetSiblingIndex(1);
            var board = root.gameObject.AddComponent<RaceResultBoard>();
            board._summary = root.gameObject;
            board._details = details.gameObject;
            board.Build(root, kit, data);
            // o alternador é irmão das duas vistas, por cima delas: vale nas duas
            board._toggleText.transform.parent.SetParent(screen.transform, false);
            board.ShowDetails(false);
            return board;
        }

        /// <summary>Troca entre o resumo (gráfico) e os números detalhados.</summary>
        public void ShowDetails(bool visible)
        {
            _details.SetActive(visible);
            _summary.SetActive(!visible);
            _toggleText.text = visible ? "Voltar ao resumo" : "Detalhes";
        }

        private void Build(RectTransform root, HudKit kit, Data data)
        {
            var random = new System.Random(7);
            // confete só quando alguém venceu
            for (var i = 0; i < (data.Winner != 0 ? 26 : 0); i++)
            {
                var piece = Picture(
                    root,
                    "Confete",
                    null,
                    new Vector2(40 + random.Next(600), 20 + random.Next(300)),
                    new Vector2(22, 10)
                );
                piece.color = Confetti[i % Confetti.Length];
                piece.rectTransform.localRotation = Quaternion.Euler(0, 0, random.Next(180));
            }

            var winnerColor =
                data.Winner == 1 ? data.ColorOne
                : data.Winner == 2 ? data.ColorTwo
                : Cream;
            if (data.WinnerPose != null && data.Winner != 0)
            {
                var pose = Picture(
                    root,
                    "Vencedor",
                    data.WinnerPose,
                    new Vector2(330, 300),
                    new Vector2(430, 430)
                );
                pose.preserveAspect = true;
            }

            _title = Words(root, "Titulo", new Vector2(330, 555), new Vector2(620, 110), 92);
            _title.text = data.Winner == 0 ? "Empate" : $"P{data.Winner} venceu";
            _title.color = winnerColor;
            var stroke = _title.gameObject.AddComponent<Outline>();
            stroke.effectColor = Cream;
            stroke.effectDistance = new Vector2(3.5f, -3.5f);

            _outcome = Words(root, "Desfecho", new Vector2(330, 640), new Vector2(620, 60), 34);
            _outcome.text = data.Outcome ?? string.Empty;
            _outcome.color = Cream;

            _stars[0] = Mathf.Clamp(data.StarsOne, 0, MaxStars);
            _stars[1] = Mathf.Clamp(data.StarsTwo, 0, MaxStars);
            for (var player = 1; player <= 2; player++)
            {
                var y = 740 + (player - 1) * 104;
                Badge(
                    root,
                    kit,
                    new Vector2(190, y),
                    kit.Portrait(player, 0),
                    player == 1 ? data.ColorOne : data.ColorTwo
                );
                for (var star = 0; star < MaxStars; star++)
                {
                    Picture(
                        root,
                        "Estrela",
                        star < _stars[player - 1] ? kit.StarOn : kit.StarOff,
                        new Vector2(300 + star * 78, y),
                        new Vector2(70, 70)
                    );
                }
            }

            // o cartão do gráfico
            var card = Picture(
                root,
                "Cartao",
                kit.Tag,
                new Vector2(1280, 400),
                new Vector2(1160, 680)
            );
            card.type = Image.Type.Sliced;
            var cardRect = card.rectTransform;
            Vector2 InCard(float x, float y) => new(x, y);

            var heading = Words(cardRect, "Cabecalho", InCard(580, 52), new Vector2(1080, 50), 34);
            heading.text = "Quem estava na frente, andar por andar";
            heading.alignment = TextAnchor.MiddleLeft;
            heading.color = Ink;

            var ahead = Words(cardRect, "P1 a frente", InCard(160, 112), new Vector2(240, 40), 26);
            ahead.text = "P1 à frente";
            ahead.alignment = TextAnchor.MiddleLeft;
            ahead.color = data.ColorOne;
            var behind = Words(cardRect, "P2 a frente", InCard(160, 508), new Vector2(240, 40), 26);
            behind.text = "P2 à frente";
            behind.alignment = TextAnchor.MiddleLeft;
            behind.color = data.ColorTwo;

            var chartObject = new GameObject("Grafico", typeof(RectTransform));
            chartObject.transform.SetParent(cardRect, false);
            Place((RectTransform)chartObject.transform, InCard(580, 310), new Vector2(1040, 360));
            _chart = chartObject.AddComponent<LeadChart>();
            _chart.raycastTarget = false;
            _chart.SetLeads(data.Leads);

            var floors = Mathf.Clamp(data.Floors, 2, 10);
            for (var floor = 0; floor < floors; floor++)
            {
                var label = Words(
                    cardRect,
                    "Andar",
                    InCard(60 + 1040f * (floor + 0.5f) / floors, 632),
                    new Vector2(80, 40),
                    28
                );
                label.text = (floor + 1) + "º";
                label.color = Ink;
            }

            if (data.HasTurn)
            {
                // o ponto do gráfico em que a frente mudou de mão
                var point = _chart.PointAt(data.Turn);
                var scenery = SceneryKit.Load();
                var mark = Picture(
                    cardRect,
                    "Virada",
                    data.TurnWithRush && scenery != null ? scenery.FastBadge : kit.Disk,
                    InCard(580 + point.x, 310 - point.y),
                    new Vector2(68, 68)
                );
                if (!data.TurnWithRush || scenery == null)
                {
                    mark.color = new Color32(0xFF, 0xD2, 0x3F, 255);
                }
            }

            var note = Picture(cardRect, "Frase", kit.Tag, InCard(580, 566), new Vector2(900, 62));
            note.type = Image.Type.Sliced;
            note.color = new Color32(0xFF, 0xF6, 0xD0, 255);
            _story = Words(
                note.rectTransform,
                "Texto",
                new Vector2(450, 31),
                new Vector2(860, 50),
                28
            );
            _story.text = data.Story ?? string.Empty;
            _story.color = Ink;

            // alternar para os números; depois de montado, sai do resumo e fica por cima
            var toggle = Picture(
                root,
                "Alternar",
                kit.Tag,
                new Vector2(1734, 800),
                new Vector2(232, 60)
            );
            toggle.type = Image.Type.Sliced;
            toggle.raycastTarget = true;
            _toggleText = Words(
                toggle.rectTransform,
                "Texto",
                new Vector2(116, 30),
                new Vector2(220, 50),
                24
            );
            _toggleText.color = Ink;
            var button = toggle.gameObject.AddComponent<Button>();
            button.onClick.AddListener(() => ShowDetails(!DetailsVisible));
        }

        private static void Badge(
            RectTransform parent,
            HudKit kit,
            Vector2 position,
            Sprite portrait,
            Color color
        )
        {
            var disk = Picture(parent, "Retrato", kit.Disk, position, new Vector2(74, 74));
            disk.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            if (portrait != null)
            {
                var face = Picture(
                    disk.rectTransform,
                    "Cabeca",
                    portrait,
                    new Vector2(37 - 11, 37 - 5),
                    new Vector2(110, 110)
                );
                face.raycastTarget = false;
            }

            Picture(parent, "AroTinta", kit.RingInk, position, new Vector2(90, 90));
            Picture(parent, "AroCor", kit.RingFill, position, new Vector2(90, 90)).color = color;
        }

        private static RectTransform Stretch(Transform parent, string name)
        {
            var node = new GameObject(name, typeof(RectTransform));
            node.transform.SetParent(parent, false);
            var rect = (RectTransform)node.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        /// <summary>Imagem posicionada pelo centro, a partir do canto de cima, à esquerda.</summary>
        private static Image Picture(
            RectTransform parent,
            string name,
            Sprite sprite,
            Vector2 position,
            Vector2 size
        )
        {
            var node = new GameObject(name, typeof(RectTransform));
            node.transform.SetParent(parent, false);
            Place((RectTransform)node.transform, position, size);
            var image = node.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        private static Text Words(
            RectTransform parent,
            string name,
            Vector2 position,
            Vector2 size,
            int fontSize
        )
        {
            var node = new GameObject(name, typeof(RectTransform));
            node.transform.SetParent(parent, false);
            Place((RectTransform)node.transform, position, size);
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

        private static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(position.x, -position.y);
            rect.sizeDelta = size;
        }
    }
}
