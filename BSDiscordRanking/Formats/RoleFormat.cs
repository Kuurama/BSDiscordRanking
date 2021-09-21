using System.Collections.Generic;


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
    }
}