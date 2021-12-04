using System.Collections.Generic;

namespace BSDiscordRanking.Formats.Level
{
    public class LevelFormat
    {
        public MainCustomData customData { get; set; }
        public List<SongFormat> songs { get; set; }
        public string playlistTitle { get; set; }
        public string playlistAuthor { get; set; }
        public string playlistDescription { get; set; }
        public string image { get; set; }
    }

    public class SongFormat
    {
        public string hash { get; set; }
        public string key { get; set; }
        public List<Difficulty> difficulties { get; set; }
        public string name { get; set; }
    }

    public class Difficulty
    {
        public string characteristic { get; set; }
        public string name { get; set; }
        public DiffCustomData customData { get; set; }
    }

    public class MainCustomData
    {
        private string m_CustomPassText;
        private string m_SyncURL;

        public string syncURL
        {
            get => m_SyncURL;
            set => m_SyncURL = string.IsNullOrEmpty(value) ? null : value;
        }

        public int level { get; set; }
        public int autoWeightDifficultyMultiplier { get; set; }
        public float weighting { get; set; }

        public string customPassText
        {
            get => m_CustomPassText;
            set => m_CustomPassText = string.IsNullOrEmpty(value) ? null : value;
        }
    }

    public class DiffCustomData
    {
        private string m_Category;
        private string m_CustomPassText;
        private string m_InfoOnGGP;

        public int levelWorth { get; set; }
        public int leaderboardID { get; set; }
        public float manualWeight { get; set; }
        public float AutoWeight { get; set; }
        public int minScoreRequirement { get; set; }

        public string category
        {
            get => m_Category;
            set => m_Category = string.IsNullOrEmpty(value) ? null : value;
        }

        public string customPassText
        {
            get => m_CustomPassText;
            set => m_CustomPassText = string.IsNullOrEmpty(value) ? null : value;
        }

        public string infoOnGGP
        {
            get => m_InfoOnGGP;
            set => m_InfoOnGGP = string.IsNullOrEmpty(value) ? null : value;
        }

        public int noteCount { get; set; }
        public int maxScore { get; set; }
        public bool forceManualWeight { get; set; }
        public bool adminConfirmationOnPass { get; set; }
    }
}