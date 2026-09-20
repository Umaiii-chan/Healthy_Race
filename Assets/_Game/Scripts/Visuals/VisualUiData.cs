using System;
using System.Globalization;

namespace HealthyRace.Visuals
{
    [Serializable]
    public struct PlayerHudData
    {
        public int Player;
        public int Wellbeing;
        public int Score;
        public int HealthyFoods;
        public int UltraProcessedFoods;
        public int FatigueEpisodes;
        public float Progress;
    }

    [Serializable]
    public struct PlayerResultData
    {
        public PlayerHudData Player;
        public float Seconds;
        public bool Finished;
        public int HealthyPoints;
        public int PuzzlePoints;
        public int TimeBonus;
        public int ArrivalBonus;
        public int WellbeingBonus;
        public int UltraPenalty;
        public int FatiguePenalty;
        public int UnfinishedPenalty;
    }

    public enum VisualScreen
    {
        Boot,
        MainMenu,
        Gameplay,
        Pause,
        Results,
        Controls,
        Audio,
    }

    public enum VisualCommand
    {
        Play,
        Continue,
        Restart,
        Menu,
        Quit,
        Controls,
        Audio,
        Back,
    }

    public static class VisualUiFormat
    {
        private static readonly CultureInfo Portuguese = CultureInfo.GetCultureInfo("pt-BR");

        public static string Number(int value) => value.ToString("N0", Portuguese);

        public static string Clock(float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds))
                return "--:--";
            var value = (int)Math.Max(0, Math.Min(seconds, 359999));
            return $"{value / 60:00}:{value % 60:00}";
        }

        public static string FinalWindow(float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds))
                return "JANELA FINAL  --";
            return $"JANELA FINAL  {Math.Max(0, Math.Ceiling(seconds)):00}s";
        }

        public static string Result(PlayerResultData data) =>
            $"SCORE FINAL\n{Number(data.Player.Score)}\n\n"
            + $"Alimentos saudáveis / ultra     {data.Player.HealthyFoods} / {data.Player.UltraProcessedFoods}\n"
            + $"Bem-Estar     {data.Player.Wellbeing}/100\nFadigas     {data.Player.FatigueEpisodes}\n"
            + $"Tempo     {(data.Finished ? Clock(data.Seconds) : "Não concluiu")}\n\n"
            + $"Alimentos     +{Number(data.HealthyPoints)}\nPuzzle     +{Number(data.PuzzlePoints)}\n"
            + $"Bônus de tempo     +{Number(data.TimeBonus)}\nBônus de chegada     +{Number(data.ArrivalBonus)}\n"
            + $"Bônus de Bem-Estar     +{Number(data.WellbeingBonus)}\n\n"
            + $"Penalidade ultra     −{Number(data.UltraPenalty)}\nPenalidade fadiga     −{Number(data.FatiguePenalty)}\n"
            + $"Não conclusão     −{Number(data.UnfinishedPenalty)}";
    }
}
