using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using BSDiscordRanking.Formats;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.VisualBasic.CompilerServices;
using Newtonsoft.Json;
using static BSDiscordRanking.Controllers.ConfigController;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace BSDiscordRanking.Controllers
{
    public class UserController
    {
        static List<UserFormat> m_Users = new List<UserFormat>();

        public static bool AccountExist(string p_ScoreSaberID)
        {
            try
            {
                WebClient l_WebClient = new WebClient();
                var l_PlayerFull = JsonSerializer.Deserialize<ApiPlayerFull>
                    (l_WebClient.DownloadString(@$"https://new.scoresaber.com/api/player/{p_ScoreSaberID}/full"));
                if (!string.IsNullOrEmpty(l_PlayerFull.playerInfo.playerName))
                {
                    return true;
                }
            }
            catch
            {
                // ignored
            }

            return false;
        }

        public static bool SSIsAlreadyLinked(string p_ScoreSaberID)
        {
            foreach (var l_User in m_Users)
            {
                if (p_ScoreSaberID == l_User.ScoreSaberID)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool UserExist(string p_DisID)
        {
            if (string.IsNullOrEmpty(GetPlayer(p_DisID)))
                return false;
            else
                return true;
        }

        public static void AddPlayer(string p_DisID, string p_ScoID)
        {
            m_Users.Add(new UserFormat { DiscordID = p_DisID, ScoreSaberID = p_ScoID });
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

        public static async Task UpdatePlayerLevel(SocketCommandContext p_Context)
        {
            if (!UserExist(p_Context.User.Id.ToString())) return;
            int l_PlayerLevel = new Player(GetPlayer(p_Context.User.Id.ToString())).GetPlayerLevel();
            foreach (RoleFormat l_Role in RoleController.ReadRolesDB().Roles)
            {
                if (l_Role.LevelID == l_PlayerLevel)
                    await ((IGuildUser)p_Context.User).AddRoleAsync(p_Context.Guild.Roles.FirstOrDefault(p_X => p_X.Id == l_Role.RoleID));
                else if (l_Role.LevelID < l_PlayerLevel && GetConfig().GiveOldRoles)
                    await ((IGuildUser)p_Context.User).AddRoleAsync(p_Context.Guild.Roles.FirstOrDefault(p_X => p_X.Id == l_Role.RoleID));
                Thread.Sleep(60); // Discord API limit
            }

            SocketGuildUser l_User = p_Context.User as SocketGuildUser;
            foreach (SocketRole l_UserRole in l_User.Roles)
            {
                foreach (RoleFormat l_Role in RoleController.ReadRolesDB().Roles)
                {
                    if (l_UserRole.Id == l_Role.RoleID && l_Role.LevelID != 0 && l_Role.LevelID > l_PlayerLevel)
                        await ((IGuildUser)p_Context.User).RemoveRoleAsync(p_Context.Guild.Roles.FirstOrDefault(p_X => p_X.Id == l_Role.RoleID));
                    else if (l_UserRole.Id == l_Role.RoleID && l_Role.LevelID != 0 && l_Role.LevelID != l_PlayerLevel && !GetConfig().GiveOldRoles)
                        await ((IGuildUser)p_Context.User).RemoveRoleAsync(p_Context.Guild.Roles.FirstOrDefault(p_X => p_X.Id == l_Role.RoleID));
                    Thread.Sleep(60); // Discord API limit
                }
            }

            await p_Context.Channel.SendMessageAsync($"> :ok_hand: Your roles are now updated.\n(if you loosed levels, you can still do the w1RR command on the other bscc bot to get them all back)");
        }

        public static bool GiveRemoveBSDRRole(ulong p_DiscordID, SocketCommandContext p_Context, bool p_Remove)
        {
            foreach (var l_Role in RoleController.ReadRolesDB().Roles)
            {
                foreach (var l_GuildRole in p_Context.Guild.Roles)
                {
                    if (string.Equals(l_Role.RoleName, $"{GetConfig().RolePrefix} Ranked", StringComparison.OrdinalIgnoreCase))
                    {
                        if (string.Equals(l_Role.RoleName, l_GuildRole.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            var l_User = p_Context.Guild.GetUser(p_DiscordID);
                            if (l_User != null)
                            {
                                foreach (var l_UserRole in l_User.Roles)
                                {
                                    if (l_UserRole.Id == l_Role.RoleID)
                                    {
                                        if (p_Remove)
                                            l_User.RemoveRoleAsync(l_GuildRole);
                                        else
                                            return false;
                                    }
                                }

                                if (!p_Remove)
                                    l_User.AddRoleAsync(l_GuildRole);
                            }
                            else
                            {
                                Console.WriteLine("Can't find this user, make him type a message straight before using the command!");
                                return false;
                            }

                            return true;
                        }
                    }
                }
            }

            return false;
        }


        private static void GenerateDB()
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