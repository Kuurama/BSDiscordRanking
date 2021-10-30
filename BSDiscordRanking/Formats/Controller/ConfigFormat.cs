using System.Collections.Generic;

namespace BSDiscordRanking.Formats.Controller
{
    public class ConfigFormat
    {
        public string DiscordToken { get; set; } = "";
        public List<string> CommandPrefix { get; set; } = new List<string>();
        public string DiscordStatus { get; set; } = "Made by Kuurama & Julien";

        public ulong BotManagementRoleID { get; set; }
        public bool FullEmbededGGP { get; set; } = true;

        public string RolePrefix { get; set; }

        public string SyncURL { get; set; }

        public List<ulong> AuthorizedChannels { get; set; }

        public bool GiveOldRoles { get; set; } = false;
        public bool automaticWeightCalculation { get; set; } = false;
        public bool perPlaylistWeighting { get; set; } = false;
        public ulong LoggingChannel { get; set; }

        public string PointsName { get; set; } = "PP";
    }
}