using System.Collections.Generic;
using System.IO;
using BSDiscordRanking.Formats;
using Discord;
using Discord.Commands;


namespace BSDiscordRanking.Controllers
{
    public class RoleController
    {
        public RolesFormat m_RoleController = ReadRolesDB();

        private void WriteRolesDB()
        {
            if (!File.Exists("./roles.json"))
            {
                File.WriteAllText($"./roles.json", System.Text.Json.JsonSerializer.Serialize(m_RoleController));
            }
        }

        private static RolesFormat ReadRolesDB()
        {
            if (File.Exists("roles.json"))
            {
                RolesFormat l_RolesFormat = System.Text.Json.JsonSerializer.Deserialize<RolesFormat>(new StreamReader("./roles.json").ReadToEnd());
                if (l_RolesFormat != null)
                {
                    return l_RolesFormat;
                }
            }
            return new RolesFormat() {Roles = new List<RoleFormat>()};
        }
        
        public async void CreateAllRoles(SocketCommandContext p_Context, bool Overwrite)
        {
            ReadRolesDB();
            foreach (var l_LevelID in LevelController.GetLevelControllerCache().LevelID)
            {
                if (!RoleExist($"Lv.{l_LevelID}") || Overwrite)
                {
                    var l_Role = p_Context.Guild.CreateRoleAsync($"Lv.{l_LevelID}", GuildPermissions.None, Color.Green, false, false).Result;
                    m_RoleController.Roles.Add(new RoleFormat(){RoleID = l_Role.Id, RoleName = l_Role.Name, LevelID = l_LevelID});
                }
            }
            if (!RoleExist($"{ConfigController.ReadConfig().RolePrefix} Ranked") || Overwrite)
            {
                var l_Role = p_Context.Guild.CreateRoleAsync($"{ConfigController.ReadConfig().RolePrefix} Ranked", GuildPermissions.None, Color.Blue, false, false).Result;
                await l_Role.ModifyAsync(p_Properties => p_Properties.Position = 0);
                m_RoleController.Roles.Add(new RoleFormat(){RoleID = l_Role.Id, RoleName = l_Role.Name, LevelID = 0});
            }
            WriteRolesDB(); 
        }   

        private bool RoleExist(string p_Name)
        { 
            if (m_RoleController == null) return false;
            foreach (var l_Role in m_RoleController.Roles) 
            {
                if (l_Role.RoleName == p_Name)
                {
                    return true;
                }
            }
            return false;
        }
    }
}