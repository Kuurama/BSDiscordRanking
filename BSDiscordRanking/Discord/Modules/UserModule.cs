using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using BSDiscordRanking.Formats;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using static System.String;

namespace BSDiscordRanking.Discord.Modules
{
    [CheckChannel]
    public class UserModule : ModuleBase<SocketCommandContext>
    {
        [Command("getinfo")]
        [Summary("Shows informations about available maps found.")]
        public async Task GetInfo(string p_Arg)
        {
            p_Arg = p_Arg.Replace("_", "");
            if (p_Arg.Length < 3)
            {
                await ReplyAsync("> :x: Sorry, the minimum for searching a map is 3 characters (excluding '_').");
                return;
            }

            var l_Maps = new List<Tuple<SongFormat, int>>();
            new LevelController().FetchLevel();
            foreach (var l_LevelID in LevelController.GetLevelControllerCache().LevelID)
            {
                var l_Level = new Level(l_LevelID);
                foreach (var l_Map in l_Level.m_Level.songs)
                {
                    if (l_Map.name.Replace(" ", "").Replace("_", "").Contains(p_Arg, StringComparison.OrdinalIgnoreCase))
                    {
                        l_Maps.Add(new Tuple<SongFormat, int>(l_Map, l_LevelID));
                    }
                }
            }

            if (l_Maps.Count == 0)
            {
                await ReplyAsync("> :x: Sorry, no maps was found.");
                return;
            }

            foreach (var l_GroupedMaps in l_Maps.GroupBy(p_Maps => p_Maps.Item1.hash))
            {
                EmbedBuilder l_EmbedBuilder = new EmbedBuilder();
                foreach (var (l_SongFormat, l_Item2) in l_GroupedMaps)
                {
                    foreach (var l_MapDifficulty in l_SongFormat.difficulties)
                    {
                        l_EmbedBuilder.AddField(l_MapDifficulty.name, l_MapDifficulty.characteristic, true);
                        l_EmbedBuilder.AddField("Level:", $"Lv.{l_Item2}", true);
                        l_EmbedBuilder.AddField("\u200B", "\u200B", true);
                    }
                }

                var l_Map = l_GroupedMaps.First().Item1;

                l_EmbedBuilder.WithDescription("Ranked difficulties:");
                l_EmbedBuilder.WithTitle(l_Map.name);
                l_EmbedBuilder.WithThumbnailUrl($"https://cdn.beatsaver.com/{l_Map.hash.ToLower()}.jpg");
                l_EmbedBuilder.WithUrl($"https://beatsaver.com/maps/{Level.FetchBeatMapByHash(l_Map.hash, Context).versions[^1].key}");
                await Context.Channel.SendMessageAsync("", false, l_EmbedBuilder.Build());
            }
        }


        [Command("link")]
        [Summary("Links your Discord account to your ScoreSaber's one.")]
        public async Task LinkUser(string p_ScoreSaberArg = "")
        {
            if (!IsNullOrEmpty(UserController.GetPlayer(Context.User.Id.ToString())))
                await ReplyAsync(
                    $"> :x: Sorry, but your account already has been linked. Please use `{BotHandler.m_Prefix}unlink`.");
            else if (!IsNullOrEmpty(p_ScoreSaberArg))
            {
                p_ScoreSaberArg = Regex.Match(p_ScoreSaberArg, @"\d+").Value;
                if (IsNullOrEmpty(UserController.GetPlayer(Context.User.Id.ToString())) &&
                    UserController.AccountExist(p_ScoreSaberArg) && !UserController.SSIsAlreadyLinked(p_ScoreSaberArg))
                {
                    UserController.AddPlayer(Context.User.Id.ToString(), p_ScoreSaberArg);
                    await ReplyAsync(
                        $"> :white_check_mark: Your account has been successfully linked.\nLittle tip: use `{BotHandler.m_Prefix}scan` to scan your latest passes!");
                }
                else if (!UserController.AccountExist(p_ScoreSaberArg))
                    await ReplyAsync("> :x: Sorry, but please enter a correct Scoresaber Link/ID.");
                else if (UserController.SSIsAlreadyLinked(p_ScoreSaberArg))
                {
                    await ReplyAsync(
                        $"> :x: Sorry but this account is already linked to an other user.\nIf you entered the correct id and you didn't linked it on an other discord account\nPlease Contact an administrator.");
                }
                else
                    await ReplyAsync("> :x: Oopsie, unhandled error.");
            }
            else
                await ReplyAsync("> :x: Please enter a ScoreSaber link/id.");
        }

