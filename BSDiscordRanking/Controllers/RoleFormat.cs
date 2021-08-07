using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Rest;

namespace BSDiscordRanking.Controllers
{
    public class RolesFormat
    {
        public List<RoleFormat> Roles { get; set; }
    }

    public class RoleFormat
    {
        public ulong RoleID { get; set; }
        public string RoleName { get; set; }
    }
}
