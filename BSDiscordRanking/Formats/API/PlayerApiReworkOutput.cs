using System.Collections.Generic;
using BSDiscordRanking.Formats.Player;
using BSDiscordRanking.Utils;
using Discord;
using Newtonsoft.Json;

namespace BSDiscordRanking.Formats.API
{
    public struct PlayerApiReworkOutput
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Country { get; set; }
        public string ProfilePicture { get; set; }
        
        [JsonConverter(typeof(DiscordColorConverter))]
        public Color ProfileColor { get; set; }
        
        public List<ApiBadge> Badges { get; set; }
        public Trophy Trophy { get; set; }
        public int Level { get; set; }
        public bool IsMapLeaderboardBanned { get; set; }
        public bool IsScanBanned { get; set; }
        // ReSharper disable once InconsistentNaming
        public List<RankData> RankData;
        public List<CustomApiPlayerCategory> CategoryData { get; set; }
        
    }

    public class RankData
    {
        public string PointsType { get; set; }
        public string PointsName { get; set; }
        public float Points { get; set; }
        public int Rank { get; set; }
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