        [Command("unlink")]
        [Summary("Unlinks your discord accounts from your ScoreSaber's one.")]
        public async Task UnLinkUser(string p_DiscordID = "")
        {
            if (!IsNullOrEmpty(p_DiscordID))
            {
                if (Context.User is SocketGuildUser l_User)
                {
                    if (l_User.Roles.Any(p_Role => p_Role.Id == Controllers.ConfigController.GetConfig().BotManagementRoleID))
                    {
                        UserController.RemovePlayer(p_DiscordID);
                    }
                }
            }
            else
            {
                if (IsNullOrEmpty(UserController.GetPlayer(Context.User.Id.ToString())))
                {
                    await ReplyAsync($"> :x: Sorry, you don't have any account linked. Please use `{BotHandler.m_Prefix}link` instead.");
                }
                else
                {
                    UserController.RemovePlayer(Context.User.Id.ToString());
                    await ReplyAsync("> :white_check_mark: Your account was successfully unlinked!");
                }
            }
        }

        [Command("scan")]
        [Alias("dc")]
        [Summary("Scans all your scores & passes. Also update your rank.")]
        public async Task Scan_Scores()
        {
            Player l_Player = new Player(UserController.GetPlayer(Context.User.Id.ToString()));
            int l_OldPlayerLevel = l_Player.GetPlayerLevel();
            if (!UserController.UserExist(Context.User.Id.ToString()))
                await ReplyAsync($"> :x: Sorry, you doesn't have any account linked. Please use `{BotHandler.m_Prefix}link` instead.");
            else
            {
                l_Player.FetchScores(Context);
                var l_FetchPass = l_Player.FetchPass(Context);
                if (l_FetchPass.Result >= 1)
                    await ReplyAsync($"> :white_check_mark: Congratulations! You passed {l_FetchPass.Result} new maps!");
                else
                    await ReplyAsync($"> :x: Sorry, you didn't pass any new maps.");

                if (l_OldPlayerLevel != l_Player.GetPlayerLevel())
                {
                    if (l_OldPlayerLevel < l_Player.GetPlayerLevel())
                        await ReplyAsync($"> :white_check_mark: Congratulations! You are now Level {l_Player.GetPlayerLevel()}");
                    else
                        await ReplyAsync($"> :warning: You lost levels. You are now Level {l_Player.GetPlayerLevel()}");
                    await ReplyAsync("> :clock1: The bot will now update your roles. This step can take a while.");
                    var l_RoleUpdate = UserController.UpdatePlayerLevel(Context);
                }
            }
        }

