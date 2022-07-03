using System;
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
        public static List<UserFormat> m_Users = new List<UserFormat>();

        public static bool AccountExist(string p_ScoreSaberID, out ApiPlayer p_PlayerFull)
        {
            try
            {
                WebClient l_WebClient = new WebClient();
                ApiPlayer l_PlayerFull = JsonSerializer.Deserialize<ApiPlayer>
                    (l_WebClient.DownloadString(@$"https://scoresaber.com/api/player/{p_ScoreSaberID}/full"));
                // ReSharper disable once PossibleNullReferenceException
                if (l_PlayerFull != null && !string.IsNullOrEmpty(l_PlayerFull.name))
                {
                    p_PlayerFull = l_PlayerFull;
                    return true;
                }
            }
            catch
            {
                // ignored
            }

            p_PlayerFull = null;
            return false;
        }

        public static bool SSIsAlreadyLinked(string p_ScoreSaberID)
        {
            foreach (UserFormat l_User in m_Users)
                if (p_ScoreSaberID == l_User.ScoreSaberID)
                    return true;

            return false;
        }

        public static bool UserExist(string p_DisID)
        {
            if (string.IsNullOrEmpty(GetPlayer(p_DisID)))
                return false;
            return true;
        }

        public static void AddPlayer(string p_DisID, string p_ScoID, ELinkState p_LinkState = ELinkState.None)
        {
            m_Users.Add(new UserFormat { DiscordID = p_DisID, ScoreSaberID = p_ScoID, PendingScoreSaberID = String.Empty, LinkState = p_LinkState });
            Console.WriteLine($"Player {p_DisID} was added with ScoreSaberID: {p_ScoID}");
            GenerateDB();
        }

        public static ELinkState GetLinkState(string p_DisID)
        {
            return m_Users.Find(p_X => p_X.DiscordID == p_DisID)?.LinkState ?? ELinkState.None;
        }

        public static void AddLinkBannedPlayer(string p_DisID)
        {
            m_Users.Add(new UserFormat { DiscordID = p_DisID, ScoreSaberID = String.Empty, PendingScoreSaberID = String.Empty, LinkState = ELinkState.Banned });
            Console.WriteLine($"DiscordID {p_DisID} was link banned.");
            GenerateDB();
        }

        public static bool AcceptVerifyPlayer(string p_DisID)
        {
            UserFormat l_User = m_Users.Find(p_X => p_X.DiscordID == p_DisID);
            if (l_User == null)
                return false;

            l_User.ScoreSaberID = l_User.PendingScoreSaberID;
            l_User.PendingScoreSaberID = null;
            l_User.LinkState = ELinkState.Verified;
            Console.WriteLine($"Player {p_DisID} was verified with ScoreSaberID: {l_User.ScoreSaberID}");
            GenerateDB();
            return true;
        }


        public static bool DenyVerifyPlayer(string p_DisID)
        {
            UserFormat l_User = m_Users.Find(p_X => p_X.DiscordID == p_DisID);
            if (l_User == null)
                return false;

            l_User.PendingScoreSaberID = null;
            l_User.LinkState = ELinkState.Denied;
            Console.WriteLine($"Player {p_DisID} was denied with ScoreSaberID: {l_User.PendingScoreSaberID}");
            GenerateDB();
            return true;
        }

        public static bool BanVerifyPlayer(string p_DisID)
        {
            UserFormat l_User = m_Users.Find(p_X => p_X.DiscordID == p_DisID);
            if (l_User == null)
                return false;

            l_User.PendingScoreSaberID = null;
            l_User.LinkState = ELinkState.Banned;
            Console.WriteLine($"Player {p_DisID} was link banned with ScoreSaberID: {l_User.PendingScoreSaberID}");
            GenerateDB();
            return true;
        }

        public static bool IsLinkDenied(string p_DisID)
        {
            UserFormat l_User = m_Users.Find(p_X => p_X.DiscordID == p_DisID);
            if (l_User == null)
                return false;

            return l_User.LinkState == ELinkState.Denied;
        }

        public static bool IsLinkBanned(string p_DisID)
        {
            UserFormat l_User = m_Users.Find(p_X => p_X.DiscordID == p_DisID);
            if (l_User == null)
                return false;

            return l_User.LinkState == ELinkState.Banned;
        }

        public static string GetPendingScoreSaberID(string p_DiscordID)
        {
            foreach (UserFormat l_User in m_Users)
                if (l_User.DiscordID == p_DiscordID)
                    return l_User.PendingScoreSaberID;

            return String.Empty;
        }

        public static void AddPlayerNeedVerification(string p_DisID, string p_ScoID)
        {
            m_Users.Add(new UserFormat { DiscordID = p_DisID, ScoreSaberID = String.Empty, PendingScoreSaberID = p_ScoID, LinkState = ELinkState.Unverified });
            Console.WriteLine($"Player {p_DisID} was added to verification with ScoreSaberID: {p_ScoID}");
            GenerateDB();
        }

        public static bool RemovePlayer(string p_DisID)
        {
            int l_Count = m_Users.Count;
            m_Users.RemoveAll(p_X => p_X.DiscordID == p_DisID);
            GenerateDB();
            return l_Count != m_Users.Count;
        }

        public static async Task<UpdatePlayerRoleFormat> UpdatePlayerLevelRole(SocketCommandContext p_Context, ulong p_DiscordID, int p_Level)
        {
            if (!UserExist(p_DiscordID.ToString()))
                return new UpdatePlayerRoleFormat
                {
                    Completed = false,
                    RoleWasAlreadyGiven = false
                };
            SocketGuildUser l_User = p_Context.Guild.GetUser(p_DiscordID);
            if (l_User != null)
            {
                bool l_RolesChanged = false;
                List<RoleFormat> l_RolesDB = RoleController.ReadRolesDB().Roles;
                IReadOnlyCollection<SocketRole> l_GuildRoles = p_Context.Guild.Roles;
                List<SocketRole> l_GuildRolesList = l_GuildRoles.ToList();
                List<ulong> l_MyUserRolesID = new List<ulong>();
                foreach (SocketRole l_Role in l_User.Roles) l_MyUserRolesID.Add(l_Role.Id);

                if (GetConfig().GiveOldRoles)
                {
                    List<IRole> l_RoleToRemove = new List<IRole>();
                    List<IRole> l_RoleToAdd = new List<IRole>();
                    foreach (RoleFormat l_Role in l_RolesDB.Where(p_Role => p_Role.LevelID > p_Level))
                        if (l_MyUserRolesID.Contains(l_Role.RoleID)) // Also mean the guild role exist as he haves it.
                        {
                            IRole l_GuildRole = l_GuildRolesList.Find(p_Role => p_Role.Id == l_Role.RoleID);
                            l_RoleToRemove.Add(l_GuildRole);
                            //await l_User.RemoveRoleAsync(l_GuildRoles.FirstOrDefault(p_X => p_X.Id == l_Role.RoleID));
                        }

                    foreach (RoleFormat l_Role in l_RolesDB.Where(p_Role => p_Role.LevelID <= p_Level))
                        if (!l_MyUserRolesID.Contains(l_Role.RoleID) && l_GuildRolesList.FindIndex(p_X => p_X.Id == l_Role.RoleID) >= 0) // Only gives a roles the guild have ofc.
                        {
                            IRole l_GuildRole = l_GuildRolesList.Find(p_Role => p_Role.Id == l_Role.RoleID);
                            l_RoleToAdd.Add(l_GuildRole);
                            //await l_User.AddRoleAsync(l_GuildRoles.FirstOrDefault(p_X => p_X.Id == l_Role.RoleID));
                        }

                    if (l_RoleToAdd.Count > 0)
                    {
                        await l_User.AddRolesAsync(l_RoleToAdd);
                        l_RolesChanged = true;
                    }
                    if (l_RoleToRemove.Count > 0)
                    {
                        await l_User.RemoveRolesAsync(l_RoleToRemove);
                        l_RolesChanged = true;
                    }
                }
                else
                {
                    List<IRole> l_RoleToRemove = new List<IRole>();
                    List<IRole> l_RoleToAdd = new List<IRole>();
                    foreach (RoleFormat l_Role in l_RolesDB.Where(p_Role => p_Role.LevelID != p_Level))
                        if (l_MyUserRolesID.Contains(l_Role.RoleID)) // Also mean the guild role exist as he haves it.
                        {
                            IRole l_GuildRole = l_GuildRolesList.Find(p_Role => p_Role.Id == l_Role.RoleID);
                            l_RoleToRemove.Add(l_GuildRole);
                            //await l_User.RemoveRoleAsync(l_GuildRoles.FirstOrDefault(p_X => p_X.Id == l_Role.RoleID));
                        }

                    foreach (RoleFormat l_Role in l_RolesDB.Where(p_Role => p_Role.LevelID == p_Level))
                        if (!l_MyUserRolesID.Contains(l_Role.RoleID))
                            if (!l_MyUserRolesID.Contains(l_Role.RoleID) && l_GuildRolesList.FindIndex(p_X => p_X.Id == l_Role.RoleID) >= 0) // Only gives a roles the guild have ofc.
                            {
                                IRole l_GuildRole = l_GuildRolesList.Find(p_Role => p_Role.Id == l_Role.RoleID);
                                l_RoleToAdd.Add(l_GuildRole);
                                //await l_User.AddRoleAsync(l_GuildRoles.FirstOrDefault(p_X => p_X.Id == l_Role.RoleID));
                            }
                    if (l_RoleToAdd.Count > 0)
                    {
                        await l_User.AddRolesAsync(l_RoleToAdd);
                        l_RolesChanged = true;
                    }
                    if (l_RoleToRemove.Count > 0)
                    {
                        await l_User.RemoveRolesAsync(l_RoleToRemove);
                        l_RolesChanged = true;
                    }
                }

                if (l_RolesChanged)
                    return new UpdatePlayerRoleFormat
                    {
                        Completed = true,
                        RoleWasAlreadyGiven = false
                    };
                //await p_Context.Channel.SendMessageAsync($"> :ok_hand: <@{l_User.Id.ToString()}>, Your roles are now updated.\n(if you lost levels, Please consider grinding to keep your Level).");
                return new UpdatePlayerRoleFormat
                {
                    Completed = true,
                    RoleWasAlreadyGiven = true
                };
                //await p_Context.Channel.SendMessageAsync($"> :x: This player already had all his roles.");
            }

            return new UpdatePlayerRoleFormat
            {
                Completed = false,
                RoleWasAlreadyGiven = false
            };
            //Console.WriteLine("Can't find this user, makes him type a message straight before using the command!");
        }

        public static async Task UpdateRoleAndSendMessage(SocketCommandContext p_Context, ulong p_UserID, int p_NewPlayerLevel)
        {
            Task<UpdatePlayerRoleFormat> l_RoleUpdate = UpdatePlayerLevelRole(p_Context, p_UserID, p_NewPlayerLevel);

            EmbedBuilder l_EmbedBuilder = new EmbedBuilder();
            Color l_Color = UserModule.GetRoleColor(RoleController.ReadRolesDB().Roles, p_Context.Guild.Roles, p_NewPlayerLevel);
            l_EmbedBuilder.WithColor(l_Color);
            if (l_RoleUpdate.Result.Completed && l_RoleUpdate.Result.RoleWasAlreadyGiven)
                l_EmbedBuilder.WithDescription("> :x: This player already had all their roles.");
            else if (l_RoleUpdate.Result.Completed && !l_RoleUpdate.Result.RoleWasAlreadyGiven)
                l_EmbedBuilder.WithDescription("> :ok_hand: Your roles are now updated.\n(if you lost levels, Please consider grinding to keep your Level).");
            else
                l_EmbedBuilder.WithDescription("Can't find this user, makes him type a message straight before using the command!");

            await p_Context.Channel.SendMessageAsync($"<@{p_UserID}>", embed: l_EmbedBuilder.Build());
        }

        public static bool GiveRemoveBSDRRole(ulong p_DiscordID, SocketCommandContext p_Context, bool p_Remove)
        {
            SocketGuildUser l_User = p_Context.Guild.GetUser(p_DiscordID);
            if (l_User != null)
            {
                List<RoleFormat> l_RolesFormat = RoleController.ReadRolesDB().Roles;

                foreach (RoleFormat l_Role in l_RolesFormat)
                    if (l_Role.LevelID == 0)
                        foreach (SocketRole l_GuildRole in p_Context.Guild.Roles)
                            if (l_Role.RoleID == l_GuildRole.Id)
                            {
                                foreach (SocketRole l_UserRole in l_User.Roles)
                                    if (l_UserRole.Id == l_Role.RoleID)
                                    {
                                        if (p_Remove)
                                            l_User.RemoveRoleAsync(l_GuildRole);
                                        else
                                            return false;
                                    }

                                if (!p_Remove)
                                    l_User.AddRoleAsync(l_GuildRole);

                                return true;
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
            return (from l_User in m_Users where p_DisID == l_User.DiscordID select l_User.ScoreSaberID).FirstOrDefault();
        }

        public static string GetDiscordID(string p_ScoreSaberID)
        {
            foreach (UserFormat l_User in m_Users)
                if (p_ScoreSaberID == l_User.ScoreSaberID)
                    return l_User.DiscordID;

            return null;
        }

        public class UpdatePlayerRoleFormat
        {
            public bool Completed { get; set; }
            public bool RoleWasAlreadyGiven { get; set; }
        }

        public static bool IsPendingVerification(string p_DisID)
        {
            return (from l_User in m_Users where p_DisID == l_User.DiscordID select l_User.LinkState == ELinkState.Unverified).FirstOrDefault();
        }
    }
}
