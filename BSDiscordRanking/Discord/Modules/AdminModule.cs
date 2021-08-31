using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using BSDiscordRanking.Formats;
using Discord;
using Discord.Commands;
using Discord.Rest;
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
            ConfigController.GetConfig();
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
        [Command("allowuser")]
        public async Task AllowUser(ulong p_DiscordID)
        {
            UserController.GiveBSDRRole(p_DiscordID, Context);
        }

        [Command("addmap")]
        public async Task AddMap(int p_Level = 0, string p_Code = "", string p_DifficultyName = "", string p_Characteristic = "Standard", int p_MinScoreRequirement = 0)
        {
            if (p_Level <= 0 || string.IsNullOrEmpty(p_Code) || string.IsNullOrEmpty(p_Characteristic) ||
                string.IsNullOrEmpty(p_DifficultyName))
            {
                await ReplyAsync($"> :x: Seems like you didn't used the command correctly, use: `{BotHandler.m_Prefix}addmap [level] [key] [ExpertPlus/Hard..] (Standard/Lawless..)`");
            }
            else
            {
                if (p_Characteristic is "Lawless" or "Standard" or "90Degree" or "360Degree")
                {
                    if (p_DifficultyName is "Easy" or "Normal" or "Hard" or "Expert" or "ExpertPlus")
                    {
                        Level l_Level = new Level(p_Level);
                        BeatSaverFormat l_Map = Level.FetchBeatMap(p_Code, Context);
                        LevelController.MapExistFormat l_MapExistCheck = new LevelController().MapExist_DifferentMinScore(l_Map.versions[^1].hash, p_DifficultyName, p_Characteristic, p_MinScoreRequirement);
                        if (!l_MapExistCheck.MapExist && !l_MapExistCheck.DifferentMinScore)
                        {
                            l_Level.AddMap(p_Code, p_Characteristic, p_DifficultyName, p_MinScoreRequirement, Context);
                            if (!l_Level.m_MapAdded)
                            {
                                EmbedBuilder l_EmbedBuilder = new EmbedBuilder();
                                l_EmbedBuilder.WithTitle("Map Added:");
                                l_EmbedBuilder.WithDescription(l_Map.name);
                                l_EmbedBuilder.AddField("Difficulty:", p_Characteristic + " - " + p_DifficultyName, true);
                                l_EmbedBuilder.AddField("Level:", p_Level, true);
                                l_EmbedBuilder.AddField("ScoreRequirement:", p_MinScoreRequirement, true);
                                l_EmbedBuilder.AddField("Link:", $"https://beatsaver.com/maps/{l_Map.versions[^1].key}", false);
                                l_EmbedBuilder.WithFooter("Operated by " + Context.User.Username);
                                l_EmbedBuilder.WithThumbnailUrl($"https://cdn.beatsaver.com/{l_Map.versions[^1].hash.ToLower()}.jpg");
                                l_EmbedBuilder.WithColor(Color.Blue);
                                await Context.Guild.GetTextChannel(ConfigController.GetConfig().LoggingChannel).SendMessageAsync("", false, l_EmbedBuilder.Build());
                            }
                        }
                        else if (l_MapExistCheck.DifferentMinScore)
                        {
                            l_Level.AddMap(p_Code, p_Characteristic, p_DifficultyName, p_MinScoreRequirement, Context);
                            EmbedBuilder l_EmbedBuilder = new EmbedBuilder();
                            l_EmbedBuilder.WithTitle("Min Score Requirement Edited on:");
                            l_EmbedBuilder.WithDescription(l_Map.name);
                            l_EmbedBuilder.AddField("Difficulty:", p_Characteristic + " - " + p_DifficultyName, true);
                            l_EmbedBuilder.AddField("Level:", p_Level, true);
                            l_EmbedBuilder.AddField("New ScoreRequirement:", p_MinScoreRequirement, true);
                            l_EmbedBuilder.AddField("Link:", $"https://beatsaver.com/maps/{l_Map.versions[^1].key}", false);
                            l_EmbedBuilder.WithFooter("Operated by " + Context.User.Username);
                            l_EmbedBuilder.WithThumbnailUrl($"https://cdn.beatsaver.com/{l_Map.versions[^1].hash.ToLower()}.jpg");
                            l_EmbedBuilder.WithColor(Color.Blue);
                            await Context.Guild.GetTextChannel(ConfigController.GetConfig().LoggingChannel).SendMessageAsync("", false, l_EmbedBuilder.Build());
                        }
                        else
                        {
                            await ReplyAsync($"> :x: Sorry, this map & difficulty already exist into level {l_MapExistCheck.Level}.");
                        }
                    }
                    else
                        await ReplyAsync("> :x: Seems like you didn't entered the characteristic name correctly. Use: \"`Standard,Lawless,90Degree or 360Degree`\"");
                }
                else
                    await ReplyAsync("> :x: Seems like you didn't entered the difficulty name correctly. Use: \"`Easy,Normal,Hard,Expert or ExpertPlus`\"");
            }
        }

        [Command("removemap")]
        public async Task RemoveMap(string p_Code = "", string p_DifficultyName = "", string p_Characteristic = "Standard")
        {
            if (string.IsNullOrEmpty(p_Code) || string.IsNullOrEmpty(p_Characteristic) ||
                string.IsNullOrEmpty(p_DifficultyName))
            {
                await ReplyAsync(
                    $"> :x: Seems like you didn't used the command correctly, use: `{BotHandler.m_Prefix}removemap [key] [ExpertPlus/Hard..] (Standard/Lawless..)`");
            }
            else
            {
                if (p_Characteristic is "Lawless" or "Standard" or "90Degree" or "360Degree")
                {
                    if (p_DifficultyName is "Easy" or "Normal" or "Hard" or "Expert" or "ExpertPlus")
                    {
                        BeatSaverFormat l_Map = Level.FetchBeatMap(p_Code, Context);
                        LevelController.MapExistFormat l_MapExistCheck = new LevelController().MapExist_DifferentMinScore(l_Map.versions[^1].hash, p_DifficultyName, p_Characteristic, 0);
                        if (l_MapExistCheck.Level >= 0)
                        {
                            Level l_Level = new Level(l_MapExistCheck.Level);
                            l_Level.RemoveMap(p_Code, p_Characteristic, p_DifficultyName, Context);

                            l_Map = Level.FetchBeatMap(p_Code, Context);
                            EmbedBuilder l_EmbedBuilder = new EmbedBuilder();
                            if (l_Level.m_Level.songs.Count == 0)
                            {
                                l_Level.DeleteLevel();
                                l_EmbedBuilder.WithTitle("Level Removed!");
                                l_EmbedBuilder.AddField("Level:", l_MapExistCheck.Level);
                                l_EmbedBuilder.AddField("Reason:", "All maps has been removed.");
                            }
                            else
                            {
                                l_EmbedBuilder.WithTitle("Map removed!");
                                l_EmbedBuilder.AddField("Map name:", l_Map.name);
                                l_EmbedBuilder.AddField("Difficulty:", p_Characteristic + " - " + p_DifficultyName);
                                l_EmbedBuilder.AddField("Level:", l_MapExistCheck.Level);
                            }

                            l_EmbedBuilder.WithFooter("Operated by " + Context.User.Username);
                            l_EmbedBuilder.WithThumbnailUrl($"https://cdn.beatsaver.com/{l_Map.versions[^1].hash.ToLower()}.jpg");
                            l_EmbedBuilder.WithColor(Color.Red);
                            await Context.Guild.GetTextChannel(ConfigController.GetConfig().LoggingChannel).SendMessageAsync("", false, l_EmbedBuilder.Build());
                        }
                        else
                        {
                            await ReplyAsync($"> :x: Sorry, this map difficulty isn't in any levels.");
                        }
                    }
                    else
                        await ReplyAsync("> :x: Seems like you didn't entered the characteristic name correctly. Use: \"`Standard,Lawless,90Degree or 360Degree`\"");
                }
                else
                    await ReplyAsync("> :x: Seems like you didn't entered the difficulty name correctly. Use: \"`Easy,Normal,Hard,Expert or ExpertPlus`\"");
            }
        }

        [Command("resetscorerequirement")]
        public async Task ResetScoreRequirement(int p_Level)
        {
            if (p_Level >= 0)
            {
                new Level(p_Level).ResetScoreRequirement();
                await ReplyAsync($"> :white_check_mark: All maps in playlist {p_Level} have now a score requirement of 0");
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
                    ulong l_RoleID = ConfigController.GetConfig().BotManagementRoleID;
                    if (l_User.Roles.Any(p_Role => p_Role.Id == l_RoleID))
                    {
                        return Task.FromResult(PreconditionResult.FromSuccess());
                    }
                }

                return Task.FromResult(PreconditionResult.FromError(ExecuteResult.FromError(new Exception(ErrorMessage = "Incorrect user's permissions"))));
            }
        }
    }
}