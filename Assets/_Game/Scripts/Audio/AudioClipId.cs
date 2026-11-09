namespace HealthyRace.Audio
{
    /// <summary>
    /// Os valores são explícitos porque o catálogo de áudio os grava como número:
    /// novos sons entram com um valor novo, nunca reaproveitando ou reordenando.
    /// </summary>
    public enum AudioClipId
    {
        // Music
        Music_Menu = 0,
        Music_Level01 = 1,

        // SFX Locomotion & Physics
        Sfx_Jump = 2,
        Sfx_Step = 3,

        // SFX Collectibles & Health
        Sfx_PickupHealthy = 4,
        Sfx_PickupUltra = 5,
        Sfx_WellbeingUp = 6,
        Sfx_WellbeingDown = 7,
        Sfx_Fatigue = 8,

        // SFX Puzzles & Mechanisms
        Sfx_Button = 9,
        Sfx_Lever = 10,
        Sfx_Door = 11,
        Sfx_PuzzleSolved = 12,

        // SFX Milestones & Results
        Sfx_Checkpoint = 13,
        Sfx_Finish = 14,
        Sfx_Victory = 15,
        Sfx_Defeat = 16,
        Sfx_Tie = 17,
    }
}
