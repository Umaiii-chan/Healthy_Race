using UnityEngine;

namespace HealthyRace.Visuals
{
    /// <summary>
    /// Peças de cenário do kit aprovado (Cenário 3): placas sem texto, selo do corredor do
    /// lanche, espinhos claros sobre faixa de perigo, mecanismo amarelo, marco de fundo por
    /// fase, luz pintada e vasos pendurados. Fica em Resources porque a fase é montada por
    /// código; sem o asset a fase usa a arte anterior.
    /// </summary>
    [CreateAssetMenu(menuName = "Healthy Race/Scenery Kit")]
    public sealed class SceneryKit : ScriptableObject
    {
        public const string ResourcePath = "SO_SceneryKit";

        public Sprite ForkRight;
        public Sprite ForkLeft;
        public Sprite LeverSign;
        public Sprite FastBadge;
        public Sprite Spikes;
        public Sprite Hazard;
        public Sprite Gate;
        public Sprite GateCap;
        public Sprite Lift;
        public Sprite LeverBase;
        public Sprite Knob;
        public Sprite HangingPot;
        public Sprite Light;

        /// <summary>Marcos em silhueta; cada fase usa um, pela ordem.</summary>
        public Sprite[] Landmarks = new Sprite[0];

        private static SceneryKit _cached;

        public static SceneryKit Load()
        {
            if (_cached == null)
            {
                _cached = Resources.Load<SceneryKit>(ResourcePath);
            }

            return _cached;
        }

        public bool IsComplete =>
            ForkRight != null
            && ForkLeft != null
            && LeverSign != null
            && FastBadge != null
            && Spikes != null
            && Hazard != null
            && Gate != null
            && GateCap != null
            && Lift != null
            && LeverBase != null
            && Knob != null
            && HangingPot != null
            && Light != null;

        /// <summary>Marco da fase (1 em diante); nulo sem marcos.</summary>
        public Sprite LandmarkFor(int levelNumber)
        {
            if (Landmarks == null || Landmarks.Length == 0)
            {
                return null;
            }

            return Landmarks[Mathf.Max(0, levelNumber - 1) % Landmarks.Length];
        }
    }
}
