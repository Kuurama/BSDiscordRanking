using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace BSDiscordRanking.Controllers
{
    public class ConfigFormat
    {
        public string DiscordToken { get; set; } = "";
        public string CommandPrefix { get; set; } = "!";
        public string DiscordStatus { get; set; } = "Made by Kuurama & Julien";
        
        public ulong BotManagementRoleID { get; set; }
        public bool BigGGP { get; set; } = false;
        
        public string RolePrefix { get; set; }

        public List<ulong> AuthorizedChannels { get; set; }
        
    }
    
}