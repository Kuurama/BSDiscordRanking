using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BSDiscordRanking.Formats;
using Discord;
using Discord.Commands;
using Discord.Rest;

namespace BSDiscordRanking.Controllers
{
    public class RoleController
    {
        public RolesFormat m_RoleController = ReadRolesDB();

        private void WriteRolesDB()
        {
            File.WriteAllText($"./roles.json", JsonSerializer.Serialize(m_RoleController));
        }

        public static RolesFormat ReadRolesDB()
        {
            if (File.Exists("roles.json"))
            {
                RolesFormat l_RolesFormat = JsonSerializer.Deserialize<RolesFormat>(new StreamReader("./roles.json").ReadToEnd());
                if (l_RolesFormat != null)
                {
                    return l_RolesFormat;
                }
            }

            return new RolesFormat { Roles = new List<RoleFormat>() };
        }

        public async Task CreateAllRoles(SocketCommandContext p_Context, bool p_Overwrite)
        {
            ReadRolesDB();
            foreach (int l_LevelID in LevelController.GetLevelControllerCache().LevelID.Where(p_LevelID => p_LevelID > 0))
            {
                if (!RoleExist($"Lv.{l_LevelID}") || p_Overwrite)
                {
                    RestRole l_Role = p_Context.Guild.CreateRoleAsync($"Lv.{l_LevelID}", GuildPermissions.None, Color.Green, false, false).Result;
                    m_RoleController.Roles.Add(new RoleFormat { RoleID = l_Role.Id, RoleName = l_Role.Name, LevelID = l_LevelID });
                }

                Thread.Sleep(60);
            }

            if (!RoleExist($"{ConfigController.GetConfig().RolePrefix} Ranked") || p_Overwrite)
            {
                var l_Role = p_Context.Guild.CreateRoleAsync($"{ConfigController.GetConfig().RolePrefix} Ranked", GuildPermissions.None, Color.Blue, false, false).Result;
                await l_Role.ModifyAsync(p_Properties => p_Properties.Position = 0);
                m_RoleController.Roles.Add(new RoleFormat() { RoleID = l_Role.Id, RoleName = l_Role.Name, LevelID = 0 });
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