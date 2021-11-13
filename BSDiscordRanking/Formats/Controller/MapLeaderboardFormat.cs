using System.Collections.Generic;

namespace BSDiscordRanking.Formats.Controller
{
    public class MapLeaderboardFormat
    {
        public string hash { get; set; }
        public string key { get; set; }
        public string name { get; set; }
        public int MaxScore { get; set; }
        public List<MapPlayerPass> Leaderboard { get; set; }
    }

    public class MapPlayerPass
    {
        public string Name { get; set; }
        public string ScoreSaberID { get; set; }
        public int Score { get; set; }
    }
}