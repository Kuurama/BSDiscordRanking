using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using BSDiscordRanking.Formats;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace BSDiscordRanking.Discord.Modules
{
    [RequireManagerRole]
    public class AdminModule : ModuleBase<SocketCommandContext>
    {
        [Command("loggingchannel")]
        [Alias("logchannel")]
        public async Task SetChannel()
        {
                ConfigController.ReadConfig();
                ConfigController.m_ConfigFormat.LoggingChannel = Context.Channel.Id;
                ConfigController.ReWriteConfig();
                ReplyAsync("> :white_check_mark: This channel is now used as log-channel.");
        }
        
        [Command("addchannel")]
        public async Task AddChannel()
        {
            ConfigController.m_ConfigFormat.AuthorizedChannels ??= new List<ulong>();
            if (ConfigController.m_ConfigFormat.AuthorizedChannels.Any(l_Channel => Context.Message.Channel.Id == l_Channel))
            {
                await ReplyAsync("> :x: Sorry, this channel can already be used for user commands");
                return;
            }
            ConfigController.m_ConfigFormat.AuthorizedChannels.Add(Context.Message.Channel.Id);
            ConfigController.ReWriteConfig();
            await ReplyAsync("> :white_check_mark: This channel can now be used for user commands");
        }
        
        [Command("removechannel")]
        public async Task RemoveChannel()
        {
            foreach (var l_Channel in ConfigController.m_ConfigFormat.AuthorizedChannels.Where(l_Channel => Context.Message.Channel.Id == l_Channel))
            {
                ConfigController.m_ConfigFormat.AuthorizedChannels.Remove(l_Channel);
            }
            
            ConfigController.ReWriteConfig();
            await ReplyAsync("> :white_check_mark: This channel cannot longer be used for user commands");
        }
        
        [Command("createroles")]
        public async Task CreateRoles()
        {
            new RoleController().CreateAllRoles(Context, false);
        }
        
        [Command("addmap")]
        public async Task AddMap(int p_Level = 0, string p_Code = "", string p_DifficultyName = "", string p_Characteristic = "Standard")
        {
            if (p_Level <= 0 || string.IsNullOrEmpty(p_Code) || string.IsNullOrEmpty(p_Characteristic) ||
                string.IsNullOrEmpty(p_DifficultyName))
            {
                await ReplyAsync(
                    $"> :x: Seems like you didn't used the command correctly, use: `{BotHandler.m_Prefix}addmap [level] [key] [ExpertPlus/Hard..] (Standard/Lawless..)`");
            }
            else
            {
                if (p_Characteristic is "Lawless" or "Standard" or "90Degree" or "360Degree")
                {
                    if (p_DifficultyName is "Easy" or "Normal" or "Hard" or "Expert" or "ExpertPlus")
                    {
                        Level l_Level = new Level(p_Level);
                        l_Level.AddMap(p_Code, p_Characteristic, p_DifficultyName, Context);
                        if (!l_Level.m_MapAdded)
                        {
                            BeatSaverFormat l_Map = Level.FetchBeatMap(p_Code, Context);
                            EmbedBuilder l_EmbedBuilder = new EmbedBuilder();
                            l_EmbedBuilder.WithTitle("Map added!");
                            l_EmbedBuilder.AddField("Map name:", l_Map.name);
                            l_EmbedBuilder.AddField("Difficulty:", p_Characteristic + " - " + p_DifficultyName);
                            l_EmbedBuilder.AddField("Level:", p_Level);
                            l_EmbedBuilder.WithFooter("Operated by " + Context.User.Username);
                            l_EmbedBuilder.WithThumbnailUrl($"https://cdn.beatsaver.com/{l_Map.versions[^1].hash.ToLower()}.jpg");
                            l_EmbedBuilder.WithColor(Color.Blue);
                            await Context.Guild.GetTextChannel(ConfigController.ReadConfig().LoggingChannel)
                                .SendMessageAsync("", false, l_EmbedBuilder.Build());
                        }
                    }
                    else
                        await ReplyAsync(
                            "> :x: Seems like you didn't entered the difficulty name correctly. Use: \"`Easy,Normal,Hard,Expert or ExpertPlus`\"");
                }
                else
                    await ReplyAsync(
                        "> :x: Seems like you didn't entered the characteristic name correctly. Use: \"`Standard,Lawless,90Degree or 360Degree`\"");
            }
        }
        
        [Command("removemap")]
        public async Task RemoveMap(int p_Level = 0, string p_Code = "", string p_DifficultyName = "", string p_Characteristic = "Standard")
        {
            if (p_Level <= 0 || string.IsNullOrEmpty(p_Code) || string.IsNullOrEmpty(p_Characteristic) ||
                string.IsNullOrEmpty(p_DifficultyName))
            {
                await ReplyAsync(
                    $"> :x: Seems like you didn't used the command correctly, use: `{BotHandler.m_Prefix}removemap [level] [key] [ExpertPlus/Hard..] (Standard/Lawless..)`");
            }
            else
            {
                if (p_Characteristic is "Lawless" or "Standard" or "90Degree" or "360Degree")
                {
                    if (p_DifficultyName is "Easy" or "Normal" or "Hard" or "Expert" or "ExpertPlus")
                    {
                        Level l_Level = new Level(p_Level);
                        l_Level.RemoveMap(p_Code, p_Characteristic, p_DifficultyName, Context);
                        if (!l_Level.m_MapAdded)
                        {
                            BeatSaverFormat l_Map = Level.FetchBeatMap(p_Code, Context);
                            EmbedBuilder l_EmbedBuilder = new EmbedBuilder();
                            l_EmbedBuilder.WithTitle("Map removed!");
                            l_EmbedBuilder.AddField("Map name:", l_Map.name);
                            l_EmbedBuilder.AddField("Difficulty:", p_Characteristic + " - " + p_DifficultyName);
                            l_EmbedBuilder.AddField("Level:", p_Level);
                            l_EmbedBuilder.WithFooter("Operated by " + Context.User.Username);
                            l_EmbedBuilder.WithThumbnailUrl($"https://cdn.beatsaver.com/{l_Map.versions[^1].hash.ToLower()}.jpg");
                            l_EmbedBuilder.WithColor(Color.Red);
                            await Context.Guild.GetTextChannel(ConfigController.ReadConfig().LoggingChannel)
                                .SendMessageAsync("", false, l_EmbedBuilder.Build());
                        }
                        
                        if (l_Level.m_Level.songs.Count == 0)
                        {
                            l_Level.DeleteLevel();
                            if (!l_Level.m_MapAdded)
                            {
                                BeatSaverFormat l_Map = Level.FetchBeatMap(p_Code, Context);
                                EmbedBuilder l_EmbedBuilder = new EmbedBuilder();
                                l_EmbedBuilder.WithTitle("Level Removed!");
                                l_EmbedBuilder.AddField("Level:", p_Level);
                                l_EmbedBuilder.AddField("Reason:", "All maps has been removed.");
                                l_EmbedBuilder.WithFooter("Operated by " + Context.User.Username);
                                l_EmbedBuilder.WithColor(Color.DarkRed);
                                await Context.Guild.GetTextChannel(ConfigController.ReadConfig().LoggingChannel)
                                    .SendMessageAsync("", false, l_EmbedBuilder.Build());
                            }
                        }

                    }
                    else
                        await ReplyAsync(
                            "> :x: Seems like you didn't entered the difficulty name correctly. Use: \"`Easy,Normal,Hard,Expert or ExpertPlus`\"");
                }
                else
                    await ReplyAsync(
                        "> :x: Seems like you didn't entered the characteristic name correctly. Use: \"`Standard,Lawless,90Degree or 360Degree`\"");
            }
        }

        [Command("reset-config")]
        public async Task Reset_config()
        {
            await ReplyAsync("> :white_check_mark: After the bot finished to reset the config, it will stops.");
            ConfigController.CreateConfig();
        }
        
        private class RequireManagerRoleAttribute : PreconditionAttribute
        {
            public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext p_Context, CommandInfo p_Command, IServiceProvider p_Services)
            {
                if (p_Context.User is SocketGuildUser l_User)
                {
                    if (l_User.Roles.Any(p_Role => p_Role.Id == Controllers.ConfigController.ReadConfig().BotManagementRoleID))
                    {
                        return Task.FromResult(PreconditionResult.FromSuccess());
                    }
                }
                return Task.FromResult(PreconditionResult.FromError("> :x: Sorry, you don't have the permission to access admin commands."));
            }
        }
    }
}