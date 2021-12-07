using System.Collections.Generic;
using BSDiscordRanking.Formats.API;

namespace BSDiscordRanking.Formats.Controller
{
    public class MapLeaderboardFormat
    {
        public string key { get; set; }
        public bool forceAutoWeightRecalculation { get; set; }

        public ApiLeaderboard info { get; set; }

        public List<MapPlayerScore> scores { get; set; }
    }

    public class MapPlayerScore
    {
        public ApiScore score { get; set; }
        public LeaderboardCustomData customData { get; set; }
    }

    public class LeaderboardCustomData
    {
        public bool isBotRegistered { get; set; }

        /// <summary>
        /// Added this field to help displaying only the players registered on the bot.
        /// </summary>
        public bool isBanned { get; set; } = false;
    }
}