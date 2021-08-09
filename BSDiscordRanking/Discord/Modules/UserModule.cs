using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace BSDiscordRanking.Discord.Modules
{
    [CheckChannel]
    public class UserModule : ModuleBase<SocketCommandContext>
    {
        [Command("profile")]
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

                int l_Plastics = l_PlayerStats.Trophy.Plastic;
                int l_Silvers = l_PlayerStats.Trophy.Silver;
                int l_Golds = l_PlayerStats.Trophy.Plastic;
                int l_Diamonds = l_PlayerStats.Trophy.Plastic;

                EmbedBuilder l_EmbedBuilder = new();
                l_EmbedBuilder.WithTitle(l_Player.m_PlayerFull.playerInfo.playerName);
                l_EmbedBuilder.WithUrl("https://scoresaber.com/u/" + l_Player.m_PlayerFull.playerInfo.playerId);
                l_EmbedBuilder.WithThumbnailUrl("https://new.scoresaber.com" + l_Player.m_PlayerFull.playerInfo.avatar);
                l_EmbedBuilder.AddField("Global Rank", ":earth_africa: #" + l_Player.m_PlayerFull.playerInfo.rank);
                l_EmbedBuilder.AddField("Number of passes", ":clap: " + l_PlayerStats.TotalNumberOfPass, true);
                l_EmbedBuilder.AddField("Level", ":trophy: " + l_Player.GetPlayerLevel(), true);
                l_EmbedBuilder.AddField("\u200B", "\u200B", true);
                l_EmbedBuilder.AddField("Plastic Trophies:", l_Plastics, true);
                l_EmbedBuilder.AddField("Silver Trophies:", l_Silvers, true);
                l_EmbedBuilder.AddField("\u200B", "\u200B", true);
                l_EmbedBuilder.AddField("Gold Trophies:", l_Golds, true);
                l_EmbedBuilder.AddField("Diamond Trophies:", l_Diamonds, true);
                l_EmbedBuilder.AddField("\u200B", "\u200B", true);
                await Context.Channel.SendMessageAsync("", false, l_EmbedBuilder.Build());
                UserController.UpdatePlayerLevel(Context);
            }
        }


        [Command("getplaylist")]
        [Alias("gpl")]
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

        [Command("ggp")]
        [Alias("getgrindpool")]
        public async Task GetGrindPool(int p_Level)
        {
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
                                            if (l_SongDifficulty.characteristic == l_PlayerPassDifficulty.characteristic && l_SongDifficulty.name == l_PlayerPassDifficulty.name)
                                            {
                                                Console.WriteLine($"Pass detected on {l_Song.name} {l_SongDifficulty.name}");
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

                    int l_NumberOfDifficulties = 0;
                    foreach (var l_Song in l_Level.m_Level.songs)
                    {
                        foreach (var l_Difficulty in l_Song.difficulties)
                        {
                            l_NumberOfDifficulties++;
                        }
                    }

                    string l_PlayerTrophy = "";
                    
                    switch ((l_NumberOfPass * 100 / l_NumberOfDifficulties))
                    {
                        case <= 39:
                        {
                            l_Player.m_PlayerStats.Trophy.Plastic += 1;
                            l_PlayerTrophy = "<:plastic:874215132874571787>";
                            break;
                        }
                        case <= 69:
                        {
                            l_Player.m_PlayerStats.Trophy.Silver += 2;
                            l_PlayerTrophy = "<:silver:874215133197500446>";
                            break;
                        }
                        case <= 99:
                        {
                            l_Player.m_PlayerStats.Trophy.Gold += 3;
                            l_PlayerTrophy = "<:gold:874215133147197460>";
                            break;
                        }

                        case 100:
                        {
                            l_Player.m_PlayerStats.Trophy.Diamond += 4;
                            l_PlayerTrophy = "<:diamond:874215133289795584>";
                            break;
                        }
                    }
                    l_Player.ReWriteStats();

                    EmbedBuilder l_EmbedBuilder = new EmbedBuilder();
                    l_EmbedBuilder.WithTitle($"Maps for Level {p_Level} {l_PlayerTrophy}");
                    string l_BigGgp = null;
                    int l_Y = 0;
                    int l_NumbedOfEmbed = 1;

                    if (ConfigController.ReadConfig().BigGGP) l_BigGgp = "\n\u200B";
                    foreach (var l_Song in l_Level.m_Level.songs)
                    {
                        foreach (var l_SongDifficulty in l_Song.difficulties)
                        {
                            if (!l_Passed[l_Y])
                                l_EmbedBuilder.AddField(l_Song.name, $"{l_SongDifficulty.name} - {l_SongDifficulty.characteristic}{l_BigGgp}", true);
                            else
                                l_EmbedBuilder.AddField($"~~{l_Song.name}~~", $"~~{l_SongDifficulty.name} - {l_SongDifficulty.characteristic}~~{l_BigGgp}", true);
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

                    l_EmbedBuilder.WithFooter($"To get the playlist file: use {BotHandler.m_Prefix}getplaylist {p_Level}");
                    await Context.Channel.SendMessageAsync("", false, l_EmbedBuilder.Build());

                    l_Player.SetGrindInfo(p_Level, l_Passed, -1, l_Player.m_PlayerStats.Trophy);
                }
                catch (Exception l_Exception)
                {
                    await ReplyAsync($"> :x: Error occured : {l_Exception.Message}");
                }
            }
            else
            {
                await ReplyAsync("> :x: This level does not exist.");
            }

            UserController.UpdatePlayerLevel(Context);
        }


        [Command("scan")]
        public async Task Scan_Scores()
        {
            Player l_Player = new Player(UserController.GetPlayer(Context.User.Id.ToString()));
            int l_OldPlayerLevel = l_Player.GetPlayerLevel();
            if (!UserController.UserExist(Context.User.Id.ToString()))
                await ReplyAsync($"> :x: Sorry, you doesn't have any account linked. Please use `{BotHandler.m_Prefix}link` instead.");
            else
            {
                l_Player.FetchScores(Context);
                int l_FetchPass = await l_Player.FetchPass(Context);
                if (l_FetchPass >= 1)
                    await ReplyAsync($"> :white_check_mark: Congratulations! You passed {l_FetchPass} new maps!");
                else
                    await ReplyAsync($"> :x: Sorry, you didn't pass any new map.");
                l_Player.SetGrindInfo(-1, null, l_FetchPass, null);
            }

            if (l_OldPlayerLevel < l_Player.GetPlayerLevel())
            {
                UserController.UpdatePlayerLevel(Context);
                await ReplyAsync(
                    $"> :white_check_mark: Congratulations! You are now Level {l_Player.GetPlayerLevel()}");
            }
        }

        [Command("ping")]
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

        [Command("unlink")]
        public async Task UnLinkUser()
        {
            /// TODO: HANDLE UNLINK SPECIFIC USERS IF ADMIN
            if (string.IsNullOrEmpty(UserController.GetPlayer(Context.User.Id.ToString())))
            {
                await ReplyAsync($"> :x: Sorry, you doesn't have any account linked. Please use `{BotHandler.m_Prefix}link` instead.");
            }
            else
            {
                UserController.RemovePlayer(Context.User.Id.ToString());
                await ReplyAsync("> :white_check_mark: Your account was successfully unlinked!");
            }
        }

        [Command("link")]
        public async Task LinkUser(string p_ScoreSaberArg)
        {
            if (string.IsNullOrEmpty(UserController.GetPlayer(Context.User.Id.ToString())) && p_ScoreSaberArg.Length == 17) ///< check if id is in a correct length
            {
                /// TODO: VERIFY SCORESABER ACCOUNT
                UserController.AddPlayer(Context.User.Id.ToString(), p_ScoreSaberArg);
                await ReplyAsync($"> :white_check_mark: Your account has been successfully linked.\nLittle tip: use `{BotHandler.m_Prefix}scan` to scan your latest pass!");
            }
            else if (p_ScoreSaberArg.Length != 17)
                await ReplyAsync("> :x: Sorry, but please enter an correct scoresaber id."); ///< TODO: HANDLE SCORESABER LINKS
            else
                await ReplyAsync($"> :x: Sorry, but your account already has been linked. Please use `{BotHandler.m_Prefix}unlink`.");
        }


        [Command("help")]
        public async Task Help()
        {
            bool l_IsAdmin = false;
            if (Context.User is SocketGuildUser l_User)
                if (l_User.Roles.Any(p_Role => p_Role.Id == Controllers.ConfigController.ReadConfig().BotManagementRoleID))
                    l_IsAdmin = true;

            EmbedBuilder l_Builder = new EmbedBuilder();
            l_Builder.WithTitle("User Commands");
            l_Builder.AddField(BotHandler.m_Prefix + "help", "This message.", true);
            l_Builder.AddField(BotHandler.m_Prefix + "link **[id]**", "Links your ScoreSaber account.", true);
            l_Builder.AddField(BotHandler.m_Prefix + "unlink", "Unlinks your ScoreSaber account", true);
            l_Builder.AddField(BotHandler.m_Prefix + "ggp *[level]*", "Display the maps to grind for the [level].", true);
            l_Builder.AddField(BotHandler.m_Prefix + "scan", "Scans all your latest scores.", true);
            l_Builder.AddField(BotHandler.m_Prefix + "gpl *[level]*", "Send the playlist file. Use \"all\" to get playlist folder.", true);
            if (!l_IsAdmin)
                l_Builder.WithFooter("Bot made by Julien#1234 & Kuurama#3423");
            l_Builder.WithColor(Color.Blue);
            await Context.Channel.SendMessageAsync("", false, l_Builder.Build());

            if (l_IsAdmin)
            {
                EmbedBuilder l_ModBuilder = new EmbedBuilder();
                l_ModBuilder.WithTitle("Admins Commands");
                l_ModBuilder.AddField(BotHandler.m_Prefix + "addmap [level] [key] [Standard/Lawless..] [ExpertPlus/Hard..]", "Add a map to a level", true);
                l_ModBuilder.AddField(BotHandler.m_Prefix + "reset-config", "Reset the config file, **the bot will stop!**", true);
                l_ModBuilder.AddField(BotHandler.m_Prefix + "unlink **[player]**", "TODO: Unlinks the ScoreSaber account of a player", true);
                l_ModBuilder.WithColor(Color.Red);
                l_ModBuilder.WithFooter("Bot made by Julien#1234 & Kuurama#3423");
                await Context.Channel.SendMessageAsync("", false, l_ModBuilder.Build());
            }
        }
    }

    class CheckChannelAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext p_Context, CommandInfo p_Command, IServiceProvider p_Services)
        {
            foreach (var l_AuthorizedChannel in ConfigController.ReadConfig().AuthorizedChannels)
            {
                if (p_Context.Message.Channel.Id == l_AuthorizedChannel)
                {
                    return Task.FromResult(PreconditionResult.FromSuccess());
                }
            }
            return Task.FromResult(PreconditionResult.FromError("> :x: Sorry, you can't use this command here."));
        }
    }
}