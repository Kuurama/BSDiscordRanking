using System.Collections.Generic;

namespace BSLevelUpDiscordSystem1._2
{
    public class apiPlayerScore
    {
        public List<ScoreFormat> scores { get; set; }
    }

    public class ScoreFormat
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