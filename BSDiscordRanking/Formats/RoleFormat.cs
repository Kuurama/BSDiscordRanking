using System.Collections.Generic;
using BSDiscordRanking.Utils;
using Newtonsoft.Json;
using Color = Discord.Color;

namespace BSDiscordRanking.Formats
{
    public class RolesFormat
    {
        public List<RoleFormat> Roles { get; set; }
    }

    public class RoleFormat
    {
        public ulong RoleID { get; set; }
        public string RoleName { get; set; }
        public int LevelID { get; set; }
        
        [JsonConverter(typeof(DiscordColorConverter))]
        public Color RoleColor { get; set; }
    }
}