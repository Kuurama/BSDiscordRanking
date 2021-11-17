using System.Collections.Generic;

namespace BSDiscordRanking.Formats.Player
{
    public class PlayerStatsFormat
    {
        public List<PassedLevel> Levels { get; set; }
        public int TotalNumberOfPass { get; set; }

        public float PassPoints { get; set; }
        public float AccPoints { get; set; }

        public bool IsFirstScan { get; set; }
    }

    public class Trophy
    {
        public int Plastic { get; set; }
        public int Silver { get; set; }
        public int Gold { get; set; }
        public int Diamond { get; set; }
    }

    public class PassedLevel
    {
        public PassedLevel()
        {
        }

        public PassedLevel(int p_LevelID, bool p_Passed, Trophy p_Trophy)
        {
            LevelID = p_LevelID;
            Passed = p_Passed;
            Trophy = p_Trophy;
        }

        public int LevelID { get; set; }
        public bool Passed { get; set; }
        public Trophy Trophy { get; set; }
    }
}