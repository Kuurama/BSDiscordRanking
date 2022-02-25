using BSDiscordRanking.Formats.Player;

namespace BSDiscordRanking.Formats.API
{
    public class PlayerApiOutput
    {
        public ApiPlayer PlayerFull { get; set; }
        public PlayerStatsFormat PlayerStats { get; set; }
    }
}