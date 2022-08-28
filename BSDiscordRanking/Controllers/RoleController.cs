using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSDiscordRanking.Formats;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace BSDiscordRanking.Controllers
{
    public class RoleController
    {
        public RolesFormat m_RoleController = ReadRolesDB();

        public void WriteRolesDB()
        {
            try
            {
                File.WriteAllText("./roles.json", JsonConvert.SerializeObject(m_RoleController));
            }
            catch (Exception l_Exception)
            {
                Console.WriteLine($"Error happened upon writing the roles.json: {l_Exception }");
            }
        }

        public static void StaticWriteRolesDB(RolesFormat p_RolesFormat)
        {
            try
            {
                File.WriteAllText("./roles.json", JsonConvert.SerializeObject(p_RolesFormat));
            }
            catch (Exception l_Exception)
            {
                Console.WriteLine($"Error happened upon writing the roles.json: {l_Exception }");
            }
        }

        public static RolesFormat ReadRolesDB()
        {
            if (File.Exists("roles.json"))
            {
                StreamReader l_StreamReader = new StreamReader("./roles.json");
                RolesFormat l_RolesFormat = JsonConvert.DeserializeObject<RolesFormat>(l_StreamReader.ReadToEnd());
                l_StreamReader.Close();
                if (l_RolesFormat != null) return l_RolesFormat;
            }

            return new RolesFormat { Roles = new List<RoleFormat>() };
        }

        public async Task CreateAllRoles(SocketCommandContext p_Context, bool p_Overwrite)
        {
            m_RoleController = ReadRolesDB();
            foreach (int l_LevelID in LevelController.GetLevelControllerCache().LevelID.Where(p_LevelID => p_LevelID is > 0 and < 40))
            {
                RoleFormat l_Role = m_RoleController.Roles.Find(p_X => p_X.LevelID == l_LevelID);
                if (l_Role is null || p_Overwrite)
                {
                    RestRole l_RestRole = p_Context.Guild.CreateRoleAsync($"Lv.{l_LevelID}", GuildPermissions.None, Color.Green, false, false).Result;
                    m_RoleController.Roles.Add(new RoleFormat { RoleID = l_RestRole.Id, RoleName = l_RestRole.Name, LevelID = l_LevelID });
                }
                else
                {
                    /// Update the role color:
                    SocketRole l_SocketRole = p_Context.Guild.GetRole(l_Role.RoleID);
                    if (l_SocketRole is not null)
                    {
                        l_Role.RoleColor = l_SocketRole.Color;
                    }
                }

                Thread.Sleep(60);
            }

            RoleFormat l_RankedRole = m_RoleController.Roles.Find(p_X => p_X.LevelID == 0);
            if (l_RankedRole is null || p_Overwrite)
            {
                RestRole l_Role = p_Context.Guild.CreateRoleAsync($"{ConfigController.GetConfig().RolePrefix} Ranked", GuildPermissions.None, Color.Blue, false, false).Result;
                await l_Role.ModifyAsync(p_Properties => p_Properties.Position = 0);
                m_RoleController.Roles.Add(new RoleFormat { RoleID = l_Role.Id, RoleName = l_Role.Name, LevelID = 0 });
            }
            else
            {
                /// Update the role color:
                SocketRole l_SocketRole = p_Context.Guild.GetRole(l_RankedRole.RoleID);
                if (l_SocketRole is not null)
                {
                    l_RankedRole.RoleColor = l_SocketRole.Color;
                }
            }

            WriteRolesDB();
        }

        /// <summary>
        /// Don't use it's garbage
        /// </summary>
        /// <param name="p_Name"></param>
        /// <returns></returns>
        private bool RoleExist_ByName(string p_Name)
        {
            if (m_RoleController == null) return false;
            foreach (RoleFormat l_Role in m_RoleController.Roles)
                if (l_Role.RoleName == p_Name)
                    return true;

            return false;
        }

        private bool RoleExist_ByLevelID(int p_LevelID)
        {
            if (m_RoleController == null) return false;
            foreach (RoleFormat l_Role in m_RoleController.Roles)
                if (l_Role.LevelID == p_LevelID)
                    return true;

            return false;
        }
    }
}
