using System.Collections.Generic;
using BSDiscordRanking.Formats.Level;

namespace BSDiscordRanking.Formats.Player
{
    public class PlayerPassFormat
    {
        public List<SongFormat> songs { get; set; }
    }

    public class PlayerPassPerLevelFormat
    {
        public List<InPassPerLevelFormat> Levels { get; set; }
    }

    public class InPassPerLevelFormat
    {
        public int LevelID { get; set; }
        public int NumberOfPass { get; set; }
        public int NumberOfMapDiffInLevel { get; set; }
        public Trophy Trophy { get; set; }
        public string TrophyString { get; set; }
    }
}