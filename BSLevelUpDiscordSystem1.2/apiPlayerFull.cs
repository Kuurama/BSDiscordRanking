namespace BSLevelUpDiscordSystem1._2
{
    public class apiPlayerFull
    {
        public InfoFormat playerInfo { get; set; }
        public StatsFormat scoreStats { get; set; }
    }

    public class InfoFormat
    {
        public string playerId { get; set; }
        public string playerName { get; set; }
        public string avatar { get; set; }
        public int rank { get; set; }
        public int countryRank { get; set; }
        public float pp { get; set; }
        public string country { get; set; }
        public string role { get; set; }
        public string[] badges { get; set; }
        public string history { get; set; }
        public short permissions { get; set; }
        public short inactive { get; set; }
        public short banned { get; set; }
    }

    public class StatsFormat
    {
        public long totalScore { get; set; }
        public long totalRankedScore { get; set; }
        public double averageRankedAccuracy { get; set; }
        public int totalPlayCount { get; set; }
        public int rankedPlayCount { get; set; }
    }
}