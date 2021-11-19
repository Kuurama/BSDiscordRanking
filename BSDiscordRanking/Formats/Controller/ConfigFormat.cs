using System.Collections.Generic;

namespace BSDiscordRanking.Formats.Controller
{
    public class ConfigFormat
    {
        public string DiscordToken { get; set; } = "";
        public List<string> CommandPrefix { get; set; } = new List<string>();
        public string DiscordStatus { get; set; } = "Made by Kuurama & Julien";

        public ulong BotManagementRoleID { get; set; }
        public bool FullEmbeddedGGP { get; set; } = true;

        public string RolePrefix { get; set; } = "";

        public string SyncURL { get; set; }

        public List<ulong> AuthorizedChannels { get; set; }

        public bool GiveOldRoles { get; set; } = false;
        public bool EnablePassBasedLeaderboard { get; set; } = true;
        public bool EnableAccBasedLeaderboard { get; set; } = true;
        public bool AutomaticWeightCalculation { get; set; } = true;
        public int MinimumNumberOfScoreForAutoWeight { get; set; } = 3;

        public bool AllowAutoWeightForAccLeaderboard { get; set; } = true;

        public bool AllowAutoWeightForPassLeaderboard { get; set; } = false;

        public bool OnlyAutoWeightForAccLeaderboard { get; set; } = true;

        public bool OnlyAutoWeightForPassLeaderboard { get; set; } = false;
        public bool PerPlaylistWeighting { get; set; } = true;
        public ulong LoggingChannel { get; set; }

        public string PassPointsName { get; set; } = "PassPoints";
        public string AccPointsName { get; set; } = "AccPoints";
        public int MaximumNumberOfMapInGetInfo { get; set; } = 8;
        public ulong BotEditorRoleID { get; set; }
    }
}