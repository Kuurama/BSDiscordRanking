using System.Collections.Generic;

namespace BSDiscordRanking.Formats
{
    public class ConfigFormat
    {
        public string DiscordToken { get; set; } = "";
        public List<string> CommandPrefix { get; set; } = new List<string>();
        public string DiscordStatus { get; set; } = "Made by Kuurama & Julien";
        
        public ulong BotManagementRoleID { get; set; }
        public bool BigGGP { get; set; } = false;
        
        public string RolePrefix { get; set; }

        public List<ulong> AuthorizedChannels { get; set; }

        public bool GiveOldRoles { get; set; } = false;

    }
    
}