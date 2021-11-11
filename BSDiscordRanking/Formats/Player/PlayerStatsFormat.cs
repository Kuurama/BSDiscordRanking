using System.Collections.Generic;

namespace BSDiscordRanking.Formats.Player
{
    public class PlayerStatsFormat
    {
        public List<bool> LevelIsPassed { get; set; }
        public int TotalNumberOfPass { get; set; }

        public List<Trophy> Trophy { get; set; }
        //plastic 0% 1
        //silver 40% 2
        //gold 70% 3
        //diamond 100% 4

        public float PassPoints { get; set; }
        public float AccPoints { get; set; }

        public int IsFirstScan { get; set; }
    }

    public class Trophy
    {
        public int Plastic { get; set; }
        public int Silver { get; set; }
        public int Gold { get; set; }
        public int Diamond { get; set; }
    }
}