using System;
using System.Collections.Generic;

namespace BSDiscordRanking.Formats
{
    public class LevelFormat
    {
        private string m_SyncURL;
        public List<SongFormat> songs { get; set; }
        public string playlistTitle { get; set; }
        public string playlistAuthor { get; set; }
        public string playlistDescription { get; set; }

        public string syncURL
        {
            get => m_SyncURL;
            set
            {
                if (String.IsNullOrEmpty(value))
                    m_SyncURL = null;
                else
                    m_SyncURL = value;
            }
        }

        public string image { get; set; }
    }

    public class SongFormat
    {
        public string hash { get; set; }
        public List<InSongFormat> difficulties { get; set; }
        public string name { get; set; }
        public string[] nameParts => name.Split(' ', '-', '_');
    }

    public class InSongFormat
    {
        public string characteristic { get; set; }
        public string name { get; set; }
        
        public int minScoreRequirement { get; set; }
    }
}