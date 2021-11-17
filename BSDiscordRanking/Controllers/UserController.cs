﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BSDiscordRanking.Discord.Modules.UserModule;
using BSDiscordRanking.Formats;
using BSDiscordRanking.Formats.API;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
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
                // ReSharper disable once PossibleNullReferenceException
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

        public static async Task<UpdatePlayerRoleFormat> UpdatePlayerLevel(SocketCommandContext p_Context, ulong p_DiscordID, int p_Level)
        {
            if (!UserExist(p_DiscordID.ToString()))
                return new UpdatePlayerRoleFormat()
                {
                    Completed = false,
                    RoleWasAlreadyGiven = false
                };
            SocketGuildUser l_User = p_Context.Guild.GetUser(p_DiscordID);
            if (l_User != null)
            {
                bool l_RolesChanged = false;
                var l_RolesDB = RoleController.ReadRolesDB().Roles;
                var l_Roles = p_Context.Guild.Roles;
                List<ulong> l_MyUserRolesID = new List<ulong>();
                foreach (var l_Role in l_User.Roles)
                {
                    l_MyUserRolesID.Add(l_Role.Id);
                }

                if (GetConfig().GiveOldRoles)
                {
                    foreach (var l_Role in l_RolesDB.Where(p_Role => p_Role.LevelID > p_Level))
                    {
                        if (l_MyUserRolesID.Contains(l_Role.RoleID))
                        {
                            await l_User.RemoveRoleAsync(l_Roles.FirstOrDefault(p_X => p_X.Id == l_Role.RoleID));
                            l_RolesChanged = true;
                        }
                    }

                    foreach (var l_Role in l_RolesDB.Where(p_Role => p_Role.LevelID <= p_Level))
                    {
                        if (!l_MyUserRolesID.Contains(l_Role.RoleID))
                        {
                            await l_User.AddRoleAsync(l_Roles.FirstOrDefault(p_X => p_X.Id == l_Role.RoleID));
                            l_RolesChanged = true;
                        }
                    }
                }
                else
                {
                    foreach (var l_Role in l_RolesDB.Where(p_Role => p_Role.LevelID != p_Level))
                    {
                        if (l_MyUserRolesID.Contains(l_Role.RoleID))
                        {
                            await l_User.RemoveRoleAsync(l_Roles.FirstOrDefault(p_X => p_X.Id == l_Role.RoleID));
                            l_RolesChanged = true;
                        }
                    }

                    foreach (var l_Role in l_RolesDB.Where(p_Role => p_Role.LevelID == p_Level))
                    {
                        if (!l_MyUserRolesID.Contains(l_Role.RoleID))
                        {
                            await l_User.AddRoleAsync(l_Roles.FirstOrDefault(p_X => p_X.Id == l_Role.RoleID));
                            l_RolesChanged = true;
                        }
                    }
                }

                if (l_RolesChanged)
                    return new UpdatePlayerRoleFormat()
                    {
                        Completed = true,
                        RoleWasAlreadyGiven = false
                    };
                //await p_Context.Channel.SendMessageAsync($"> :ok_hand: <@{l_User.Id.ToString()}>, Your roles are now updated.\n(if you lost levels, Please consider grinding to keep your Level).");
                else
                    return new UpdatePlayerRoleFormat()
                    {
                        Completed = true,
                        RoleWasAlreadyGiven = true
                    };
                //await p_Context.Channel.SendMessageAsync($"> :x: This player already had all his roles.");
            }
            else
            {
                return new UpdatePlayerRoleFormat()
                {
                    Completed = false,
                    RoleWasAlreadyGiven = false
                };
                //Console.WriteLine("Can't find this user, makes him type a message straight before using the command!");
            }
        }

        public static async Task UpdateRoleAndSendMessage(SocketCommandContext p_Context, ulong p_UserID, int p_NewPlayerLevel)
        {
            Task<UpdatePlayerRoleFormat> l_RoleUpdate = UpdatePlayerLevel(p_Context, p_UserID, p_NewPlayerLevel);

            EmbedBuilder l_EmbedBuilder = new EmbedBuilder();
            Color l_Color = UserModule.GetRoleColor(RoleController.ReadRolesDB().Roles, p_Context.Guild.Roles, p_NewPlayerLevel);
            l_EmbedBuilder.WithColor(l_Color);
            if (l_RoleUpdate.Result.Completed && l_RoleUpdate.Result.RoleWasAlreadyGiven)
            {
                l_EmbedBuilder.WithDescription($"> :x: This player already had all their roles.");
            }
            else if (l_RoleUpdate.Result.Completed && !l_RoleUpdate.Result.RoleWasAlreadyGiven)
            {
                l_EmbedBuilder.WithDescription($"> :ok_hand: Your roles are now updated.\n(if you lost levels, Please consider grinding to keep your Level).");
            }
            else
            {
                l_EmbedBuilder.WithDescription("Can't find this user, makes him type a message straight before using the command!");
            }

            await p_Context.Channel.SendMessageAsync($"<@{p_UserID}>", embed: l_EmbedBuilder.Build());
        }

        public class UpdatePlayerRoleFormat
        {
            public bool Completed { get; set; }
            public bool RoleWasAlreadyGiven { get; set; }
        }

        public static bool GiveRemoveBSDRRole(ulong p_DiscordID, SocketCommandContext p_Context, bool p_Remove)
        {
            var l_User = p_Context.Guild.GetUser(p_DiscordID);
            if (l_User != null)
            {
                foreach (var l_Role in RoleController.ReadRolesDB().Roles)
                {
                    foreach (var l_GuildRole in p_Context.Guild.Roles)
                    {
                        if (string.Equals(l_Role.RoleName, $"{GetConfig().RolePrefix} Ranked", StringComparison.OrdinalIgnoreCase))
                        {
                            if (string.Equals(l_Role.RoleName, l_GuildRole.Name, StringComparison.OrdinalIgnoreCase))
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

                                return true;
                            }
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("Can't find this user, make him type a message straight before using the command!");
                return false;
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

        public static string GetDiscordID(string p_ScoreSaberID)
        {
            foreach (var l_User in m_Users)
            {
                if (p_ScoreSaberID == l_User.ScoreSaberID)
                {
                    return l_User.DiscordID;
                }
            }

            return null;
        }
    }
}