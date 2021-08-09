using System.Collections.Generic;

namespace BSDiscordRanking
{
    public class PlayerStatsFormat
    {
        public List<bool> LevelIsPassed { get; set; }
        public int TotalNumberOfPass { get; set; }

        public List<int> Trophy { get; set; }
        //plastic 0% 1
        //silver 40% 2
        //gold 70% 3
        //diamond 100% 4
    }
}