using System.Collections.Generic;
using BSDiscordRanking.Formats.Level;

namespace BSDiscordRanking.Formats.Player
{
    public class PlayerPassFormat
    {
        public List<InPlayerSong> SongList { get; set; }
    }

    public class InPlayerSong
    {
        public string hash { get; set; }
        public string key { get; set; }
        public string name { get; set; }
        public List<InPlayerPassFormat> DiffList { get; set; }
    }
    public class InPlayerPassFormat
    {
        public Difficulty Difficulty { get; set; }
        public float Score { get; set; }
        public int Rank { get; set; }
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