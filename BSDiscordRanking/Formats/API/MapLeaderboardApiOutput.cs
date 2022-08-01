using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using BSDiscordRanking.Utils;
using Discord;

namespace BSDiscordRanking.Formats.API
{
    public struct ApiMapLeaderboardCollectionStruct
    {
        public List<ApiMapLeaderboardContentStruct> Leaderboards { get; set; }
        public ApiPageMetadataStruct Metadata { get; set; }
        public ApiCustomDataStruct CustomData { get; set; }
    }

   public struct ApiMapLeaderboardContentStruct
    {
        public UInt64 ScoreSaberID { get; set; }
        public int Rank { get; set; }
        public string Name { get; set; }
        public string Country { get; set; }
        public string Avatar { get; set; }
        public List<RankData> RankData;
        /// <summary>
        /// The weight is used to display how much points it actually really gave to the player (=> Earned Points = Points * Weight) (not used yet on BSDR, but it's something used on ScoreSaber)
        /// </summary>
        public float Weight { get; set; }
        public int BaseScore { get; set; }
        public int ModifiedScore { get; set; }
        public string Modifiers { get; set; }
        public float Multiplier { get; set; }
        public int BadCuts { get; set; }
        public int MissedNotes { get; set; }
        public int MaxCombo { get; set; }
        public bool FullCombo { get; set; }
        public int HMD { get; set; }
        public string TimeSet { get; set; }
    }

    public struct ApiPageMetadataStruct
    {
        public int Page { get; set; }
        public int MaxPage { get; set; }
        public int CountPerPage { get; set; }
    }

    public struct ApiCustomDataStruct
    {
        public int Level { get; set; }

        [JsonConverter(typeof(DiscordColorConverter))]
        public Color Color { get; set; }

        public string Category { get; set; }
    }
}
