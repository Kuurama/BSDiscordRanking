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
    [CheckChannel] /// Actually makes all user's related command only work on the specifics attributed channels.
    public partial class UserModule : ModuleBase<SocketCommandContext>
    {
        private const int PERMISSION = 0;

        public static LevelFormat RemovePassFromPlaylist(PlayerPassFormat p_PlayerPass, LevelFormat p_LevelFormat,  string p_Category, string p_ScoreSaberID)
        {
            if (string.IsNullOrEmpty(p_Category)) p_Category = "null";

            LevelFormat l_TempFormat = new LevelFormat
            {
                customData = new MainCustomData
                {
                    syncURL = ConfigController.GetConfig().ApiURL + "playlist/" + p_LevelFormat.customData.level + "/" + p_Category + "/" + p_ScoreSaberID,
                    autoWeightDifficultyMultiplier = p_LevelFormat.customData.autoWeightDifficultyMultiplier,
                    customPassText = p_LevelFormat.customData.customPassText,
                    level = p_LevelFormat.customData.level,
                    weighting = p_LevelFormat.customData.weighting
                },
                image = p_LevelFormat.image,
                playlistAuthor = p_LevelFormat.playlistAuthor,
                playlistDescription = p_LevelFormat.playlistDescription,
                playlistTitle = p_LevelFormat.playlistTitle,
                songs = new List<SongFormat>()
            };

            if (p_LevelFormat.songs.Count > 0)
            {
                foreach (SongFormat l_Song in p_LevelFormat.songs) l_TempFormat.songs.Add(l_Song);

                foreach (InPlayerSong l_PlayerPassSong in p_PlayerPass.SongList)
                    for (int l_I = l_TempFormat.songs.Count - 1; l_I >= 0; l_I--)
                        if (l_TempFormat.songs.Count > l_I)
                            if (string.Equals(l_TempFormat.songs[l_I].hash, l_PlayerPassSong.hash,
                                    StringComparison.CurrentCultureIgnoreCase))
                                foreach (InPlayerPassFormat l_PlayerPassSongDifficulty in l_PlayerPassSong.DiffList)
                                    if (l_TempFormat.songs.Count > l_I)
                                        for (int l_Y = l_TempFormat.songs[l_I].difficulties.Count - 1; l_Y >= 0; l_Y--)
                                            if (l_TempFormat.songs[l_I].difficulties.Count > 0 &&
                                                l_TempFormat.songs.Count > 0)
                                                if (l_TempFormat.songs[l_I].difficulties[l_Y].characteristic ==
                                                    l_PlayerPassSongDifficulty.Difficulty.characteristic &&
                                                    l_TempFormat.songs[l_I].difficulties[l_Y].name ==
                                                    l_PlayerPassSongDifficulty.Difficulty.name)
                                                {
                                                    /// Here remove diff or map if it's the only ranked diff
                                                    if (l_TempFormat.songs[l_I].difficulties.Count <= 1)
                                                        l_TempFormat.songs.Remove(l_TempFormat.songs[l_I]);
                                                    else
                                                        l_TempFormat.songs[l_I].difficulties
                                                            .Remove(l_TempFormat.songs[l_I].difficulties[l_Y]);
                                                }
            }

            return l_TempFormat;
        }

        public static RemoveCategoriesFormat RemoveOtherCategoriesFromPlaylist(LevelFormat p_LevelFormat, string p_Category)
        {
            RemoveCategoriesFormat l_RemoveCategoriesFormat = new RemoveCategoriesFormat { Categories = new List<string>() };
            LevelFormat l_TempFormat = new LevelFormat
            {
                customData = new MainCustomData
                {
                    syncURL = ConfigController.GetConfig().ApiURL + "playlist/" + p_LevelFormat.customData.level + "/" + p_Category,
                    autoWeightDifficultyMultiplier = p_LevelFormat.customData.autoWeightDifficultyMultiplier,
                    customPassText = p_LevelFormat.customData.customPassText,
                    level = p_LevelFormat.customData.level,
                    weighting = p_LevelFormat.customData.weighting
                },
                image = p_LevelFormat.image,
                playlistAuthor = p_LevelFormat.playlistAuthor,
                playlistDescription = p_LevelFormat.playlistDescription,
                playlistTitle = p_LevelFormat.playlistTitle,
                songs = new List<SongFormat>()
            };
            if (p_LevelFormat.songs.Count > 0)
            {
                foreach (SongFormat l_Song in p_LevelFormat.songs) l_TempFormat.songs.Add(l_Song);

                for (int l_I = l_TempFormat.songs.Count - 1; l_I >= 0; l_I--)
                    if (l_TempFormat.songs.Count > l_I)
                        if (l_TempFormat.songs.Count > l_I)
                            for (int l_Y = l_TempFormat.songs[l_I].difficulties.Count - 1; l_Y >= 0; l_Y--)
                                if (l_TempFormat.songs[l_I].difficulties.Count > 0 && l_TempFormat.songs.Count > 0)
                                    if (l_TempFormat.songs[l_I].difficulties[l_Y].customData.category !=
                                        p_Category)
                                    {
                                        int l_FindIndex = l_RemoveCategoriesFormat.Categories.FindIndex(p_X => p_X == l_TempFormat.songs[l_I].difficulties[l_Y].customData.category);
                                        if (l_FindIndex < 0) l_RemoveCategoriesFormat.Categories.Add(l_TempFormat.songs[l_I].difficulties[l_Y].customData.category);

                                        /// Here remove diffs or maps on which the category isn't correct.
                                        if (l_TempFormat.songs[l_I].difficulties.Count <= 1)
                                            l_TempFormat.songs.Remove(l_TempFormat.songs[l_I]);
                                        else
                                            l_TempFormat.songs[l_I].difficulties
                                                .Remove(l_TempFormat.songs[l_I].difficulties[l_Y]);
                                    }
            }

            l_RemoveCategoriesFormat.LevelFormat = l_TempFormat;
            ;

            return l_RemoveCategoriesFormat;
        }

        private static string RemoveSpecialCharacters(string p_Str)
        {
            StringBuilder l_SB = new StringBuilder();
            foreach (char l_C in p_Str)
                if (l_C is >= '0' and <= '9' or >= 'A' and <= 'Z' or >= 'a' and <= 'z' or '_')
                    l_SB.Append(l_C);

            return l_SB.ToString();
        }

        private static void DeleteAllFolderAndFile(string p_OriginalPath)
        {
            try
            {
                DirectoryInfo l_Directory = new DirectoryInfo(p_OriginalPath);
                foreach (FileInfo l_File in l_Directory.EnumerateFiles()) l_File.Delete();

                foreach (DirectoryInfo l_Dir in l_Directory.EnumerateDirectories()) l_Dir.Delete(true);

                Directory.Delete(p_OriginalPath);
            }
            catch (Exception l_Exception)
            {
                Console.WriteLine($"DeleteAllFolderAndFile : {l_Exception}");
            }
        }

        private static void DeleteFile(string p_FilePath)
        {
            try
            {
                if (File.Exists(p_FilePath)) /// Mean there is already a personnal playlist file.
                    File.Delete(p_FilePath);
            }
            catch (Exception l_Exception)
            {
                Console.WriteLine($"DeleteFile : {l_Exception}");
            }
        }

        private static string GetTrophyString(bool p_UseBigEmote, int p_NumberOfPass, int p_TotalNumberOfMaps, float p_Multiplier = 1.0f)
        {
            string l_TrophyString = null;

            if (p_TotalNumberOfMaps != 0)
            {
                if (!p_UseBigEmote)
                {
#pragma warning disable 8509
                    l_TrophyString = (p_NumberOfPass * 100 * p_Multiplier / p_TotalNumberOfMaps) switch
                    {
                        <= 0 => "",
                        < 25 => "<:plastic:874215132874571787>",
                        < 50 => "<:silver:874215133197500446>",
                        < 75 => "<:gold:874215133147197460>",
                        < 100 => "<:diamond:874215133289795584>",
                        >= 100 => "<:ruby:954043083887116368>"
                    };
                }
                else
                {
#pragma warning disable 8509
                    l_TrophyString = (p_NumberOfPass * 100 * p_Multiplier / p_TotalNumberOfMaps) switch
#pragma warning restore 8509
                    {
                        <= 0 => "",
                        < 25 => "<:big_plastic:916492151402164314>",
                        < 50 => "<:big_silver:916492243743932467>",
                        < 75 => "<:big_gold:916492277780709426>",
                        < 100 => "<:big_diamond:916492304108355685>",
                        >= 100 => "<:big_ruby:916803316925755473>"
                    };
                }
            }

            return l_TrophyString;
        }

        private static double StandardDeviation(IReadOnlyCollection<double> p_Sequence)
        {
            double l_Result = 0;

            if (p_Sequence.Any())
            {
                double l_Average = p_Sequence.Average();
                double l_Sum = p_Sequence.Sum(p_X => Math.Pow(p_X - l_Average, 2));
                l_Result = Math.Sqrt(l_Sum / (p_Sequence.Count - 1));
            }

            return l_Result;
        }

        private async Task SendProfile(string p_DiscordOrScoreSaberID, bool p_IsSomeoneElse)
        {
            ConfigFormat l_Config = ConfigController.GetConfig();
            bool l_IsScoreSaberAccount = UserController.AccountExist(p_DiscordOrScoreSaberID, out _);

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
                        if (l_User.Roles.Any(p_Role => p_Role.Id == l_Config.BotAdminRoleID))
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
                    ? "> :x: Sorry, this Discord User don't have any ScoreSaber account linked/isn't a correct ScoreSaberID."
                    : $"> :x: Sorry, you don't have any account linked. Please use `{BotHandler.m_Prefix}link <ScoreSaber link/id>` instead.\n> (Or to get started with the bot: use the `{BotHandler.m_Prefix}getstarted` command)");
                return;
            }

            Player l_Player = new Player(p_DiscordOrScoreSaberID, false);
            int l_GlobalPlayerLevel = l_Player.GetPlayerLevel();
            PlayerStatsFormat l_PlayerStats = l_Player.GetStats();

            int l_Plastics = 0;
            int l_Silvers = 0;
            int l_Golds = 0;
            int l_Diamonds = 0;
            int l_Rubys = 0;


            if (l_Player.m_PlayerStats.Levels is not null)
                if (l_Config.EnableLevelByCategory)
                    foreach (CategoryPassed l_Category in l_Player.m_PlayerStats.Levels.SelectMany(p_PlayerStatsLevel => p_PlayerStatsLevel.Categories))
                    {
                        l_Category.Trophy ??= new Trophy();
                        l_Plastics += l_Category.Trophy.Plastic;
                        l_Silvers += l_Category.Trophy.Silver;
                        l_Golds += l_Category.Trophy.Gold;
                        l_Diamonds += l_Category.Trophy.Diamond;
                        l_Rubys += l_Category.Trophy.Ruby;
                    }
                else
                    foreach (PassedLevel l_PassedLevel in l_Player.m_PlayerStats.Levels)
                    {
                        l_PassedLevel.Trophy ??= new Trophy();
                        l_Plastics += l_PassedLevel.Trophy.Plastic;
                        l_Silvers += l_PassedLevel.Trophy.Silver;
                        l_Golds += l_PassedLevel.Trophy.Gold;
                        l_Diamonds += l_PassedLevel.Trophy.Diamond;
                        l_Rubys += l_PassedLevel.Trophy.Ruby;
                    }


            int l_PassFindIndex = -1;
            PassLeaderboardController l_PassLeaderboardController = null;
            bool l_IsAccLeaderboardBan = false;
            bool l_IsPassLeaderboardBan = false;
            if (l_Config.EnableAccBasedLeaderboard)
            {
                l_PassLeaderboardController = new PassLeaderboardController();
                l_IsPassLeaderboardBan = l_PassLeaderboardController.m_Leaderboard.Leaderboard.Any(p_X => p_X.ScoreSaberID == p_DiscordOrScoreSaberID && p_X.IsBanned);
                l_PassLeaderboardController.m_Leaderboard.Leaderboard.RemoveAll(p_X => p_X.IsBanned);
                l_PassFindIndex = l_PassLeaderboardController.m_Leaderboard.Leaderboard.FindIndex(p_X => p_X.ScoreSaberID == p_DiscordOrScoreSaberID);
            }

            int l_AccFindIndex = -1;
            AccLeaderboardController l_AccLeaderboardController = null;
            if (l_Config.EnableAccBasedLeaderboard)
            {
                l_AccLeaderboardController = new AccLeaderboardController();
                l_IsAccLeaderboardBan = l_AccLeaderboardController.m_Leaderboard.Leaderboard.Any(p_X => p_X.ScoreSaberID == p_DiscordOrScoreSaberID && p_X.IsBanned);
                l_AccLeaderboardController.m_Leaderboard.Leaderboard.RemoveAll(p_X => p_X.IsBanned);
                l_AccFindIndex = l_AccLeaderboardController.m_Leaderboard.Leaderboard.FindIndex(p_X => p_X.ScoreSaberID == p_DiscordOrScoreSaberID);
            }


            EmbedBuilder l_EmbedBuilder = new EmbedBuilder();
            l_EmbedBuilder.WithTitle(l_Player.m_PlayerFull.name);
            l_EmbedBuilder.WithUrl("https://scoresaber.com/u/" + l_Player.m_PlayerFull.id);
            l_EmbedBuilder.WithThumbnailUrl(l_Player.m_PlayerFull.profilePicture);
            l_EmbedBuilder.AddField("Score Saber Rank", ":earth_africa: #" + l_Player.m_PlayerFull.rank, true);

            Color l_Color = GetRoleColor(RoleController.ReadRolesDB().Roles, Context.Guild.Roles, l_GlobalPlayerLevel);

            l_EmbedBuilder.WithColor(l_Color);

            string l_PassRankFieldValue = null;
            string l_AccRankFieldValue = null;

            if (l_Config.EnablePassBasedLeaderboard && l_PassLeaderboardController is not null && !l_IsPassLeaderboardBan)
            {
                if (l_PassFindIndex == -1)
                    l_PassRankFieldValue = $":medal: #0 - 0 {l_Config.PassPointsName}";

                else
                    l_PassRankFieldValue = $":medal: #{l_PassFindIndex + 1} - {l_PassLeaderboardController.m_Leaderboard.Leaderboard[l_PassFindIndex].Points} {l_Config.PassPointsName}";
            }

            if (l_Config.EnableAccBasedLeaderboard && l_AccLeaderboardController is not null && !l_IsAccLeaderboardBan)
            {
                if (l_AccFindIndex == -1)
                    l_AccRankFieldValue = $":medal: #0 - 0 {l_Config.AccPointsName}";

                else
                    l_AccRankFieldValue = $":medal: #{l_AccFindIndex + 1} - {l_AccLeaderboardController.m_Leaderboard.Leaderboard[l_AccFindIndex].Points} {l_Config.AccPointsName}";
            }

            if (l_Config.EnableAccBasedLeaderboard || l_Config.EnablePassBasedLeaderboard)
                if (l_PassRankFieldValue != null && l_AccRankFieldValue != null)
                    l_EmbedBuilder.AddField("Leaderboard Rank", $"{l_PassRankFieldValue}\n{l_AccRankFieldValue}", true);
                else if (l_PassRankFieldValue != null)
                    l_EmbedBuilder.AddField("Leaderboard Rank", $"{l_PassRankFieldValue}", true);
                else if (l_AccRankFieldValue != null) l_EmbedBuilder.AddField("Leaderboard Rank", $"{l_AccRankFieldValue}", true);

            l_EmbedBuilder.AddField("\u200B", "\u200B", true);
            l_EmbedBuilder.AddField("Number of passes", ":clap: " + l_PlayerStats.TotalNumberOfPass, true);

            if (l_Config.EnableLevelByCategory)
            {
                List<double> l_LevelEquilibriumList = new List<double>();
                bool l_GlobalLevelIsUseless = false;
                List<Tuple<string, int>> l_CategoryTuples = new List<Tuple<string, int>>();
                foreach (CategoryPassed l_LevelCategory in from l_Level in l_PlayerStats.Levels where l_Level.Categories != null from l_LevelCategory in l_Level.Categories let l_CategoryFindIndex = l_CategoryTuples.FindIndex(p_X => p_X.Item1 == l_LevelCategory.Category) where l_CategoryFindIndex < 0 && l_Level.LevelID == 1 select l_LevelCategory) l_CategoryTuples.Add(new Tuple<string, int>(l_LevelCategory.Category, l_Player.GetPlayerLevel(false, l_LevelCategory.Category, true)));

                int l_Index = 0;
                foreach ((string l_CategoryName, int l_CategoryMaxLevel) in l_CategoryTuples.Where(p_Category => p_Category.Item1 != null))
                {
                    l_Index++;
                    int l_LevelCategory = l_Player.GetPlayerLevel(false, l_CategoryName);

                    if (l_LevelCategory != l_CategoryMaxLevel) l_LevelEquilibriumList.Add(l_LevelCategory);

                    if (l_GlobalPlayerLevel == l_LevelCategory)
                    {
                        l_GlobalLevelIsUseless = true;
                        l_EmbedBuilder.AddField($"{l_CategoryName}", $"{GetTrophyString(true, l_LevelCategory, l_CategoryMaxLevel)} **{l_LevelCategory}/{l_CategoryMaxLevel}**", true);
                    }
                    else
                    {
                        l_EmbedBuilder.AddField($"{l_CategoryName}", $"{GetTrophyString(true, l_LevelCategory, l_CategoryMaxLevel)} {l_LevelCategory}/{l_CategoryMaxLevel}", true);
                    }

                    if (l_Index % 2 == 0) l_EmbedBuilder.AddField("\u200B", "\u200B");
                }

                if (l_Index != 0 && l_Index % 2 != 0) l_EmbedBuilder.AddField("\u200B", "\u200B");

                if (!l_GlobalLevelIsUseless) l_EmbedBuilder.AddField("Global Level", $":trophy: **{l_GlobalPlayerLevel}**", true);

                double l_LevelEquilibriumPercentage;
                if (l_LevelEquilibriumList.Any())
                    l_LevelEquilibriumPercentage = Math.Abs(100f - StandardDeviation(l_LevelEquilibriumList) * 100f / l_LevelEquilibriumList.Average());
                else
                    l_LevelEquilibriumPercentage = 100f;

                EmbedFieldBuilder l_LevelEquilibriumField = new EmbedFieldBuilder { Name = "Skill Equilibrium", Value = $"{GetTrophyString(true, (int)l_LevelEquilibriumPercentage, 100)} {l_LevelEquilibriumPercentage:n2}%", IsInline = true };
                l_EmbedBuilder.Fields.Insert(4, l_LevelEquilibriumField);
                l_EmbedBuilder.Fields.Insert(5, new EmbedFieldBuilder { Name = "\u200B", Value = "\u200B", IsInline = false });
            }
            else
            {
                l_EmbedBuilder.AddField("Global Level", $":trophy: **{l_GlobalPlayerLevel}**", true);
                l_EmbedBuilder.AddField("\u200B", "\u200B", true);
            }


            l_EmbedBuilder.AddField("Plastic trophies:", $"<:big_plastic:916492151402164314>: {l_Plastics}", true);
            l_EmbedBuilder.AddField("\u200B", "\u200B", true);
            l_EmbedBuilder.AddField("Silver trophies:", $"<:big_silver:916492243743932467>: {l_Silvers}", true);
            l_EmbedBuilder.AddField("Gold trophies:", $"<:big_gold:916492277780709426>: {l_Golds}", true);
            l_EmbedBuilder.AddField("\u200B", "\u200B", true);
            l_EmbedBuilder.AddField("Diamond trophies:", $"<:big_diamond:916492304108355685>: {l_Diamonds}", true);
            l_EmbedBuilder.AddField("Ruby trophies:", $"<:big_ruby:916803316925755473>: {l_Rubys}", true);

            await Context.Channel.SendMessageAsync("", false, l_EmbedBuilder.Build());
        }

        public static string FirstCharacterToUpper(string p_Text)
        {
            if (p_Text == null)
                return null;

            p_Text = p_Text.Length switch
            {
                0 => null,
                1 => char.ToUpper(p_Text[0]).ToString(),
                _ => char.ToUpper(p_Text[0]) + p_Text.Substring(1)
            };
            return p_Text;
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
                l_ProgressText += "▇";

            for (int l_I = 0; l_I < l_EmptyProgress; l_I++) l_ProgressText += "—";

            return $"[{l_ProgressText}]";
        }

        public static Color GetRoleColor(List<RoleFormat> p_RoleList, IEnumerable<SocketRole> p_Roles, int p_Level, ulong p_RoleID = default(ulong))
        {
            Color l_Color = Color.Default;
            if (p_RoleList == null) return l_Color;

            if (p_Roles == null)
            {
                foreach (RoleFormat l_Role in p_RoleList)
                    if (l_Role.LevelID == p_Level && p_Level != 0)
                    {
                        return l_Role.RoleColor;
                    }
            }
            else
            {
                foreach (SocketRole l_UserRole in p_Roles)
                    if (p_RoleID != default(ulong))
                    {
                        if (l_UserRole.Id == p_RoleID)
                            return l_UserRole.Color;
                    }
                    else
                    {
                        foreach (RoleFormat l_Role in p_RoleList)
                            if (l_UserRole.Id == l_Role.RoleID && l_Role.LevelID == p_Level && p_Level != 0)
                            {
                                if (l_Role.RoleColor != l_UserRole.Color)
                                {
                                    l_Role.RoleColor = l_UserRole.Color;
                                    RoleController.StaticWriteRolesDB(new RolesFormat() { Roles = p_RoleList });
                                }
                                return l_UserRole.Color;
                            }
                    }
            }

            return l_Color;
        }

        public static Color GetRoleColorFromDatabase(List<RoleFormat> p_RoleList, IEnumerable<SocketRole> p_Roles, int p_Level, ulong p_RoleID = default(ulong))
        {
            Color l_Color = Color.Default;
            foreach (SocketRole l_UserRole in p_Roles)
                if (p_RoleID != default(ulong))
                {
                    if (l_UserRole.Id == p_RoleID)
                        return l_UserRole.Color;
                }
                else
                {
                    foreach (RoleFormat l_Role in p_RoleList)
                        if (l_UserRole.Id == l_Role.RoleID && l_Role.LevelID == p_Level && p_Level != 0)
                        {
                            if (l_Role.RoleColor != l_UserRole.Color)
                            {
                                l_Role.RoleColor = l_UserRole.Color;
                            }
                            return l_UserRole.Color;
                        }
                }

            return l_Color;
        }

        private static PlayerFromDiscordOrScoreSaberIDFormat PlayerFromDiscordOrScoreSaberID(string p_DiscordOrScoreSaberID, SocketCommandContext p_Context)
        {
            bool l_IsDiscordLinked = false;
            string l_ScoreSaberOrDiscordName = "";
            string l_DiscordID = "";
            SocketGuildUser l_User = null;

            bool l_IsScoreSaberAccount = UserController.AccountExist(p_DiscordOrScoreSaberID, out _);

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
                    return new PlayerFromDiscordOrScoreSaberIDFormat
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
                return new PlayerFromDiscordOrScoreSaberIDFormat
                {
                    IsScoreSaberAccount = false,
                    DiscordID = p_DiscordOrScoreSaberID,
                    IsDiscordLinked = false,
                    ScoreSaberOrDiscordName = null
                };
            }

            return new PlayerFromDiscordOrScoreSaberIDFormat
            {
                IsScoreSaberAccount = l_IsScoreSaberAccount,
                DiscordID = l_DiscordID,
                IsDiscordLinked = l_IsDiscordLinked,
                ScoreSaberOrDiscordName = l_ScoreSaberOrDiscordName
            };
        }

        public class RemoveCategoriesFormat
        {
            public List<string> Categories;
            public LevelFormat LevelFormat;
        }

        public class PlayerFromDiscordOrScoreSaberIDFormat
        {
            public bool IsDiscordLinked { get; set; }
            public bool IsScoreSaberAccount { get; set; }
            public string DiscordID { get; set; }
            public string ScoreSaberOrDiscordName { get; set; }
        }

        private class CheckChannelAttribute : PreconditionAttribute
        {
            public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext p_Context,
                CommandInfo p_Command, IServiceProvider p_Services)
            {
                foreach (ulong l_AuthorizedChannel in ConfigController.GetConfig().AuthorizedChannels)
                    if (p_Context.Message.Channel.Id == l_AuthorizedChannel)
                        return Task.FromResult(PreconditionResult.FromSuccess());

                return Task.FromResult(
                    PreconditionResult.FromError(
                        ExecuteResult.FromError(new Exception(ErrorMessage = "Forbidden channel"))));
            }
        }
    }
}
