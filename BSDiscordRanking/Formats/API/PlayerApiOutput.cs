using System.Collections.Generic;
using BSDiscordRanking.Formats.Player;
using BSDiscordRanking.Utils;
using Discord;
using Newtonsoft.Json;

namespace BSDiscordRanking.Formats.API
{
    public class PlayerApiOutput
    {
        public ApiPlayer PlayerFull { get; set; }
        public PlayerStatsFormat PlayerStats { get; set; }
        
        public CustomApiPlayer CustomData { get; set; }
    }

    public class CustomApiPlayer
    {
        public string PassPointsName { get; set; } = "PassPoints";
        public string AccPointsName { get; set; } = "AccPoints";
        public int PassRank { get; set; }
        public int AccRank { get; set; }
        public int Level { get; set; }
        
        [JsonConverter(typeof(DiscordColorConverter))]
        public Color ProfileColor { get; set; }
        public Trophy Trophy { get; set; }
        public List<CustomApiPlayerCategory> Categories { get; set; }
    }

    public class CustomApiPlayerCategory
    {
        private string m_Category;
        public string Category
        {
            get => m_Category;
            set => m_Category = string.IsNullOrEmpty(value) ? null : value;
        }
        
        public int Level { get; set; }
        public int MaxLevel { get; set; }
        public int NumberOfPass { get; set; }
        public int TotalNumberOfMaps { get; set; }
        public Trophy Trophy { get; set; }
    }
}