using System.Collections.Generic;

namespace BSDiscordRanking.Formats
{
    public class LeaderboardControllerFormat
    {
        public List<RankedPlayer> Leaderboard { get; set; }
    }

    public class RankedPlayer
    {
        public string Name { get; set; }
        public string ScoreSaberID { get; set; }
        public float Points { get; set; }
        public int Level { get; set; }
        public Trophy Trophy { get; set; }
        public bool Ping { get; set; }
    }
}