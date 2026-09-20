using HealthyRace.WellBeing;
using UnityEngine;
using UnityEngine.UI;

namespace HealthyRace.Visuals
{
    public sealed class PlayerHudView : MonoBehaviour
    {
        [SerializeField]
        private VisualCatalog _catalog;

        [SerializeField]
        private Text _identity;

        [SerializeField]
        private Image _portrait;

        [SerializeField]
        private Image _bar;

        [SerializeField]
        private Text _wellbeing;

        [SerializeField]
        private Text _score;

        [SerializeField]
        private Text _counts;

        [SerializeField]
        private Image _progress;
        private WellbeingComponent _source;
        private FatigueComponent _fatigue;
        public PlayerHudData Data { get; private set; }

        public void Configure(
            VisualCatalog catalog,
            Text identity,
            Image portrait,
            Image bar,
            Text wellbeing,
            Text score,
            Text counts,
            Image progress
        )
        {
            _catalog = catalog;
            _identity = identity;
            _portrait = portrait;
            _bar = bar;
            _wellbeing = wellbeing;
            _score = score;
            _counts = counts;
            _progress = progress;
        }

        public void Bind(WellbeingComponent source, FatigueComponent fatigue)
        {
            Unsubscribe();
            _source = source;
            _fatigue = fatigue;
            if (isActiveAndEnabled)
                Subscribe();
        }

        private void OnEnable() => Subscribe();

        private void OnDisable() => Unsubscribe();

        private void OnDestroy() => Unsubscribe();

        private void Subscribe()
        {
            if (_source != null)
            {
                _source.ValueChanged -= OnWellbeing;
                _source.ValueChanged += OnWellbeing;
                OnWellbeing(_source.CurrentValue);
            }
            if (_fatigue != null)
            {
                _fatigue.EpisodeCountChanged -= OnFatigue;
                _fatigue.EpisodeCountChanged += OnFatigue;
                OnFatigue(_fatigue.FatigueEpisodeCount);
            }
        }

        private void Unsubscribe()
        {
            if (_source != null)
                _source.ValueChanged -= OnWellbeing;
            if (_fatigue != null)
                _fatigue.EpisodeCountChanged -= OnFatigue;
        }

        private void OnWellbeing(int value)
        {
            var data = Data;
            data.Wellbeing = value;
            Render(data);
        }

        private void OnFatigue(int value)
        {
            var data = Data;
            data.FatigueEpisodes = value;
            Render(data);
        }

        public void Render(PlayerHudData data)
        {
            Data = data;
            if (_catalog == null)
                return;
            var appearance = VisualCatalog.AppearanceFor(data.Wellbeing);
            _identity.text = data.Player == 2 ? "P2  ◆" : "P1  ●";
            _identity.color = _catalog.IdentityColor(data.Player);
            _portrait.sprite = _catalog.Character(appearance);
            _bar.fillAmount = Mathf.Clamp01(data.Wellbeing / 100f);
            _bar.color =
                appearance >= WellbeingAppearance.Critical
                    ? new Color(1f, 0.55f, 0.4f)
                    : new Color(0.65f, 0.9f, 0.35f);
            _wellbeing.text =
                $"{VisualCatalog.StateNames[(int)appearance]}  {Mathf.Clamp(data.Wellbeing, 0, 100)}/100";
            _score.text = $"{VisualUiFormat.Number(data.Score)}  pts";
            _counts.text =
                $"Saudáveis {data.HealthyFoods}   Ultra {data.UltraProcessedFoods}   Fadigas {data.FatigueEpisodes}";
            _progress.fillAmount = float.IsNaN(data.Progress) ? 0 : Mathf.Clamp01(data.Progress);
        }
    }
}
