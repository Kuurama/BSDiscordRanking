using System.Collections.Generic;

namespace BSDiscordRanking
{
    public class PlayerStatsFormat
    {
        public List<bool> LevelIsPassed { get; set; }
        public int TotalNumberOfPass { get; set; }
        public List<int> Trophy { get; set; }
    }
}