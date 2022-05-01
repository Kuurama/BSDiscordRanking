using System.Collections.Generic;
using BSDiscordRanking.Formats.API;

namespace BSDiscordRanking.Formats.Player
{
    public class PlayerStatsFormat
    {
        public List<PassedLevel> Levels { get; set; }
        public int TotalNumberOfPass { get; set; }

        public float PassPoints { get; set; }
        public float AccPoints { get; set; }
        public List<ApiBadge> Badges { get; set; }
        public bool IsFirstScan { get; set; }
        public bool IsMapLeaderboardBanned { get; set; }
        public bool IsScanBanned { get; set; }
    }

    public class Trophy
    {
        public int Plastic { get; set; }
        public int Silver { get; set; }
        public int Gold { get; set; }
        public int Diamond { get; set; }
        public int Ruby { get; set; }
    }

    public class PassedLevel
    {
        public int LevelID { get; set; }
        public bool Passed { get; set; }
        public int NumberOfPass { get; set; }
        public int TotalNumberOfMaps { get; set; }
        public Trophy Trophy { get; set; }
        public List<CategoryPassed> Categories { get; set; }
    }

    public class CategoryPassed
    {
        public string Category { get; set; }
        public bool Passed { get; set; }
        public int NumberOfPass { get; set; }
        public int TotalNumberOfMaps { get; set; }
        public Trophy Trophy { get; set; }
    }
}