using System;
using UnityEngine;

namespace HealthyRace.Visuals
{
    public enum WellbeingAppearance
    {
        Healthy,
        Stable,
        Tired,
        Critical,
        Salo,
    }

    public enum VisualAction
    {
        Idle,
        Move,
        Jump,
        Fall,
        Land,
        Push,
        Interact,
        Eat,
        Fatigue,
        Recover,
        Victory,
        Defeat,
    }

    public enum PuzzleAppearance
    {
        Inactive,
        Active,
        Blocked,
        Interactable,
    }

    public enum FeedbackKind
    {
        HealthyPickup,
        UltraProcessedPickup,
        WellbeingChange,
        BandChange,
        Fatigue,
        Recovery,
        PuzzleActivation,
        Checkpoint,
        Finish,
        Victory,
        Defeat,
        Dust,
    }

    /// <summary>Arte de cenário da arena: fundo em camadas, terreno, perigos e placa.</summary>
    [Serializable]
    public struct ArenaArt
    {
        public Sprite Sky;
        public Sprite[] Clouds;
        public Sprite HillsFar;
        public Sprite HillsNear;
        public Sprite Bushes;
        public Sprite Ground;
        public Sprite Plank;
        public Sprite Stone;
        public Sprite Spikes;
        public Sprite Mud;
        public Sprite Sign;

        public bool HasBackground =>
            Sky != null
            && Clouds != null
            && Clouds.Length > 0
            && HillsFar != null
            && HillsNear != null
            && Bushes != null;
    }

    /// <summary>Arte das fases em labirinto: estufa (caminho longo) e lanchonete (curto).</summary>
    [Serializable]
    public struct GreenhouseArt
    {
        public Sprite Wall;

        /// <summary>Folhagem de fundo que ladrilha; camada de parallax.</summary>
        public Sprite Leaves;
        public Sprite Brick;
        public Sprite BrickTop;
        public Sprite Ramp;
        public Sprite FastFloor;
        public Sprite FastSign;
        public Sprite Pot;
        public Sprite Vine;
        public Sprite Lift;
        public Sprite Gate;

        public bool IsComplete =>
            Wall != null
            && Leaves != null
            && Brick != null
            && BrickTop != null
            && Ramp != null
            && FastFloor != null
            && FastSign != null
            && Pot != null
            && Vine != null
            && Lift != null
            && Gate != null;
    }

    [CreateAssetMenu(menuName = "Healthy Race/Visual Catalog")]
    public sealed class VisualCatalog : ScriptableObject
    {
        public Sprite[] Characters = new Sprite[5];

        /// <summary>
        /// Retratos de Zuri e Tom para a interface: cinco por jogador, um por faixa de
        /// Bem-Estar (muda só a faixa de cabeça), jogador 1 primeiro.
        /// </summary>
        public Sprite[] RunnerPortraits = new Sprite[10];

        /// <summary>Zuri e Tom de corpo inteiro, correndo, para as telas. Jogador 1 primeiro.</summary>
        public Sprite[] RunnerPoses = new Sprite[2];

        /// <summary>Partes do personagem articulado, uma pele por estado de Bem-Estar.</summary>
        public CharacterSkin[] Skins = new CharacterSkin[5];

        public ArenaArt Arena;

        public GreenhouseArt Greenhouse;
        public Sprite[] Foods = new Sprite[18];
        public Sprite[] Environment = new Sprite[13];
        public Sprite Background;
        public RuntimeAnimatorController PlayerAnimator;
        public Sprite Particle;
        public Sprite Heart;
        public Sprite Droplet;
        public Sprite Ring;
        public Material SpriteMaterial;
        public Color PlayerOne = new Color32(49, 142, 240, 255);
        public Color PlayerTwo = new Color32(192, 106, 235, 255);

        public static readonly string[] FoodIds =
        {
            "apple",
            "banana",
            "orange",
            "grapes",
            "strawberry",
            "carrot",
            "broccoli",
            "tomato",
            "water",
            "nuts",
            "burger",
            "pizza",
            "soda",
            "fries",
            "donut",
            "hotdog",
            "icecream",
            "chocolate",
        };
        public static readonly string[] FoodNames =
        {
            "Maçã",
            "Banana",
            "Laranja",
            "Uva",
            "Morango",
            "Cenoura",
            "Brócolis",
            "Tomate",
            "Água",
            "Castanhas",
            "Hambúrguer",
            "Pizza",
            "Refrigerante",
            "Batata frita",
            "Donut",
            "Cachorro-quente",
            "Sorvete",
            "Chocolate",
        };
        public static readonly string[] StateNames =
        {
            "VITA saudável",
            "VITA estável",
            "VITA cansado",
            "Estado crítico",
            "SALO",
        };

        // Presentation fallback for previews and snapshot-only consumers. Gameplay owns its bands.
        public static WellbeingAppearance AppearanceFor(int value)
        {
            if (value <= 0)
                return WellbeingAppearance.Salo;
            if (value <= 25)
                return WellbeingAppearance.Critical;
            if (value <= 50)
                return WellbeingAppearance.Tired;
            return value <= 75 ? WellbeingAppearance.Stable : WellbeingAppearance.Healthy;
        }

        public Color IdentityColor(int player) => player == 2 ? PlayerTwo : PlayerOne;

        public Sprite Character(WellbeingAppearance appearance) => Characters[(int)appearance];

        /// <summary>
        /// Retrato do jogador na faixa de Bem-Estar dada. Sem os retratos dos corredores
        /// (catálogo antigo), cai no sprite do broto.
        /// </summary>
        public Sprite Portrait(int player, WellbeingAppearance appearance)
        {
            var index = (player == 2 ? 5 : 0) + (int)appearance;
            var portrait =
                RunnerPortraits != null && index < RunnerPortraits.Length
                    ? RunnerPortraits[index]
                    : null;
            return portrait != null ? portrait : Character(appearance);
        }

        /// <summary>Corredor de corpo inteiro do jogador; sem ele, o broto saudável.</summary>
        public Sprite RunnerPose(int player)
        {
            var index = player == 2 ? 1 : 0;
            var pose =
                RunnerPoses != null && index < RunnerPoses.Length ? RunnerPoses[index] : null;
            return pose != null ? pose : Characters[0];
        }

        public bool TryGetSkin(WellbeingAppearance appearance, out CharacterSkin skin)
        {
            var index = (int)appearance;
            if (Skins != null && index >= 0 && index < Skins.Length && Skins[index].IsComplete)
            {
                skin = Skins[index];
                return true;
            }

            skin = default;
            return false;
        }

        public Sprite Food(string id)
        {
            var index = Array.IndexOf(FoodIds, id);
            return index >= 0 && index < Foods.Length ? Foods[index] : null;
        }
    }
}
