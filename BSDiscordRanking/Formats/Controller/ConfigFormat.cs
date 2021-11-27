using System.Collections.Generic;

namespace BSDiscordRanking.Formats.Controller
{
    public class ConfigFormat
    {
        public string DiscordToken { get; set; } = "";
        public List<string> CommandPrefix { get; set; } = new List<string>();
        public string DiscordStatus { get; set; } = "Made by Kuurama & Julien";

        public ulong BotAdminRoleID { get; set; }
        public ulong RankingTeamRoleID { get; set; }
        public bool FullEmbeddedGGP { get; set; } = true;

        public string RolePrefix { get; set; } = "";

        public string SyncURL { get; set; }

        public List<ulong> AuthorizedChannels { get; set; }
        public ulong LoggingChannel { get; set; }
        public ulong AdminPingOnPassChannel { get; set; }

        public bool GiveOldRoles { get; set; } = false;
        public bool EnablePassBasedLeaderboard { get; set; } = true;
        public bool EnableAccBasedLeaderboard { get; set; } = true;
        public string PassPointsName { get; set; } = "PassPoints";
        public string AccPointsName { get; set; } = "AccPoints";
        public float PassPointMultiplier { get; set; } = 1;
        public float AccPointMultiplier { get; set; } = 1;
        public bool AutomaticWeightCalculation { get; set; } = true;
        public int MinimumNumberOfScoreForAutoWeight { get; set; } = 3;

        public bool AllowAutoWeightForAccLeaderboard { get; set; } = true;

        public bool AllowAutoWeightForPassLeaderboard { get; set; } = false;
        
        public bool AllowForceManualWeightForAccLeaderboard { get; set; } = true;

        public bool AllowForceManualWeightForPassLeaderboard { get; set; } = false;
        public bool OnlyAutoWeightForAccLeaderboard { get; set; } = true;

        public bool OnlyAutoWeightForPassLeaderboard { get; set; } = false;
        public bool PerPlaylistWeighting { get; set; } = true;
        
        public int MaximumNumberOfMapInGetInfo { get; set; } = 8;

        public bool DisplayCustomPassTextInGetInfo { get; set; } = false;

        public const string SCORE_SABER_API_VERSION = "3.0.0";
    }
}