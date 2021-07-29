using System;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using BSDiscordRanking.Controllers;
using Discord;
using Discord.Commands;
using BeatSaverSharp;
using Newtonsoft.Json.Serialization;

namespace BSDiscordRanking.Discord.Modules
{
    public class Commands : ModuleBase<SocketCommandContext>
    {
        [Command("getplaylist")]
        [Alias("gpl")]
        public async Task GetPlaylist(string p_level)
        {
            int p_levelID;
            if (int.TryParse(p_level, out p_levelID))
            {
                // TODO: check if playlist exist
                await Context.Channel.SendFileAsync(Level.GetPath() + $"/{p_level}{Level.SUFFIX_NAME}.bplist", "> :white_check_mark: Here's your playlist!");
            }
            else if (p_level == "all")
            {
                foreach (var l_levelID in new LevelController().m_LevelController.LevelID)
                {
                    
                }
                await Context.Channel.SendFileAsync("", "> :white_check_mark: Here's your playlist folder!");
            }
            else
            {
                await ReplyAsync("> :x: Wrong argument, please use \"1,2,3..\" or \"all\"");
            }


        }
        
        [Command("ggp")]
        public async Task GetGrindPool(int p_level)
        {
            if (p_level >= 0)
            {
                Level l_level = new Level(p_level);
                EmbedBuilder l_embedBuilder = new EmbedBuilder();
                l_embedBuilder.WithTitle($"Maps for Level {p_level}");
                foreach (var l_song in l_level.m_Level.songs)
                {
                    foreach (var l_difficulty in l_song.difficulties)
                    {
                        l_embedBuilder.AddField(l_song.name,$"{l_difficulty.name} - {l_difficulty.characteristic}", true);
                    }
                }
                l_embedBuilder.WithFooter($"To get the playlist file: use {BotHandler.m_Prefix}getplaylist {p_level}");
                await Context.Channel.SendMessageAsync("", false, l_embedBuilder.Build());
                
            }
            else
            {
                await ReplyAsync("> :x: Please enter a correct level number.");
            }
        }

        [Command("addmap")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task AddMap(int p_level = 0, string p_key = "", string p_characteristic = "", string p_difficultyName = "")
        {
            if (p_level <= 0 || string.IsNullOrEmpty(p_key) || string.IsNullOrEmpty(p_characteristic) || string.IsNullOrEmpty(p_difficultyName))
            {
                ReplyAsync($"> :x: Seems like you didn't use the command correctly, use: `{BotHandler.m_Prefix}addmap [level] [key] [Standard/Lawless..] [ExpertPlus/Hard..] `");
            }
            else
            {
                if (p_characteristic == "Lawless" || p_characteristic == "Standard" || p_characteristic == "90Degree" || p_characteristic == "360Degree")
                {
                    if (p_difficultyName == "Easy" || p_difficultyName == "Normal" ||
                        p_difficultyName == "Hard" || p_difficultyName == "Expert" ||
                        p_difficultyName == "ExpertPlus")
                    {
                        HttpOptions l_options = new HttpOptions("BSRanking", new Version(1, 0, 0));
                        string l_hash = null;
                        try
                        {
                            l_hash = new BeatSaver(l_options).Key(p_key).Result.Hash;
                        }
                        catch (Exception l_e)
                        {
                            await ReplyAsync($"> :x: Seems like BeatSaver didn't responded, the **key** might be wrong?");
                        }
                        if (!string.IsNullOrEmpty(l_hash))
                            new Level(p_level).AddMap(l_hash, p_characteristic, p_difficultyName, Context);
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
                Player l_player = new Player(UserController.GetPlayer(Context.User.Id.ToString()));
                l_player.FetchScores(Context);
                int l_fetchPass = await l_player.FetchPass(Context);
                if (l_fetchPass >= 1)
                    await ReplyAsync($"> :white_check_mark: Congratulations! You passed {l_fetchPass} new maps!");
                else
                    await ReplyAsync($"> :x: Sorry, you didn't passed any new map.");
            }
        }

        [Command("ping")]
        public async Task Ping()
        {
            EmbedBuilder l_embedBuilder = new EmbedBuilder();
            l_embedBuilder.AddField("Discord: ", new Ping().Send("discord.com").RoundtripTime + "ms");
            l_embedBuilder.AddField("ScoreSaber: ", new Ping().Send("scoresaber.com").RoundtripTime + "ms");
            l_embedBuilder.WithFooter("#LoveArche",
                "https://images.genius.com/d4b8905048993e652aba3d8e105b5dbf.1000x1000x1.jpg");
            l_embedBuilder.WithColor(Color.Blue);
            await Context.Channel.SendMessageAsync("", false, l_embedBuilder.Build());
        }

        [Command("unlink")]
        public async Task UnLinkUser()
        {
            /// TODO: HANDLE UNLINK SPECIFIC USERS IF ADMIN
            if (string.IsNullOrEmpty(UserController.GetPlayer(Context.User.Id.ToString())))
            {
                ReplyAsync($"> :x: Sorry, you doesn't have any account linked. Please use `{BotHandler.m_Prefix}link` instead.");
            }
            else
            {
                UserController.RemovePlayer(Context.User.Id.ToString());
                ReplyAsync("> :white_check_mark: Your account was successfully unlinked!");
            }

        }
        
        [Command("link")]
        public async Task LinkUser(string p_scoreSaberArg)
        {
            if (string.IsNullOrEmpty(UserController.GetPlayer(Context.User.Id.ToString())) && p_scoreSaberArg.Length == 17) ///< check if id is in a correct length
            {
                /// TODO: VERIFY SCORESABER ACCOUNT
                UserController.AddPlayer(Context.User.Id.ToString(), p_scoreSaberArg);
                await ReplyAsync($"> :white_check_mark: Your account has been successfully linked.\nLittle tip: use `{BotHandler.m_Prefix}scan` to scan your latest pass!");
            }
            else if(p_scoreSaberArg.Length != 17)
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
            l_Builder.AddField(BotHandler.m_Prefix + "", "do something", true);
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