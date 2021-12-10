using System.Collections.Generic;

namespace BSDiscordRanking.Formats.API
{
    public class PlayerCollection
    {
        public List<ApiPlayer> players { get; set; }
        public Metadata metadata { get; set; }
    }
    
    public class ApiPlayer
    {
        public string id { get; set; }
        public string name { get; set; }
        public string profilePicture { get; set; }
        public string country { get; set; }
        public float pp { get; set; }
        public int rank { get; set; }
        public int countryRank { get; set; }
        public string role { get; set; }
        public List<ApiBadge> badges { get; set; }
        public string histories { get; set; }
        public short permissions { get; set; }
        public bool banned { get; set; }
        public bool inactive { get; set; }
        public ApiScoreStats scoreStats { get; set; }
    }

    public class ApiBadge
    {
        public string description { get; set; }
        public string image { get; set; }
    }

    public class ApiScoreStats
    {
        public long totalScore { get; set; }
        public long totalRankedScore { get; set; }
        public double averageRankedAccuracy { get; set; }
        public int totalPlayCount { get; set; }
        public int rankedPlayCount { get; set; }
        public int replaysWatched { get; set; }
    }

    public class ApiScoreCollection
    {
        public List<ApiScore> scores { get; set; }
        public ApiMetadata metadata { get; set; }
    }
    public class ApiPlayerScoreCollection
    {
        public List<ApiPlayerScore> playerScores { get; set; }
        public ApiMetadata metadata { get; set; }
    }

    public class ApiPlayerScore
    {
        public ApiScore score { get; set; }
        public ApiLeaderboardInfo leaderboard { get; set; }
    }

    public class ApiScore
    {
        public ulong id { get; set; }
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
        public bool fullCombo { get; set; }
        public short hmd { get; set; }
        public bool hasReplay { get; set; }
        public string timeSet { get; set; }
    }

    public class ApiLeaderboardInfoCollection
    {
        public ApiLeaderboardInfo leaderboards { get; set; }
        public Metadata metadata { get; set; }
    }

    public class ApiLeaderboardInfo
    {
        public int id { get; set; }
        public string songHash { get; set; }
        public string songName { get; set; }
        public string songSubName { get; set; }
        public string songAuthorName { get; set; }
        public string levelAuthorName { get; set; }
        public ApiDifficulty difficulty { get; set; }
        public int maxScore { get; set; }
        public string createdDate { get; set; }
        public string rankedDate { get; set; }
        public string qualifiedDate { get; set; }
        public string lovedDate { get; set; }
        public bool ranked { get; set; }
        public bool qualified { get; set; }
        public bool loved { get; set; }
        public float maxPP { get; set; }
        public float stars { get; set; }
        public bool positiveModifiers { get; set; }
        public int plays { get; set; }
        public int dailyPlays { get; set; }
        public string coverImage { get; set; }
        public ApiScore playerScore { get; set; }
        public List<ApiDifficulty> difficulties { get; set; }
    }

    public class ApiMetadata
    {
        public int total { get; set; }
        public int page { get; set; }
        public int itemsPerPage { get; set; }
    }

    public class ApiLeaderboardPlayerInfo
    {
        public string id { get; set; }
        public string name { get; set; }
        public string profilePicture { get; set; }
        public string country { get; set; }
        public short permissions { get; set; }
        public string role { get; set; }
    }

    public class ApiDifficulty
    {
        public int leaderboardID { get; set; }
        public int difficulty { get; set; }
        public string gameMode { get; set; }
        public string difficultyRaw { get; set; }
    }


    public class ApiCheck
    {
        public string service { get; set; }
        public string version { get; set; }
    }
}