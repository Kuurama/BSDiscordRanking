using System;
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
        public List<Difficulties> difficulties { get; set; }
        public string name { get; set; }
    }

    public class Difficulties
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
            set => m_SyncURL = String.IsNullOrEmpty(value) ? null : value;
        }

        public float weighting { get; set; }

        public string customPassText
        {
            get => m_CustomPassText;
            set => m_CustomPassText = String.IsNullOrEmpty(value) ? null : value;
        }
    }

    public class DiffCustomData
    {
        private string m_CustomPassText;
        private string m_Category;
        private string m_InfoOnGGP;
        public float weighting { get; set; }
        public int minScoreRequirement { get; set; }

        public string category
        {
            get => m_Category;
            set => m_Category = String.IsNullOrEmpty(value) ? null : value;
        }

        public string customPassText
        {
            get => m_CustomPassText;
            set => m_CustomPassText = String.IsNullOrEmpty(value) ? null : value;
        }

        public string infoOnGGP
        {
            get => m_InfoOnGGP;
            set => m_InfoOnGGP = String.IsNullOrEmpty(value) ? null : value;
        }
    }
}