using System.Collections.Generic;

namespace BSLevelUpDiscordSystem1._2
{
    public class ApiPlayerFull
    {
        public ApiPlayerInfo playerInfo { get; set; }
        public ApiScoreStats scoreStats { get; set; }
    }

    public class ApiPlayerInfo
    {
        public string playerId { get; set; }
        public string playerName { get; set; }
        public string avatar { get; set; }
        public int rank { get; set; }
        public int countryRank { get; set; }
        public float pp { get; set; }
        public string country { get; set; }
        public string role { get; set; }
        public ApiPlayerBadge[] badges { get; set; }
        public string history { get; set; }
        public short permissions { get; set; }
        public short inactive { get; set; }
        public short banned { get; set; }
    }

    public class ApiPlayerBadge
    {
        public string image { get; set; }
        public string description { get; set; }
    }

    public class ApiScoreStats
    {
        public long totalScore { get; set; }
        public long totalRankedScore { get; set; }
        public double averageRankedAccuracy { get; set; }
        public int totalPlayCount { get; set; }
        public int rankedPlayCount { get; set; }
    }

    public class ApiScores
    {
        public List<ApiScore> scores { get; set; }
    }

    public class ApiScore
    {
        public int rank { get; set; }
        public int scoreId { get; set; }
        public int score { get; set; }
        public int unmodififiedScore { get; set; }
        public string mods { get; set; }
        public float pp { get; set; }
        public float weight { get; set; }
        public string timeSet { get; set; }
        public int leaderboardId { get; set; }
        public string songHash { get; set; }
        public string songName { get; set; }
        public string songSubName { get; set; }
        public string songAuthorName { get; set; }
        public string levelAuthorName { get; set; }
        public float difficulty { get; set; }
        public string difficultyRaw { get; set; }
        public int maxScore { get; set; }
    }
}