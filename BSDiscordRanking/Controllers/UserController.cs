using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace BSDiscordRanking.Controllers
{
    public class UserController
    {
        static List<UserFormat> m_Users = new List<UserFormat>();

        public static bool UserExist(string p_DisID)
        {
            if (string.IsNullOrEmpty(GetPlayer(p_DisID)))
                return false;
            else
                return true;
        }

        public static void AddPlayer(string p_DisID, string p_ScoID)
        {
            m_Users.Add(new UserFormat {DiscordID = p_DisID, ScoreSaberID = p_ScoID});
            Console.WriteLine($"Player {p_DisID} was added with scoresaber: {p_ScoID}");
            GenerateDB();
        }

        public static bool RemovePlayer(string p_DisID)
        {
            foreach (var l_User in m_Users)
            {
                if (p_DisID == l_User.DiscordID)
                {
                    m_Users.Remove(l_User);
                    Console.WriteLine($"Player {l_User.DiscordID} was removed");
                    GenerateDB();
                    return true;
                }
            }

            return false;
        }

        public static void UpdatePlayerLevel(SocketCommandContext p_Context)
        {
            int l_PlayerLevel = new Player(UserController.GetPlayer(p_Context.User.Id.ToString())).GetPlayerLevel();
            ulong l_RoleID = 0;
            foreach (RoleFormat l_Role in new RoleController().m_RoleController.Roles.Where(l_Role => l_Role.LevelID == l_PlayerLevel))
            {
                l_RoleID = l_Role.RoleID;
            }

            if (l_RoleID != 0)
                ((IGuildUser)p_Context.User).AddRoleAsync(p_Context.Guild.Roles.FirstOrDefault(x => x.Id == l_RoleID));
        }
        
        public static void GenerateDB()
        {
            File.WriteAllText("players.json", JsonConvert.SerializeObject(m_Users));
        }

        public static void ReadDB()
        {
            try
            {
                m_Users = JsonConvert.DeserializeObject<List<UserFormat>>(File.ReadAllText("players.json"));
                Console.WriteLine($"Finished to read all database, {m_Users.Count} players found!");
            }
            catch
            {
                GenerateDB();
                ReadDB();
            }
        }

        public static string GetPlayer(string p_DisID)
        {
            foreach (var l_User in m_Users)
            {
                if (p_DisID == l_User.DiscordID)
                {
                    return l_User.ScoreSaberID;
                }
            }

            return null;
        }
    }
}