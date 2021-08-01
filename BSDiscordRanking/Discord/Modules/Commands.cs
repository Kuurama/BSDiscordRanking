using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using Discord;
using Discord.Commands;
using BeatSaverSharp;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace BSDiscordRanking.Discord.Modules
{
    public class Commands : ModuleBase<SocketCommandContext>
    {
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
                ZipFile.CreateFromDirectory("./Levels/", "levels.zip");
                await Context.Channel.SendFileAsync("levels.zip", "> :white_check_mark: Here's your playlist folder!");
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
                EmbedBuilder l_EmbedBuilder = new EmbedBuilder();
                l_EmbedBuilder.WithTitle($"Maps for Level {p_Level}");
                try
                {
                    List<bool> l_Passed = new List<bool>();
                    var l_PlayerPasses = JsonSerializer.Deserialize<PlayerPassFormat>(File.ReadAllText("./Players/" + UserController.GetPlayer(Context.User.Id.ToString()) + "/pass.json"));
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

                    int l_Y = 0;
                    foreach (var l_Song in l_Level.m_Level.songs)
                    {
                        foreach (var l_SongDifficulty in l_Song.difficulties)
                        {
                            if (!l_Passed[l_Y])
                            {
                                l_EmbedBuilder.AddField(l_Song.name, $"{l_SongDifficulty.name} - {l_SongDifficulty.characteristic}", true);
                            }
                            else
                            {
                                l_EmbedBuilder.AddField($"~~{l_Song.name}~~", $"~~{l_SongDifficulty.name} - {l_SongDifficulty.characteristic}~~", true);
                            }

                            l_Y++;
                        }
                    }

                    l_EmbedBuilder.WithFooter($"To get the playlist file: use {BotHandler.m_Prefix}getplaylist {p_Level}");
                    await Context.Channel.SendMessageAsync("", false, l_EmbedBuilder.Build());
                }
                catch (Exception l_Exception)
                {
                    await ReplyAsync($"> :x: Please scan first? Error occured : {l_Exception.Message}");
                }
            }
            else
            {
                await ReplyAsync("> :x: This level does not exist.");
            }
        }

        [Command("addmap")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task AddMap(int p_Level = 0, string p_Key = "", string p_Characteristic = "", string p_DifficultyName = "")
        {
            if (p_Level <= 0 || string.IsNullOrEmpty(p_Key) || string.IsNullOrEmpty(p_Characteristic) || string.IsNullOrEmpty(p_DifficultyName))
            {
                await ReplyAsync($"> :x: Seems like you didn't use the command correctly, use: `{BotHandler.m_Prefix}addmap [level] [key] [Standard/Lawless..] [ExpertPlus/Hard..] `");
            }
            else
            {
                if (p_Characteristic == "Lawless" || p_Characteristic == "Standard" || p_Characteristic == "90Degree" || p_Characteristic == "360Degree")
                {
                    if (p_DifficultyName == "Easy" || p_DifficultyName == "Normal" ||
                        p_DifficultyName == "Hard" || p_DifficultyName == "Expert" ||
                        p_DifficultyName == "ExpertPlus")
                    {
                        HttpOptions l_Options = new HttpOptions("BSRanking", new Version(1, 0, 0));
                        string l_Hash = null;
                        try
                        {
                            var l_Beatmap = new BeatSaver(l_Options).Key(p_Key).Result;
                            if (l_Beatmap is not null) l_Hash = l_Beatmap.Hash;
                        }
                        catch (Exception l_E)
                        {
                            await ReplyAsync($"> :x: Seems like BeatSaver didn't responded, the **key** might be wrong? : {l_E.Message}");
                        }

                        if (!string.IsNullOrEmpty(l_Hash))
                            new Level(p_Level).AddMap(l_Hash, p_Characteristic, p_DifficultyName, Context);
                    }

                    else
                        await ReplyAsync(
                            $"> :x: Seems like you didn't entered the difficulty name correctly. Use: \"`Easy,Normal,Hard,Expert or ExpertPlus`\"");
                }
                else
                    await ReplyAsync(
                        $"> :x: Seems like you didn't entered the characteristic name correctly. Use: \"`Standard,Lawless,90Degree or 360Degree`\"");
            }
        }

        [Command("scan")]
        public async Task Scan_Scores()
        {
            if (!UserController.UserExist(Context.User.Id.ToString()))
                await ReplyAsync($"> :x: Sorry, you doesn't have any account linked. Please use `{BotHandler.m_Prefix}link` instead.");
            else
            {
                Player l_Player = new Player(UserController.GetPlayer(Context.User.Id.ToString()));
                l_Player.FetchScores(Context);
                int l_FetchPass = await l_Player.FetchPass(Context);
                if (l_FetchPass >= 1)
                    await ReplyAsync($"> :white_check_mark: Congratulations! You passed {l_FetchPass} new maps!");
                else
                    await ReplyAsync($"> :x: Sorry, you didn't passed any new map.");
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


        [Command("reset-config")]
        [RequireOwner]
        public async Task Reset_config()
        {
            await ReplyAsync("> :white_check_mark: After the bot finished to reset the config, it will stops.");
            ConfigController.CreateConfig();
        }

        [Command("help")]
        public async Task Help()
        {
            EmbedBuilder l_Builder = new EmbedBuilder();
            l_Builder.WithTitle("User Commands");
            l_Builder.AddField(BotHandler.m_Prefix + "help", "This message.", true);
            l_Builder.AddField(BotHandler.m_Prefix + "link **[id]**", "Links your ScoreSaber account.", true);
            l_Builder.AddField(BotHandler.m_Prefix + "unlink", "Unlinks your ScoreSaber account", true);
            l_Builder.AddField(BotHandler.m_Prefix + "ggp *[level]*", "Shows you the maps of your level.", true);
            l_Builder.AddField(BotHandler.m_Prefix + "scan", "Scans all your latest scores.", true);
            l_Builder.AddField(BotHandler.m_Prefix + "gpl *[level]*", "Send the playlist file. Use \"all\" to get playlist folder.", true);
            l_Builder.WithColor(Color.Blue);
            await Context.Channel.SendMessageAsync("", false, l_Builder.Build());

            EmbedBuilder l_ModBuilder = new EmbedBuilder();
            l_ModBuilder.WithTitle("Admins Commands");
            l_ModBuilder.AddField(BotHandler.m_Prefix + "addmap [level] [key] [Standard/Lawless..] [ExpertPlus/Hard..]", "Add a map to a level", true);
            l_ModBuilder.AddField(BotHandler.m_Prefix + "reset-config", "Owner only: Reset the config file, **the bot will stop!**", true);
            l_ModBuilder.AddField(BotHandler.m_Prefix + "unlink **[player]**", "TODO: Unlinks the ScoreSaber account of a player", true);
            l_ModBuilder.WithColor(Color.Red);
            l_ModBuilder.WithFooter("Bot made by Julien#1234 & Kuurama#3423");
            await Context.Channel.SendMessageAsync("", false, l_ModBuilder.Build());
        }
    }
}