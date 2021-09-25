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
        [Summary("Set this channel as a logs's message sending channel (Map Added/Deleted etc).")]
        public async Task SetChannel()
        {
            ConfigController.GetConfig();
            ConfigController.m_ConfigFormat.LoggingChannel = Context.Channel.Id;
            ConfigController.ReWriteConfig();
            await ReplyAsync("> :white_check_mark: This channel is now used as log-channel.");
        }


        [Command("addchannel")]
        [Summary("Allow the bot to answer player's commands in this channel.")]
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
        [Summary("Remove the bot's permission to answer player's commands in this channel.")]
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
        [Summary("Creates or updates level's roles. (discord roles)")]
        public async Task CreateRoles()
        {
            await new RoleController().CreateAllRoles(Context, false);
        }

        [Command("allowuser")]
        [Summary("Gives user the matching Ranked role.")]
        public async Task AllowUser(ulong p_DiscordID)
        {
            if (UserController.GiveRemoveBSDRRole(p_DiscordID, Context, false))
            {
                await ReplyAsync(
                    $"'{ConfigController.GetConfig().RolePrefix} Ranked' Role added to user <@{p_DiscordID}>,{Environment.NewLine}You might want to **check the pins** for *answers*, use the ``{ConfigController.GetConfig().CommandPrefix[0]}getstarted`` command to get started.\nps: if you don't like being here you can still ask to be removed.");
            }
            else
            {
                await ReplyAsync($"This player can't be found/already have the '{ConfigController.GetConfig().RolePrefix} Ranked' Role.");
            }
        }

        [Command("rejectuser")]
        [Summary("Removes user it's matching Ranked role.")]
        public async Task RemoveUser(ulong p_DiscordID)
        {
            if (UserController.GiveRemoveBSDRRole(p_DiscordID, Context, true))
            {
                await ReplyAsync($"'{ConfigController.GetConfig().RolePrefix} Ranked' Role removed from user <@{p_DiscordID}>.");
            }
            else
            {
                await ReplyAsync($"This player can't be found/do not have the '{ConfigController.GetConfig().RolePrefix} Ranked' Role.");
            }
        }

        private int ScoreFromAcc(float p_Acc = 0f, int p_NoteCount = 0)
        {
            /// Made by MoreOwO :3

            /// Calculate maxScore

            int l_MaxScore;

            switch (p_NoteCount)
            {
                case <= 0:
                    return 0;
                case 1:
                    l_MaxScore = 115;
                    break;
                case <= 5:
                    l_MaxScore = 115 + ((p_NoteCount - 1) * 2 * 115);
                    break;

                case <13:
                    l_MaxScore = 1035 + ((p_NoteCount - 5) * 4 * 115);
                    break;
                case 13:
                    l_MaxScore = 4715;
                    break;
                case > 13:
                    l_MaxScore = (p_NoteCount * 8 * 115) - 7245;
                    break;
            }

            if ((int)p_Acc == 0)
                return 0;

            return (int)Math.Round(l_MaxScore * (p_Acc / 100));
        }

        [Command("addmap")]
        [Summary("Adds a map or updates it from a desired level.")]
        public async Task AddMap(int p_Level = 0, string p_Code = "", string p_DifficultyName = "", string p_Characteristic = "Standard", float p_MinPercentageRequirement = 0f)
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
                        int l_NumberOfNote = 0;
                        bool l_DiffExist = false;
                        foreach (var l_Diff in l_Map.versions[^1].diffs)
                        {
                            if (l_Diff.characteristic == p_Characteristic && l_Diff.difficulty == p_DifficultyName)
                            {
                                l_NumberOfNote = l_Diff.notes;
                                l_DiffExist = true;
                            }
                        }

                        if (l_DiffExist)
                        {
                            LevelController.MapExistFormat l_MapExistCheck = new LevelController().MapExist_DifferentMinScore(l_Map.versions[^1].hash, p_DifficultyName, p_Characteristic, ScoreFromAcc(p_MinPercentageRequirement, l_NumberOfNote));
                            if (!l_MapExistCheck.MapExist && !l_MapExistCheck.DifferentMinScore)
                            {
                                l_Level.AddMap(p_Code, p_Characteristic, p_DifficultyName, ScoreFromAcc(p_MinPercentageRequirement, l_NumberOfNote), Context);
                                if (!l_Level.m_MapAdded)
                                {
                                    EmbedBuilder l_EmbedBuilder = new EmbedBuilder();
                                    l_EmbedBuilder.WithTitle("Map Added:");
                                    l_EmbedBuilder.WithDescription(l_Map.name);
                                    l_EmbedBuilder.AddField("Difficulty:", p_Characteristic + " - " + p_DifficultyName, true);
                                    l_EmbedBuilder.AddField("Level:", p_Level, true);
                                    l_EmbedBuilder.AddField("ScoreRequirement:", $"{p_MinPercentageRequirement}% ({ScoreFromAcc(p_MinPercentageRequirement, l_NumberOfNote)})", true);
                                    l_EmbedBuilder.AddField("Link:", $"https://beatsaver.com/maps/{l_Map.versions[^1].key}", false);
                                    l_EmbedBuilder.WithFooter("Operated by " + Context.User.Username);
                                    l_EmbedBuilder.WithThumbnailUrl($"https://cdn.beatsaver.com/{l_Map.versions[^1].hash.ToLower()}.jpg");
                                    l_EmbedBuilder.WithColor(Color.Blue);
                                    await Context.Guild.GetTextChannel(ConfigController.GetConfig().LoggingChannel).SendMessageAsync("", false, l_EmbedBuilder.Build());
                                }
                            }
                            else if (l_MapExistCheck.DifferentMinScore)
                            {
                                l_Level.AddMap(p_Code, p_Characteristic, p_DifficultyName, ScoreFromAcc(p_MinPercentageRequirement, l_NumberOfNote), Context);
                                EmbedBuilder l_EmbedBuilder = new EmbedBuilder();
                                l_EmbedBuilder.WithTitle("Min Score Requirement Edited on:");
                                l_EmbedBuilder.WithDescription(l_Map.name);
                                l_EmbedBuilder.AddField("Difficulty:", p_Characteristic + " - " + p_DifficultyName, true);
                                l_EmbedBuilder.AddField("Level:", p_Level, true);
                                l_EmbedBuilder.AddField("New ScoreRequirement:", $"{p_MinPercentageRequirement}% ({ScoreFromAcc(p_MinPercentageRequirement, l_NumberOfNote)})", true);
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
                        {
                            await Context.Channel.SendMessageAsync($"The diff {p_DifficultyName} - {p_Characteristic} doesn't exist in this BeatMap", false);
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
        [Summary("Searches then removes a map difficulty from the levels.")]
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
                        if (l_MapExistCheck.MapExist)
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
                        await ReplyAsync("> :x: Seems like you didn't entered the difficulty name correctly. Use: \"`Easy,Normal,Hard,Expert or ExpertPlus`\"");
                }
                else
                    await ReplyAsync("> :x: Seems like you didn't entered the characteristic name correctly. Use: \"`Standard,Lawless,90Degree or 360Degree`\"");
            }
        }

        [Command("unlink")]
        [Summary("Unlinks your discord accounts from your ScoreSaber's one.")]
        public async Task UnLinkUser(string p_DiscordID = "")
        {
            if (!String.IsNullOrEmpty(p_DiscordID))
            {
                if (UserController.UserExist(p_DiscordID))
                {
                    if (Context.User is SocketGuildUser l_User)
                    {
                        if (l_User.Roles.Any(p_Role => p_Role.Id == Controllers.ConfigController.GetConfig().BotManagementRoleID))
                        {
                            UserController.RemovePlayer(p_DiscordID);
                            await ReplyAsync($"> :white_check_mark: Player <@{p_DiscordID}> was successfully unlinked!");
                        }
                    }
                }
                else
                {
                    await ReplyAsync($"> :x: Sorry but this account isn't registered, and as a result, can't be unlinked.");
                }
            }
        }

        [Command("setlevel")]
        [Summary("Set a specific Level to someone + change his stats/roles and leaderboard level.")]
        public async Task SetLevelRole(string p_DiscordOrScoreSaberID, int p_Level)
        {
            bool l_IsScoreSaberAccount = UserController.AccountExist(p_DiscordOrScoreSaberID);

            string l_DiscordID = null;
            
            if (UserController.UserExist(p_DiscordOrScoreSaberID))
            {
                l_DiscordID = p_DiscordOrScoreSaberID;
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
                await ReplyAsync("> :x: Sorry, this Discord User doesn't have any ScoreSaber account linked/isn't a correct ScoreSaberID.");
                return;
            }
            
            Player l_Player = new Player(p_DiscordOrScoreSaberID);
            l_Player.LoadStats();
            if (l_Player.m_PlayerStats.IsFirstScan > 0)
            {
                await ReplyAsync("> :x: Sorry, but this ScoreSaber account isn't registered on the bot yet, !scan it first.");
                return;
            }
            l_Player.ResetLevels();
            for (int l_I = 0; l_I < p_Level; l_I++)
            {
                l_Player.m_PlayerStats.LevelIsPassed.Add(true);
            }
            l_Player.ReWriteStats();
            new LeaderboardController().ManagePlayer(l_Player.m_PlayerFull.playerInfo.playerName, p_DiscordOrScoreSaberID, -1, p_Level, null);

            if (l_DiscordID != null)
            {
                SocketGuildUser l_MyUser = Context.Guild.GetUser(Convert.ToUInt64(l_DiscordID));
                await ReplyAsync($"> :clock1: The bot will now update {l_MyUser.Username}'s roles. This step can take a while. ``(The bot should now be responsive again)``"); 
                var l_RoleUpdate = UserController.UpdatePlayerLevel(Context, l_MyUser.Id, p_Level);
            }
            else
            {
                await ReplyAsync($"{l_Player.m_PlayerFull.playerInfo.playerName}'s Level set to Level {p_Level}");
            }
        }

        [Command("scan")]
        [Alias("dc")]
        [Summary("Scans a specific score saber account and add it to the database.")]
        public async Task Scan_Scores(string p_DiscordOrScoreSaberID)
        {
            bool l_IsDiscordLinked = false;
            string l_ScoreSaberOrDiscordName = "";
            string l_DiscordID = "";
            SocketGuildUser l_User = null;
            
            bool l_IsScoreSaberAccount = UserController.AccountExist(p_DiscordOrScoreSaberID);

            if (UserController.UserExist(p_DiscordOrScoreSaberID))
            {
                l_User = Context.Guild.GetUser(Convert.ToUInt64(p_DiscordOrScoreSaberID));
                p_DiscordOrScoreSaberID = UserController.GetPlayer(p_DiscordOrScoreSaberID);
                if (l_User != null)
                {
                    l_IsDiscordLinked = true;
                    l_DiscordID = l_User.Id.ToString();
                    l_ScoreSaberOrDiscordName = l_User.Username;
                }
                else
                {
                    await ReplyAsync("> :x: This Discord User isn't accessible yet.");
                    return;
                }
            }
            else if (l_IsScoreSaberAccount)
            {
                if (!UserController.SSIsAlreadyLinked(p_DiscordOrScoreSaberID))
                {
                    l_User = Context.Guild.GetUser(Convert.ToUInt64(UserController.GetDiscordID(p_DiscordOrScoreSaberID)));
                    l_IsDiscordLinked = true;
                    l_DiscordID = l_User.Id.ToString();
                    l_ScoreSaberOrDiscordName = l_User.Username;
                }
            }
            else if (!UserController.UserExist(p_DiscordOrScoreSaberID))
            {
                await ReplyAsync("> :x: Sorry, this Discord User doesn't have any ScoreSaber account linked/isn't a correct ScoreSaberID.");
                return;
            }

            Player l_Player = new Player(p_DiscordOrScoreSaberID);
            if (!l_IsDiscordLinked)
            {
                l_ScoreSaberOrDiscordName = l_Player.m_PlayerFull.playerInfo.playerName;
            }

            int l_OldPlayerLevel = l_Player.GetPlayerLevel(); /// By doing so, as a result => loadstats() inside too.

            bool l_FirsScan = l_Player.FetchScores(Context); /// FetchScore Return true if it's the first scan.
            var l_FetchPass = l_Player.FetchPass(Context);
            if (l_FetchPass.Result >= 1)
            {
                if (l_IsDiscordLinked)
                {
                    await ReplyAsync($"> 🎉 {l_ScoreSaberOrDiscordName} passed {l_FetchPass.Result} new maps!\n");
                }
            }
            else
            {
                if (l_FirsScan)
                    await ReplyAsync($"> Oh, it seems like {l_ScoreSaberOrDiscordName} didn't pass any maps from the pools.");
                else
                    await ReplyAsync($"> :x: Sorry but {l_ScoreSaberOrDiscordName} didn't pass any new maps.");
            }

            int l_NewPlayerLevel = l_Player.GetPlayerLevel();
            if (l_OldPlayerLevel != l_NewPlayerLevel)
            {
                if (l_OldPlayerLevel < l_NewPlayerLevel)
                    if (l_IsDiscordLinked)
                        await ReplyAsync($"> <:Stonks:884058036371595294> GG! <@{l_DiscordID}>, You are now Level {l_NewPlayerLevel}.\n> To see your new pool, try the ``{ConfigController.GetConfig().CommandPrefix[0]}ggp`` command.");
                    else
                        await ReplyAsync($"> <:Stonks:884058036371595294> {l_ScoreSaberOrDiscordName} is now Level {l_NewPlayerLevel}.\n");
                else if (l_IsDiscordLinked)
                    await ReplyAsync($"> <:NotStonks:884057234886238208> <@{l_DiscordID}>, You lost levels. You are now Level {l_NewPlayerLevel}");
                else
                    await ReplyAsync($"> <:NotStonks:884057234886238208> {l_ScoreSaberOrDiscordName} lost levels. they are now Level {l_NewPlayerLevel}");
                if (l_IsDiscordLinked)
                {
                    await ReplyAsync($"> :clock1: The bot will now update {l_ScoreSaberOrDiscordName}'s roles. This step can take a while. ``(The bot should now be responsive again)``");
                    var l_RoleUpdate = UserController.UpdatePlayerLevel(Context, l_User.Id, l_NewPlayerLevel);
                }
            }

            if (!l_IsDiscordLinked)
                await ReplyAsync($"> :white_check_mark: {l_ScoreSaberOrDiscordName}'s info added/updated");
        }

        [Command("resetscorerequirement")]
        [Summary("Sets all maps's score requirement from a level to 0.")]
        public async Task ResetScoreRequirement(int p_Level)
        {
            if (p_Level >= 0)
            {
                new Level(p_Level).ResetScoreRequirement();
                await ReplyAsync($"> :white_check_mark: All maps in playlist {p_Level} have now a score requirement of 0");
            }
        }

        [Command("reset-config")]
        [Summary("Resets the bot config file, stops the bot.")]
        public async Task Reset_config()
        {
            await ReplyAsync("> :white_check_mark: After the bot finished to reset the config, it will stops.");
            ConfigController.CreateConfig();
        }

        [Command("shutdown")]
        [Summary("Shutdown the bot.")]
#pragma warning disable 1998
        public async Task Shutdown()
#pragma warning restore 1998
        {
            await ReplyAsync("**Shutting Down the bot**");
            Environment.Exit(0);
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