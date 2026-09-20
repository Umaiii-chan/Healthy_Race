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
    }

    [CreateAssetMenu(menuName = "Healthy Race/Visual Catalog")]
    public sealed class VisualCatalog : ScriptableObject
    {
        public Sprite[] Characters = new Sprite[5];
        public Sprite[] Foods = new Sprite[18];
        public Sprite[] Environment = new Sprite[13];
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

        public Sprite Food(string id)
        {
            var index = Array.IndexOf(FoodIds, id);
            return index >= 0 && index < Foods.Length ? Foods[index] : null;
        }
    }
}
