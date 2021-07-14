using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace BSDiscordRanking.Controllers
{
    public class UserController
    {
        static List<UserFormat> m_Users = new List<UserFormat>();

        public static bool UserExist(string p_disID)
        {
            if (string.IsNullOrEmpty(GetPlayer(p_disID)))
                return false;
            else
                return true;
        }

        public static void AddPlayer(string p_disID, string p_scoID)
        {
            m_Users.Add(new UserFormat{DiscordID = p_disID, ScoreSaberID = p_scoID});
            Console.WriteLine($"Player {p_disID} was added with scoresaber: {p_scoID}");
            GenerateDB();
        }

        public static bool RemovePlayer(string p_disID)
        {
            foreach (var l_user in m_Users)
            {
                if (p_disID == l_user.DiscordID)
                {
                    m_Users.Remove(l_user);
                    Console.WriteLine($"Player {l_user.DiscordID} was removed");
                    GenerateDB();
                    return true;
                }
            }
            return false;
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

        public static string GetPlayer(string p_disID)
        {
            foreach (var l_user in m_Users)
            {
                if (p_disID == l_user.DiscordID)
                {
                    return l_user.ScoreSaberID;
                }
            }
            return null;
        }
    }
}