        [Command("ggp")]
        [Alias("getgrindpool")]
        [Summary("Shows the Level's maps while displaying your passes, not giving a specific level will display the next available level you might want to grind.")]
        public async Task GetGrindPool(int p_Level = -1)
        {
            bool l_CheckForLastGGP = false;
            try
            {
                if (p_Level < 0)
                {
                    int l_PlayerLevel =
                        new Player(UserController.GetPlayer(Context.User.Id.ToString())).GetPlayerLevel();
                    int l_LevelTemp = int.MaxValue;
                    foreach (var l_ID in LevelController.GetLevelControllerCache().LevelID)
                    {
                        if (l_ID > l_PlayerLevel && l_ID <= l_LevelTemp)
                        {
                            l_LevelTemp = l_ID;
                        }
                    }

                    p_Level = l_LevelTemp;
                    l_CheckForLastGGP = true;
                }
            }
            catch
            {
                // ignored
            }

            bool l_IDExist = false;
            foreach (var l_ID in LevelController.GetLevelControllerCache().LevelID)
            {
                if (l_ID == p_Level)
                    l_IDExist = true;
            }

            if (l_IDExist)
            {
                Level l_Level = new Level(p_Level);
                try
                {
                    List<bool> l_Passed = new List<bool>();
                    int l_NumberOfPass = 0;
                    Player l_Player = new Player(UserController.GetPlayer(Context.User.Id.ToString()));
                    PlayerPassFormat l_PlayerPasses = l_Player.GetPass();
                    l_Player.LoadStats();
                    int l_I = 0;
                    if (l_Level.m_Level.songs.Count > 0)
                    {
                        foreach (var l_Song in l_Level.m_Level.songs)
                        {
                            if (l_PlayerPasses != null)
                                foreach (var l_PlayerPass in l_PlayerPasses.songs)
                                {
                                    if (l_Song.hash == l_PlayerPass.hash)
                                    {
                                        foreach (var l_SongDifficulty in l_Song.difficulties)
                                        {
                                            foreach (var l_PlayerPassDifficulty in l_PlayerPass.difficulties)
                                            {
                                                if (l_SongDifficulty.characteristic ==
                                                    l_PlayerPassDifficulty.characteristic && l_SongDifficulty.name ==
                                                    l_PlayerPassDifficulty.name)
                                                {
                                                    Console.WriteLine(
                                                        $"Pass detected on {l_Song.name} {l_SongDifficulty.name}");
                                                    l_NumberOfPass++;
                                                    l_Passed.Insert(l_I, true);
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }


                            for (int l_N = 0; l_N < l_Song.difficulties.Count; l_N++)
                            {
                                if (l_Passed.Count <= l_I)
                                {
                                    l_Passed.Insert(l_I, false);
                                }

                                l_I++;
                            }
                        }
                    }

                    int l_NumberOfDifficulties = 0;
                    foreach (var l_Song in l_Level.m_Level.songs)
                    {
                        foreach (var l_Difficulty in l_Song.difficulties)
                        {
                            l_NumberOfDifficulties++;
                        }
                    }

                    string l_PlayerTrophy = "";

                    while (l_Player.m_PlayerStats.Trophy.Count <= p_Level - 1)
                    {
                        l_Player.m_PlayerStats.Trophy.Add(new Trophy
                        {
                            Plastic = 0,
                            Silver = 0,
                            Gold = 0,
                            Diamond = 0
                        });
                    }

                    // ReSharper disable once IntDivisionByZero
                    if (l_NumberOfDifficulties != 0)
                    {
                        switch (l_NumberOfPass * 100 / l_NumberOfDifficulties)
                        {
                            case 0:
                            {
                                l_PlayerTrophy = "";
                                break;
                            }
                            case <= 39:
                            {
                                l_Player.m_PlayerStats.Trophy[p_Level - 1].Plastic = 1;
                                l_PlayerTrophy = "<:plastic:874215132874571787>";
                                break;
                            }
                            case <= 69:
                            {
                                l_Player.m_PlayerStats.Trophy[p_Level - 1].Silver = 1;
                                l_PlayerTrophy = "<:silver:874215133197500446>";
                                break;
                            }
                            case <= 99:
                            {
                                l_Player.m_PlayerStats.Trophy[p_Level - 1].Gold = 1;
                                l_PlayerTrophy = "<:gold:874215133147197460>";
                                break;
                            }

                            case 100:
                            {
                                l_Player.m_PlayerStats.Trophy[p_Level - 1].Diamond = 1;
                                l_PlayerTrophy = "<:diamond:874215133289795584>";
                                break;
                            }
                        }
                    }

                    l_Player.ReWriteStats();

                    EmbedBuilder l_EmbedBuilder = new EmbedBuilder();
                    l_EmbedBuilder.WithTitle($"Maps for Level {p_Level} {l_PlayerTrophy}");
                    string l_BigGgp = null;
                    int l_Y = 0;
                    int l_NumbedOfEmbed = 1;

                    if (ConfigController.GetConfig().BigGGP) l_BigGgp = "\n\u200B";
                    foreach (var l_Song in l_Level.m_Level.songs)
                    {
                        foreach (var l_SongDifficulty in l_Song.difficulties)
                        {
                            var l_EmbedValue = $"{l_SongDifficulty.name} - {l_SongDifficulty.characteristic}{l_BigGgp}";
                            if (l_SongDifficulty.minScoreRequirement != 0)
                                l_EmbedValue += $" - MinScore: {l_SongDifficulty.minScoreRequirement}";

                            if (!l_Passed[l_Y])
                                l_EmbedBuilder.AddField(l_Song.name, l_EmbedValue, true);
                            else
                                l_EmbedBuilder.AddField($"~~{l_Song.name}~~", $"~~{l_EmbedValue}~~", true);

                            if (l_NumbedOfEmbed % 2 != 0)
                            {
                                if (l_Y % 2 == 0)
                                    l_EmbedBuilder.AddField("\u200B", "\u200B", true);
                            }
                            else if (l_Y % 2 != 0)
                                l_EmbedBuilder.AddField("\u200B", "\u200B", true);

                            l_Y++;
                            if (l_Y % 15 == 0)
                            {
                                await Context.Channel.SendMessageAsync("", false, l_EmbedBuilder.Build());
                                l_EmbedBuilder = new EmbedBuilder();
                                l_NumbedOfEmbed++;
                            }
                        }
                    }

                    l_EmbedBuilder.WithFooter(
                        $"To get the playlist file: use {BotHandler.m_Prefix}getplaylist {p_Level}");
                    await Context.Channel.SendMessageAsync("", false, l_EmbedBuilder.Build());

                    l_Player.SetGrindInfo(p_Level, l_Passed, -1, l_Player.m_PlayerStats.Trophy[p_Level - 1], -1);
                }
                catch (Exception l_Exception)
                {
                    await ReplyAsync($"> :x: Error occured : {l_Exception.Message}");
                }
            }
            else if (l_CheckForLastGGP)
            {
                await ReplyAsync(
                    "> :white_check_mark: Seems like there isn't any new level to grind for you right now, good job.");
            }
            else
            {
                await ReplyAsync("> :x: This level does not exist.");
            }
        }

        [Command("getplaylist")]
        [Alias("gpl")]
        [Summary("Sends the desired Level's playlist file. Use `all` instead of the level id to get the whole level folder.")]
        public async Task GetPlaylist(string p_Level)
        {
            if (int.TryParse(p_Level, out _))
            {
                string l_Path = Level.GetPath() + $"/{p_Level}{Level.SUFFIX_NAME}.bplist";
                if (File.Exists(l_Path))

                    await Context.Channel.SendFileAsync(l_Path, "> :white_check_mark: Here's your playlist!");
                else

                    await Context.Channel.SendMessageAsync("> :x: This level does not exist.");
            }
            else if (p_Level == "all")
            {
                if (File.Exists("levels.zip"))
                    File.Delete("levels.zip");
                try
                {
                    ZipFile.CreateFromDirectory("./Levels/", "levels.zip");
                    await Context.Channel.SendFileAsync("levels.zip", "> :white_check_mark: Here's your playlist folder!");
                }
                catch
                {
                    await ReplyAsync("> :x: Seems like you forgot to add Levels. Unless you want an empty zip file?");
                }
            }
            else
                await ReplyAsync("> :x: Wrong argument, please use \"1,2,3..\" or \"all\"");
        }

        [Command("profile")]
        [Alias("stats")]
        [Summary("Sends your profile's informations (Level, Passes, Trophies etc).")]
        public async Task Profile()
        {
            if (!UserController.UserExist(Context.User.Id.ToString()))
            {
                await ReplyAsync($"> :x: Sorry, you doesn't have any account linked. Please use `{BotHandler.m_Prefix}link` instead.");
            }
            else
            {
                Player l_Player = new Player(UserController.GetPlayer(Context.User.Id.ToString()));
                var l_PlayerStats = l_Player.GetStats();

                int l_Plastics = 0;
                int l_Silvers = 0;
                int l_Golds = 0;
                int l_Diamonds = 0;
                foreach (var l_Trophy in l_PlayerStats.Trophy)
                {
                    l_Plastics += l_Trophy.Plastic;
                    l_Silvers += l_Trophy.Silver;
                    l_Golds += l_Trophy.Gold;
                    l_Diamonds += l_Trophy.Diamond;
                }

                LeaderboardController l_LeaderboardController = new LeaderboardController();
                int l_FindIndex = l_LeaderboardController.m_Leaderboard.Leaderboard.FindIndex(p_X =>
                    p_X.ScoreSaberID == UserController.GetPlayer(Context.User.Id.ToString()));
                EmbedBuilder l_EmbedBuilder = new();
                l_EmbedBuilder.WithTitle(l_Player.m_PlayerFull.playerInfo.playerName);
                l_EmbedBuilder.WithUrl("https://scoresaber.com/u/" + l_Player.m_PlayerFull.playerInfo.playerId);
                l_EmbedBuilder.WithThumbnailUrl("https://new.scoresaber.com" + l_Player.m_PlayerFull.playerInfo.avatar);
                l_EmbedBuilder.AddField("Global Rank", ":earth_africa: #" + l_Player.m_PlayerFull.playerInfo.rank, true);
                if (l_FindIndex == -1)
                    l_EmbedBuilder.AddField("Server Rank", ":medal: #0 - 0 RPL", true);
                else
                    l_EmbedBuilder.AddField("Server Rank", ":medal: #" + $"{l_FindIndex + 1} - {l_LeaderboardController.m_Leaderboard.Leaderboard[l_FindIndex].Points} RPL", true);
                l_EmbedBuilder.AddField("\u200B", "\u200B", true);
                l_EmbedBuilder.AddField("Number of passes", ":clap: " + l_PlayerStats.TotalNumberOfPass, true);
                l_EmbedBuilder.AddField("Level", ":trophy: " + l_Player.GetPlayerLevel(), true);
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
        }

        [Command("ping")]
        [Summary("Shows the latency from Discord & ScoreSaber")]
        public async Task Ping()
        {
            EmbedBuilder l_EmbedBuilder = new EmbedBuilder();
            l_EmbedBuilder.AddField("Discord: ", new Ping().Send("discord.com").RoundtripTime + "ms");
            l_EmbedBuilder.AddField("ScoreSaber: ", new Ping().Send("scoresaber.com").RoundtripTime + "ms");
            l_EmbedBuilder.WithFooter("#LoveArche",
                "https://images.genius.com/d4b8905048993e652aba3d8e105b5dbf.1000x1000x1.jpg");
            l_EmbedBuilder.WithColor(Color.Blue);
            await Context.Channel.SendMessageAsync("", false, l_EmbedBuilder.Build());
        }

        [Command("leaderboard")]
        [Alias("ld", "leaderboards")]
        [Summary("Shows the leaderboard.")]
        public async Task Leaderboard(int p_Page = default)
        {
            bool l_PageExist = false;
            EmbedBuilder l_EmbedBuilder = new EmbedBuilder();
            LeaderboardController l_LeaderboardController = new LeaderboardController();
            if (p_Page == default)
            {
                try
                {
                    p_Page = l_LeaderboardController.m_Leaderboard.Leaderboard.FindIndex(x =>
                        x.ScoreSaberID == UserController.GetPlayer(Context.User.Id.ToString())) / 10 + 1;
                }
                catch
                {
                    p_Page = 1;
                }
            }

            for (var l_Index = (p_Page - 1) * 10; l_Index < (p_Page - 1) * 10 + 10; l_Index++)
            {
                try
                {
                    var l_RankedPlayer = l_LeaderboardController.m_Leaderboard.Leaderboard[l_Index];
                    l_EmbedBuilder.AddField(
                        $"#{l_Index + 1} - {l_RankedPlayer.Name} : {l_RankedPlayer.Points} RPL",
                        $"Level: {l_RankedPlayer.Level}. [ScoreSaber Profile](https://scoresaber.com/u/{l_RankedPlayer.ScoreSaberID})");
                    l_PageExist = true;
                }
                catch
                {
                    // ignored
                }
            }

            if (l_PageExist)
            {
                l_EmbedBuilder.WithTitle("Leaderboard:");
                await Context.Channel.SendMessageAsync("", false, l_EmbedBuilder.Build());
            }
            else
                await ReplyAsync("> :x: Sorry, this page doesn't exist");
        }

        [Command("help")]
        [Summary("Shows all the commands & their summaries.")]
        public async Task Help()
        {
            bool l_IsAdmin = false;
            if (Context.User is SocketGuildUser l_User)
                if (l_User.Roles.Any(p_Role => p_Role.Id == ConfigController.GetConfig().BotManagementRoleID))
                    l_IsAdmin = true;


            int i = 0;
            foreach (var l_Module in BotHandler.m_Commands.Modules)
            {
                EmbedBuilder l_Builder = new EmbedBuilder();
                l_Builder.WithTitle(l_Module.Name);
                foreach (var l_Command in l_Module.Commands)
                {
                    string l_Title = ConfigController.GetConfig().CommandPrefix.First() + l_Command.Name;
                    foreach (var l_Parameter in l_Command.Parameters)
                    {
                        l_Title += " [" + l_Parameter.Name.Replace("p_", "") + "]";
                    }

                    l_Builder.AddField(l_Title, l_Command.Summary, true);
                    l_Builder.WithFooter("Prefix: " + Join(", ", ConfigController.GetConfig().CommandPrefix) + " | Bot made by Julien#1234 & Kuurama#3423");
                }

                if (l_Module.Name.Contains("Admin"))
                {
                    if (!l_IsAdmin)
                        continue;
                    l_Builder.WithColor(Color.Red);
                    l_Builder.Footer = null;
                }

                await Context.Channel.SendMessageAsync("", false, l_Builder.Build());
                i++;
            }
        }
    }

    class CheckChannelAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext p_Context, CommandInfo p_Command, IServiceProvider p_Services)
        {
            foreach (var l_AuthorizedChannel in ConfigController.GetConfig().AuthorizedChannels)
            {
                if (p_Context.Message.Channel.Id == l_AuthorizedChannel)
                {
                    return Task.FromResult(PreconditionResult.FromSuccess());
                }
            }

            return Task.FromResult(PreconditionResult.FromError(ExecuteResult.FromError(new Exception(ErrorMessage = "Forbidden channel"))));
        }
    }
}