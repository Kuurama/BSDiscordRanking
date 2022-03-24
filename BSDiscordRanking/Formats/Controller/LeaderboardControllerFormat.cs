using System.Collections.Generic;
using BSDiscordRanking.Formats.Player;

namespace BSDiscordRanking.Formats.Controller
{
    public class LeaderboardControllerFormat
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public string Page { get; set; }
        public int MaxPage { get; set; }
        public int CountPerPage { get; set; }
        public List<RankedPlayer> Leaderboard { get; set; }
    }

    public class RankedPlayer
    {
        public string Name { get; set; }
        public string ScoreSaberID { get; set; }
        public string DiscordID { get; set; }
        public float Points { get; set; }
        public int Level { get; set; }
        public Trophy Trophy { get; set; }
        public bool IsPingAllowed { get; set; }
        public bool IsBanned { get; set; } = false;
    }
}