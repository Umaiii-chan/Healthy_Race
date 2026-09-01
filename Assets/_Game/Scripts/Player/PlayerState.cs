namespace HealthyRace.Player
{
    /// <summary>
    /// Describes the locomotion state inferred from the player's physics state.
    /// </summary>
    public enum PlayerState
    {
        Idle,
        Move,
        Jump,
        Fall,
        Land,
    }
}
