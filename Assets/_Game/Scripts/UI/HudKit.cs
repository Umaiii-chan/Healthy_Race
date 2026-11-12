using UnityEngine;

namespace HealthyRace.UI
{
    /// <summary>
    /// Peças do HUD das fases (HUD 5 da página de propostas: placas penduradas, cinco
    /// folhas, relógio de madeira e trepadeira da corrida). Fica em Resources porque o HUD
    /// é montado por código em cada fase; quem preenche é o construtor do Editor.
    /// </summary>
    [CreateAssetMenu(menuName = "Healthy Race/HUD Kit")]
    public sealed class HudKit : ScriptableObject
    {
        public const string ResourcePath = "SO_HudKit";
        public const int PortraitsPerPlayer = 5;

        public Sprite Sign;
        public Sprite LeafOn;
        public Sprite LeafOff;
        public Sprite Clock;
        public Sprite Vine;
        public Sprite Goal;
        public Sprite Disk;
        public Sprite RingInk;
        public Sprite RingFill;
        public Sprite Tag;

        /// <summary>Vaso do "Prontos?" do menu; branco, tingido na cor do jogador.</summary>
        public Sprite Pad;

        /// <summary>Estrelas da tela de resultado: conquistada e não conquistada.</summary>
        public Sprite StarOn;
        public Sprite StarOff;

        /// <summary>Cinco retratos por jogador, um por faixa de Bem-Estar; jogador 1 primeiro.</summary>
        public Sprite[] Portraits = new Sprite[PortraitsPerPlayer * 2];

        private static HudKit _cached;

        public static HudKit Load()
        {
            if (_cached == null)
            {
                _cached = Resources.Load<HudKit>(ResourcePath);
            }

            return _cached;
        }

        public bool IsComplete =>
            Sign != null
            && LeafOn != null
            && LeafOff != null
            && Clock != null
            && Vine != null
            && Goal != null
            && Disk != null
            && RingInk != null
            && RingFill != null
            && Tag != null
            && Pad != null;

        /// <summary>Retrato do jogador na faixa dada (0 = saudável … 4 = no fim), ou nulo.</summary>
        public Sprite Portrait(int player, int appearance)
        {
            var index =
                (player == 2 ? PortraitsPerPlayer : 0)
                + Mathf.Clamp(appearance, 0, PortraitsPerPlayer - 1);
            return Portraits != null && index < Portraits.Length ? Portraits[index] : null;
        }
    }
}
