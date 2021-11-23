using System.Collections.Generic;
using BSDiscordRanking.Formats.API;

namespace BSDiscordRanking.Formats.Controller
{
    public class MapLeaderboardFormat
    {
        public string key { get; set; }
        
        public ApiLeaderboard info { get; set; }
        
        public List<MapPlayerScore> scores { get; set; }
    }

    public class MapPlayerScore
    {
        public ulong id { get; set; }
        public bool isBotRegistered { get; set; } /// <summary>
                                                  /// Added this field to help displaying only the players registered on the bot.
                                                  /// </summary>
        public ApiLeaderboardPlayerInfo leaderboardPlayerInfo { get; set; }
        public int rank { get; set; }
        public int baseScore { get; set; }
        public int modifiedScore { get; set; }
        public float pp { get; set; }
        public float weight { get; set; }
        public string modifiers { get; set; }
        public float multiplier { get; set; }
        public int badCuts { get; set; }
        public int missedNotes { get; set; }
        public int maxCombo { get; set; }
        public short fullCombo { get; set; }
        public short hmd { get; set; }
        public bool hasReplay { get; set; }
        public string timeSet { get; set; }
    }
    
}