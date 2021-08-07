using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Rest;


namespace BSDiscordRanking.Controllers
{
    public class RoleController
    {
        private RoleFormat m_RoleController = ReadRolesDB();

        private void WriteRolesDB()
        {
            if (!File.Exists("./roles.json"))
            {
                File.WriteAllText($"./roles.json", System.Text.Json.JsonSerializer.Serialize(m_RoleController));
            }
        }

        private static RoleFormat ReadRolesDB()
        {
            if (File.Exists("roles.json"))
            {
                RoleFormat l_RoleFormat = System.Text.Json.JsonSerializer.Deserialize<RoleFormat>(new StreamReader("./roles.json").ReadToEnd());
                if (l_RoleFormat != null)
                {
                    return l_RoleFormat;
                }
            }
            return new RoleFormat();
        }
        
        public void CreateAllRoles(SocketCommandContext p_Context)
        {
            if (!RoleExist($"{ConfigController.ReadConfig().RolePrefix} Ranked"))
                m_RoleController.RestRoles.Add(p_Context.Guild.CreateRoleAsync($"{ConfigController.ReadConfig().RolePrefix} Ranked", GuildPermissions.None, Color.Blue, false, false).Result);
            foreach (var l_LevelID in LevelController.GetLevelControllerCache().LevelID)
            {
                if (!RoleExist($"Lv.{l_LevelID}"))
                    m_RoleController.RestRoles.Add(p_Context.Guild.CreateRoleAsync($"Lv.{l_LevelID}", GuildPermissions.None, Color.Green, false, false).Result);
            }
            WriteRolesDB();
        }

        private bool RoleExist(string p_Name)
        {
            Console.WriteLine("I'm working (48)");
            if (m_RoleController == null) return false;
            foreach (var l_Role in m_RoleController.RestRoles) 
            {
                Console.WriteLine("I'm working (51)");
                if (l_Role.Name == p_Name)
                {
                    return true;
                }
            }
            return false;
        }
    }
}