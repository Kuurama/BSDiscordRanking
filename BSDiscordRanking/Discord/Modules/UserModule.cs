using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using BSDiscordRanking.Formats;
using BSDiscordRanking.Formats.Controller;
using BSDiscordRanking.Formats.Level;
using BSDiscordRanking.Formats.Player;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

// ReSharper disable once CheckNamespace
namespace BSDiscordRanking.Discord.Modules.UserModule
{
    [CheckChannel]
    public partial class UserModule : ModuleBase<SocketCommandContext>
    {
        private void CreateUnpassedPlaylist(string p_ScoreSaberID, int p_Level, string p_Path)
        {
            PlayerPassFormat l_PlayerPass = new Player(p_ScoreSaberID).ReturnPass();
            Level l_Level = new Level(p_Level);
            LevelFormat l_LevelFormat = l_Level.GetLevelData();
            if (l_LevelFormat.songs.Count > 0)
            {
                foreach (var l_PlayerPassSong in l_PlayerPass.SongList)
                {
                    for (int l_I = l_LevelFormat.songs.Count - 1; l_I >= 0; l_I--)
                    {
                        if (l_LevelFormat.songs.Count > l_I)
                        {
                            if (String.Equals(l_LevelFormat.songs[l_I].hash, l_PlayerPassSong.hash,
                                StringComparison.CurrentCultureIgnoreCase))
                            {
                                foreach (var l_PlayerPassSongDifficulty in l_PlayerPassSong.DiffList)
                                {
                                    if (l_LevelFormat.songs.Count > l_I)
                                    {
                                        for (int l_Y = l_LevelFormat.songs[l_I].difficulties.Count - 1; l_Y >= 0; l_Y--)
                                        {
                                            if (l_LevelFormat.songs[l_I].difficulties.Count > 0 &&
                                                l_LevelFormat.songs.Count > 0)
                                            {
                                                if (l_LevelFormat.songs[l_I].difficulties[l_Y].characteristic ==
                                                    l_PlayerPassSongDifficulty.Difficulty.characteristic &&
                                                    l_LevelFormat.songs[l_I].difficulties[l_Y].name ==
                                                    l_PlayerPassSongDifficulty.Difficulty.name)
                                                {
                                                    /// Here remove diff or map if it's the only ranked diff
                                                    if (l_LevelFormat.songs[l_I].difficulties.Count <= 1)
                                                    {
                                                        l_LevelFormat.songs.Remove(l_LevelFormat.songs[l_I]);
                                                    }
                                                    else
                                                    {
                                                        l_LevelFormat.songs[l_I].difficulties
                                                            .Remove(l_LevelFormat.songs[l_I].difficulties[l_Y]);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                l_LevelFormat.customData.syncURL = null;
            }

            if (l_LevelFormat.songs.Count <= 0)
                return; /// Do not create the file if it's empty.
            JsonDataBaseController.CreateDirectory(p_Path);
            l_Level.ReWritePlaylist(true, p_Path, l_LevelFormat); /// Write the personal playlist file in the PATH folder.
        }

        private static string RemoveSpecialCharacters(string p_Str)
        {
            StringBuilder l_SB = new StringBuilder();
            foreach (char l_C in p_Str)
            {
                if (l_C is >= '0' and <= '9' or >= 'A' and <= 'Z' or >= 'a' and <= 'z' or '_')
                {
                    l_SB.Append(l_C);
                }
            }

            return l_SB.ToString();
        }

        private void DeleteUnpassedPlaylist(string p_OriginalPath, string p_FileName)
        {
            ///// Delete all personal files before generating new ones /////////
            string l_Path = p_OriginalPath + p_FileName + "/";
            if (File.Exists(p_OriginalPath + p_FileName + ".zip"))
                File.Delete(p_OriginalPath + p_FileName + ".zip");

            DirectoryInfo l_Directory = new DirectoryInfo(l_Path);
            foreach (FileInfo l_File in l_Directory.EnumerateFiles())
            {
                l_File.Delete();
            }

            foreach (DirectoryInfo l_Dir in l_Directory.EnumerateDirectories())
            {
                l_Dir.Delete(true);
            }

            Directory.Delete(p_OriginalPath + p_FileName + "/");
            ///////////////////////////////////////////////////////////////////////
        }


        private async Task SendProfile(string p_DiscordOrScoreSaberID, bool p_IsSomeoneElse)
        {
            bool l_IsScoreSaberAccount = UserController.AccountExist(p_DiscordOrScoreSaberID);

            if (UserController.UserExist(p_DiscordOrScoreSaberID))
            {
                p_DiscordOrScoreSaberID = UserController.GetPlayer(p_DiscordOrScoreSaberID);
            }
            else if (l_IsScoreSaberAccount)
            {
                if (!UserController.SSIsAlreadyLinked(p_DiscordOrScoreSaberID))
                {
                    bool l_IsAdmin = false;
                    if (Context.User is SocketGuildUser l_User)
                        if (l_User.Roles.Any(p_Role => p_Role.Id == ConfigController.GetConfig().BotManagementRoleID))
                            l_IsAdmin = true;
                    if (!l_IsAdmin)
                    {
                        await ReplyAsync("> Sorry, This Score Saber account isn't registered on the bot.");
                        return;
                    }
                }
            }
            else if (!UserController.UserExist(p_DiscordOrScoreSaberID))
            {
                await ReplyAsync(p_IsSomeoneElse
                    ? "> :x: Sorry, this Discord User doesn't have any ScoreSaber account linked/isn't a correct ScoreSaberID."
                    : $"> :x: Sorry, you doesn't have any account linked. Please use `{BotHandler.m_Prefix}link <ScoreSaber link/id>` instead.\n> (Or to get started with the bot: use the `{BotHandler.m_Prefix}getstarted` command)");
                return;
            }

            Player l_Player = new Player(p_DiscordOrScoreSaberID);
            int l_PlayerLevel = l_Player.GetPlayerLevel();
            var l_PlayerStats = l_Player.GetStats();

            int l_Plastics = 0;
            int l_Silvers = 0;
            int l_Golds = 0;

            int l_Diamonds = 0;
            if (l_Player.m_PlayerStats.Levels is not null)
            {
                foreach (var l_PlayerStatsLevel in l_Player.m_PlayerStats.Levels)
                {
                    l_PlayerStatsLevel.Trophy ??= new Trophy();
                    l_Plastics += l_PlayerStatsLevel.Trophy.Plastic;
                    l_Silvers += l_PlayerStatsLevel.Trophy.Silver;
                    l_Golds += l_PlayerStatsLevel.Trophy.Gold;
                    l_Diamonds += l_PlayerStatsLevel.Trophy.Diamond;
                }
            }

            ConfigFormat l_Config = ConfigController.GetConfig();

            int l_PassFindIndex = -1;
            PassLeaderboardController l_PassLeaderboardController = null;
            if (l_Config.EnableAccBasedLeaderboard)
            {
                l_PassLeaderboardController = new PassLeaderboardController();
                l_PassFindIndex = l_PassLeaderboardController.m_Leaderboard.Leaderboard.FindIndex(p_X => p_X.ScoreSaberID == p_DiscordOrScoreSaberID);
            }

            int l_AccFindIndex = -1;
            AccLeaderboardController l_AccLeaderboardController = null;
            if (l_Config.EnableAccBasedLeaderboard)
            {
                l_AccLeaderboardController = new AccLeaderboardController();
                l_AccFindIndex = l_AccLeaderboardController.m_Leaderboard.Leaderboard.FindIndex(p_X => p_X.ScoreSaberID == p_DiscordOrScoreSaberID);
            }


            EmbedBuilder l_EmbedBuilder = new();
            l_EmbedBuilder.WithTitle(l_Player.m_PlayerFull.playerInfo.playerName);
            l_EmbedBuilder.WithUrl("https://scoresaber.com/u/" + l_Player.m_PlayerFull.playerInfo.playerId);
            l_EmbedBuilder.WithThumbnailUrl("https://new.scoresaber.com" + l_Player.m_PlayerFull.playerInfo.avatar);
            l_EmbedBuilder.AddField("Score Saber Rank", ":earth_africa: #" + l_Player.m_PlayerFull.playerInfo.rank, true);
            
            Color l_Color = GetRoleColor(RoleController.ReadRolesDB().Roles, Context.Guild.Roles, l_PlayerLevel);

            l_EmbedBuilder.WithColor(l_Color);

            string l_PassRankFieldValue = null;
            string l_AccRankFieldValue = null;

            if (l_Config.EnablePassBasedLeaderboard && l_PassLeaderboardController is not null)
            {
                if (l_PassFindIndex == -1)
                    l_PassRankFieldValue = $":medal: #0 - 0 {l_Config.PassPointsName}";

                else
                    l_PassRankFieldValue = $":medal: #{l_PassFindIndex + 1} - {l_PassLeaderboardController.m_Leaderboard.Leaderboard[l_PassFindIndex].Points} {l_Config.PassPointsName}";
            }

            if (l_Config.EnableAccBasedLeaderboard && l_AccLeaderboardController is not null)
            {
                if (l_AccFindIndex == -1)
                    l_AccRankFieldValue = $":medal: #0 - 0 {l_Config.AccPointsName}";

                else
                    l_AccRankFieldValue = $":medal: #{l_AccFindIndex + 1} - {l_AccLeaderboardController.m_Leaderboard.Leaderboard[l_AccFindIndex].Points} {l_Config.AccPointsName}";
            }

            if (l_Config.EnableAccBasedLeaderboard || l_Config.EnablePassBasedLeaderboard)
            {
                l_EmbedBuilder.AddField("Leaderboard Rank", $"{l_PassRankFieldValue}\n{l_AccRankFieldValue}", true);
            }

            l_EmbedBuilder.AddField("\u200B", "\u200B", true);
            l_EmbedBuilder.AddField("Number of passes", ":clap: " + l_PlayerStats.TotalNumberOfPass, true);
            l_EmbedBuilder.AddField("Level", ":trophy: " + l_PlayerLevel, true);
            l_EmbedBuilder.AddField("\u200B", "\u200B", true);
            l_EmbedBuilder.AddField($"Plastic trophies:", $"<:plastic:874215132874571787>: {l_Plastics}", true);
            l_EmbedBuilder.AddField($"Silver trophies:", $"<:silver:874215133197500446>: {l_Silvers}", true);
            l_EmbedBuilder.AddField("\u200B", "\u200B", true);
            l_EmbedBuilder.AddField($"Gold trophies:", $"<:gold:874215133147197460>: {l_Golds}", true);
            l_EmbedBuilder.AddField($"Diamond trophies:", $"<:diamond:874215133289795584>: {l_Diamonds}", true);
            l_EmbedBuilder.AddField("\u200B", "\u200B", true);
            await Context.Channel.SendMessageAsync("", false, l_EmbedBuilder.Build());
            // UserController.UpdatePlayerLevel(Context); /// Seems too heavy for !profile
        }


        private string GenerateProgressBar(int p_Value, int p_MaxValue, int p_Size)
        {
            float l_Percentage = (float)p_Value / p_MaxValue;
            int l_Progress = (int)Math.Round(p_Size * l_Percentage);
            int l_EmptyProgress = p_Size - l_Progress;

            string l_ProgressText = "";
            for (int l_I = 0;
                l_I < l_Progress;
                l_I++)
            {
                l_ProgressText += "▇";
            }

            for (int l_I = 0; l_I < l_EmptyProgress; l_I++)
            {
                l_ProgressText += "—";
            }

            return $"[{l_ProgressText}]";
        }

        public static Color GetRoleColor(List<RoleFormat> p_RoleList, IReadOnlyCollection<SocketRole> p_Roles, int p_Level)
        {
            Color l_Color = Color.Default;
            foreach (var l_UserRole in p_Roles)
            {
                foreach (var l_Role in p_RoleList)
                {
                    if (l_UserRole.Id == l_Role.RoleID && l_Role.LevelID == p_Level && p_Level != 0)
                    {
                        l_Color = l_UserRole.Color;
                    }
                }
            }

            return l_Color;
        }
        
        public static PlayerFromDiscordOrScoreSaberIDFormat PlayerFromDiscordOrScoreSaberID(string p_DiscordOrScoreSaberID, SocketCommandContext p_Context)
        {
            bool l_IsDiscordLinked = false;
            string l_ScoreSaberOrDiscordName = "";
            string l_DiscordID = "";
            SocketGuildUser l_User = null;

            bool l_IsScoreSaberAccount = UserController.AccountExist(p_DiscordOrScoreSaberID);

            if (UserController.UserExist(p_DiscordOrScoreSaberID))
            {
                l_DiscordID = p_DiscordOrScoreSaberID;
                l_User = p_Context.Guild.GetUser(Convert.ToUInt64(p_DiscordOrScoreSaberID));
                p_DiscordOrScoreSaberID = UserController.GetPlayer(p_DiscordOrScoreSaberID);
                if (l_User != null)
                {
                    l_IsDiscordLinked = true;
                    l_ScoreSaberOrDiscordName = l_User.Username;
                }
                else
                {
                    return new PlayerFromDiscordOrScoreSaberIDFormat()
                    {
                        IsScoreSaberAccount = l_IsScoreSaberAccount,
                        DiscordID = l_DiscordID,
                        IsDiscordLinked = true,
                        ScoreSaberOrDiscordName = null
                    };
                }
            }
            else if (l_IsScoreSaberAccount)
            {
                if (!UserController.SSIsAlreadyLinked(p_DiscordOrScoreSaberID))
                {
                    l_User = p_Context.Guild.GetUser(Convert.ToUInt64(UserController.GetDiscordID(p_DiscordOrScoreSaberID)));
                    if (l_User != null)
                    {
                        l_IsDiscordLinked = true;
                        l_DiscordID = l_User.Id.ToString();
                        l_ScoreSaberOrDiscordName = l_User.Username;
                    }
                }
            }
            else if (!UserController.UserExist(p_DiscordOrScoreSaberID))
            {
                return new PlayerFromDiscordOrScoreSaberIDFormat()
                {
                    IsScoreSaberAccount = false,
                    DiscordID = p_DiscordOrScoreSaberID,
                    IsDiscordLinked = false,
                    ScoreSaberOrDiscordName = null
                };
            }

            return new PlayerFromDiscordOrScoreSaberIDFormat()
            {
                IsScoreSaberAccount = l_IsScoreSaberAccount,
                DiscordID = l_DiscordID,
                IsDiscordLinked = l_IsDiscordLinked,
                ScoreSaberOrDiscordName = l_ScoreSaberOrDiscordName
            };
        }
        
        public class PlayerFromDiscordOrScoreSaberIDFormat
        {
            public bool IsDiscordLinked { get; set; }
            public bool IsScoreSaberAccount { get; set; }
            public string DiscordID { get; set; }
            public string ScoreSaberOrDiscordName { get; set; }
        }

        class CheckChannelAttribute : PreconditionAttribute
        {
            public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext p_Context,
                CommandInfo p_Command, IServiceProvider p_Services)
            {
                foreach (var l_AuthorizedChannel in ConfigController.GetConfig().AuthorizedChannels)
                {
                    if (p_Context.Message.Channel.Id == l_AuthorizedChannel)
                    {
                        return Task.FromResult(PreconditionResult.FromSuccess());
                    }
                }

                return Task.FromResult(
                    PreconditionResult.FromError(
                        ExecuteResult.FromError(new Exception(ErrorMessage = "Forbidden channel"))));
            }
        }
    }
}