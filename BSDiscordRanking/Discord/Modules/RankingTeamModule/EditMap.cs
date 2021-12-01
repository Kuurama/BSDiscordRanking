using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using BSDiscordRanking.Formats.API;
using BSDiscordRanking.Formats.Controller;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Version = BSDiscordRanking.Formats.API.Version;

namespace BSDiscordRanking.Discord.Modules.RankingTeamModule
{
    [PermissionHandler.RequirePermissionAttribute(Permission)]
    public partial class RankingTeamModule : ModuleBase<SocketCommandContext>
    {
        [Command("editmap")]
        [Alias("rankedit")]
        [Summary("Open the Edit-Map management menu.")]
        private async Task EditMap(string p_BSRCode = null, string p_DifficultyName = "ExpertPlus", string p_Characteristic = "Standard", [Summary("DoNotDisplayOnHelp")] bool p_DisplayEditMap = true, [Summary("DoNotDisplayOnHelp")] bool p_ChangeLevel = false, [Summary("DoNotDisplayOnHelp")] int p_NewLevel = default, [Summary("DoNotDisplayOnHelp")] bool p_ChangeMinScoreRequirement = false, [Summary("DoNotDisplayOnHelp")] float p_NewMinPercentageRequirement = default, [Summary("DoNotDisplayOnHelp")] bool p_ChangeCategory = false, [Summary("DoNotDisplayOnHelp")] string p_NewCategory = null,
            [Summary("DoNotDisplayOnHelp")] bool p_ChangeInfoOnGGP = false, [Summary("DoNotDisplayOnHelp")] string p_NewInfoOnGGP = null, [Summary("DoNotDisplayOnHelp")] bool p_ChangeCustomPassText = false, [Summary("DoNotDisplayOnHelp")] string p_NewCustomPassText = null, [Summary("DoNotDisplayOnHelp")] bool p_ToggleManualWeight = false, [Summary("DoNotDisplayOnHelp")] bool p_ChangeWeight = false, [Summary("DoNotDisplayOnHelp")] float p_NewWeight = default,
            [Summary("DoNotDisplayOnHelp")] bool p_ChangeName = false, [Summary("DoNotDisplayOnHelp")] string p_NewName = null, [Summary("DoNotDisplayOnHelp")] bool p_ToggleAdminConfirmationOnPass = false, [Summary("DoNotDisplayOnHelp")]bool p_RemoveMap = false, [Summary("DoNotDisplayOnHelp")] ulong p_UserID = default, [Summary("DoNotDisplayOnHelp")] ulong p_ChannelID = default)
        {
            if (string.IsNullOrEmpty(p_BSRCode))
            {
                await ReplyAsync($"> :x: Seems like you didn't used the command correctly, use: `{BotHandler.m_Prefix}editmap [level] [key] [ExpertPlus/Hard..] (Standard/Lawless..)`");
            }
            else
            {
                if (Context != null)
                {
                    Program.m_TempGlobalGuildID = Context.Guild.Id;
                }

                bool l_MapDeleted = false;
                BeatSaverFormat l_Map = Level.FetchBeatMap(p_BSRCode);
                if (l_Map is null)
                {
                    l_MapDeleted = true;
                    Diff l_Diff = new Diff()
                    {
                        characteristic = p_Characteristic,
                        difficulty = p_DifficultyName
                    };
                    Version l_Version = new Version()
                    {
                        hash = null,
                        key = p_BSRCode,
                        diffs = new List<Diff>() { l_Diff }
                    };
                    l_Map = new BeatSaverFormat()
                    {
                        id = p_BSRCode,
                        versions = new List<Version>() { l_Version }
                    };
                }
                LevelController.MapExistFormat l_MapExistCheck = LevelController.MapExist_Check(l_Map.versions[^1].hash, p_DifficultyName, p_Characteristic, 0, p_NewCategory, p_NewInfoOnGGP, p_NewCustomPassText, false, p_NewWeight, false, l_Map.id);
                EmbedBuilder l_EmbedBuilder = new EmbedBuilder();
                if (l_MapDeleted && !l_MapExistCheck.MapExist)
                {
                    l_MapDeleted = true;
                    l_EmbedBuilder.WithTitle("Sorry this map's BSRCode doesn't exist on BeatSaver (and isn't in any level).");
                    if (Context != null)
                    {
                        await Context.Channel.SendMessageAsync("", false, l_EmbedBuilder.Build(),
                            component: new ComponentBuilder()
                                .WithButton(new ButtonBuilder("Close Menu", $"ExitEditMap_{Context.User.Id}", ButtonStyle.Danger))
                                .Build());
                    }
                    else if (p_ChannelID != default && p_UserID != default && Program.m_TempGlobalGuildID != default)
                    {
                        await BotHandler.m_Client.GetGuild(Program.m_TempGlobalGuildID).GetTextChannel(p_ChannelID).SendMessageAsync("", false, l_EmbedBuilder.Build(),
                            component: new ComponentBuilder()
                                .WithButton(new ButtonBuilder("Close Menu", $"ExitEditMap_{p_UserID}", ButtonStyle.Danger))
                                .Build());
                    }
                }
                else
                {
                    ConfigFormat l_Config = ConfigController.GetConfig();
                    if (l_MapExistCheck.MapExist)
                    {
                        Level l_Level = new Level(l_MapExistCheck.Level);
                        foreach (var l_LevelSong in l_Level.m_Level.songs)
                        {
                            if (string.Equals(l_LevelSong.hash, l_Map.versions[^1].hash, StringComparison.CurrentCultureIgnoreCase) || string.Equals(l_LevelSong.key, l_Map.id, StringComparison.CurrentCultureIgnoreCase))
                            {
                                foreach (var l_LevelDiff in l_LevelSong.difficulties)
                                {
                                    if (l_LevelDiff.characteristic == p_Characteristic && l_LevelDiff.name == p_DifficultyName)
                                    {
                                        l_MapExistCheck = LevelController.MapExist_Check(l_Map.versions[^1].hash, p_DifficultyName, p_Characteristic, AdminModule.AdminModule.ScoreFromAcc(p_NewMinPercentageRequirement, l_LevelDiff.customData.noteCount), p_NewCategory, p_NewInfoOnGGP, p_NewCustomPassText, l_LevelDiff.customData.forceManualWeight, p_NewWeight, l_LevelDiff.customData.adminConfirmationOnPass, l_Map.id); /// So i can check for New Score Requirement, etc.
                                        l_EmbedBuilder.AddField("BSRCode", p_BSRCode, true);
                                        l_EmbedBuilder.AddField(l_LevelDiff.name, l_LevelDiff.characteristic, true);
                                        l_EmbedBuilder.AddField("Level", $"Lv.{l_MapExistCheck.Level}", true);

                                        if (l_LevelDiff.customData.category != null)
                                        {
                                            l_EmbedBuilder.AddField("Category", l_LevelDiff.customData.category, true);
                                        }

                                        if (l_LevelDiff.customData.infoOnGGP != null)
                                        {
                                            l_EmbedBuilder.AddField("InfoOnGGP", l_LevelDiff.customData.infoOnGGP, true);
                                        }

                                        if (l_LevelDiff.customData.minScoreRequirement > 0)
                                        {
                                            l_EmbedBuilder.AddField("Min Score Requirement", $"{l_LevelDiff.customData.minScoreRequirement} ({((float)l_LevelDiff.customData.minScoreRequirement / l_LevelDiff.customData.maxScore * 100f):n2}%)", true);
                                        }

                                        if (l_LevelDiff.customData.customPassText != null)
                                        {
                                            l_EmbedBuilder.AddField("CustomPassText", l_LevelDiff.customData.customPassText, true);
                                        }

                                        l_EmbedBuilder.AddField("Manual Weight", $"{l_LevelDiff.customData.forceManualWeight.ToString()} ({l_LevelDiff.customData.manualWeight})", true);

                                        l_EmbedBuilder.AddField("Admin Confirmation", l_LevelDiff.customData.adminConfirmationOnPass.ToString(), true);

                                        l_EmbedBuilder.WithTitle(p_NewName ?? l_LevelSong.name);
                                        
                                        if (!l_MapDeleted)
                                        {
                                            l_EmbedBuilder.WithThumbnailUrl($"https://cdn.beatsaver.com/{l_LevelSong.hash.ToLower()}.jpg");
                                            l_EmbedBuilder.WithUrl($"https://beatsaver.com/maps/{Level.FetchBeatMapByHash(l_LevelSong.hash, Context).id}");
                                        }
                                        else
                                        {
                                            l_EmbedBuilder.WithTitle($"{p_NewName ?? l_LevelSong.name} :warning: Map has been deleted from beatsaver");
                                        }

                                        EditMapFormat l_EditMapFormat = new EditMapFormat()
                                        {
                                            BeatSaverFormat = l_Map,
                                            Category = l_LevelDiff.customData.category,
                                            CustomPassText = l_LevelDiff.customData.customPassText,
                                            ForceManualWeight = l_LevelDiff.customData.forceManualWeight,
                                            InfoOnGGP = l_LevelDiff.customData.infoOnGGP,
                                            MinScoreRequirement = l_LevelDiff.customData.minScoreRequirement,
                                            NumberOfNote = l_LevelDiff.customData.noteCount,
                                            SelectedCharacteristic = l_LevelDiff.characteristic,
                                            SelectedDifficultyName = l_LevelDiff.name,
                                            Weighting = l_LevelDiff.customData.manualWeight,
                                            adminConfirmationOnPass = l_LevelDiff.customData.adminConfirmationOnPass
                                        };

                                        if (p_DisplayEditMap)
                                        {
                                            if (Context != null)
                                            {
                                                ComponentBuilder l_ComponentBuilder = new ComponentBuilder();
                                                    
                                                if (!l_MapDeleted)
                                                {
                                                    l_ComponentBuilder.WithButton(new ButtonBuilder("Change Name", $"NameChange_{Context.User.Id}", ButtonStyle.Secondary))
                                                        .WithButton(new ButtonBuilder("Change Level", $"LevelIDChange_{Context.User.Id}", ButtonStyle.Secondary))
                                                        .WithButton(new ButtonBuilder("Change MinPercentageRequirement", $"MinPercentageRequirementChange_{Context.User.Id}", ButtonStyle.Secondary))
                                                        .WithButton(new ButtonBuilder("Change Category", $"CategoryChange_{Context.User.Id}", ButtonStyle.Secondary))
                                                        .WithButton(new ButtonBuilder("Change InfoOnGGP", $"InfoOnGGPChange_{Context.User.Id}", ButtonStyle.Secondary))
                                                        .WithButton(new ButtonBuilder("Change CustomPassText", $"CustomPassTextChange_{Context.User.Id}", ButtonStyle.Secondary))
                                                        .WithButton(new ButtonBuilder("Change Manual Weight", $"ManualWeightChange_{Context.User.Id}", ButtonStyle.Secondary))
                                                        .WithButton(new ButtonBuilder("Toggle Manual Weight Preference", $"ToggleManualWeight_{Context.User.Id}", ButtonStyle.Secondary))
                                                        .WithButton(new ButtonBuilder("Toggle Admin Confirmation Preference", $"ToggleAdminConfirmation_{Context.User.Id}", ButtonStyle.Secondary))
                                                        .WithButton(new ButtonBuilder("Remove Map", $"RemoveMap_{Context.User.Id}", ButtonStyle.Danger))
                                                        .WithButton(new ButtonBuilder("Close Menu", $"ExitEditMap_{Context.User.Id}", ButtonStyle.Danger));
                                                }
                                                else
                                                {
                                                    l_ComponentBuilder.WithButton(new ButtonBuilder("Remove Map", $"RemoveMap_{Context.User.Id}", ButtonStyle.Danger))
                                                        .WithButton(new ButtonBuilder("Close Menu", $"ExitEditMap_{Context.User.Id}", ButtonStyle.Danger));
                                                }
                                                l_EmbedBuilder.WithFooter($"DiscordID_{Context.User.Id}");
                                                await Context.Channel.SendMessageAsync("", false, l_EmbedBuilder.Build(), component: l_ComponentBuilder.Build());
                                            }
                                            else if (p_ChannelID != default && p_UserID != default && Program.m_TempGlobalGuildID != default)
                                            {
                                                ComponentBuilder l_ComponentBuilder = new ComponentBuilder();
                                                if (!l_MapDeleted)
                                                {
                                                    l_ComponentBuilder.WithButton(new ButtonBuilder("Change Name", $"NameChange_{p_UserID}", ButtonStyle.Secondary))
                                                        .WithButton(new ButtonBuilder("Change Level", $"LevelIDChange_{p_UserID}", ButtonStyle.Secondary))
                                                        .WithButton(new ButtonBuilder("Change MinPercentageRequirement", $"MinPercentageRequirementChange_{p_UserID}", ButtonStyle.Secondary))
                                                        .WithButton(new ButtonBuilder("Change Category", $"CategoryChange_{p_UserID}", ButtonStyle.Secondary))
                                                        .WithButton(new ButtonBuilder("Change InfoOnGGP", $"InfoOnGGPChange_{p_UserID}", ButtonStyle.Secondary))
                                                        .WithButton(new ButtonBuilder("Change CustomPassText", $"CustomPassTextChange_{p_UserID}", ButtonStyle.Secondary))
                                                        .WithButton(new ButtonBuilder("Change Manual Weight", $"ManualWeightChange_{p_UserID}", ButtonStyle.Secondary))
                                                        .WithButton(new ButtonBuilder("Toggle Manual Weight Preference", $"ToggleManualWeight_{p_UserID}", ButtonStyle.Secondary))
                                                        .WithButton(new ButtonBuilder("Toggle Admin Confirmation Preference", $"ToggleAdminConfirmation_{p_UserID}", ButtonStyle.Secondary))
                                                        .WithButton(new ButtonBuilder("Remove Map", $"RemoveMap_{p_UserID}", ButtonStyle.Danger))
                                                        .WithButton(new ButtonBuilder("Close Menu", $"ExitEditMap_{p_UserID}", ButtonStyle.Danger));;
                                                }
                                                else
                                                {
                                                    l_ComponentBuilder.WithButton(new ButtonBuilder("Remove Map", $"RemoveMap_{p_UserID}", ButtonStyle.Danger))
                                                        .WithButton(new ButtonBuilder("Close Menu", $"ExitEditMap_{p_UserID}", ButtonStyle.Danger));
                                                }

                                                l_EmbedBuilder.WithFooter($"DiscordID_{p_UserID}");
                                                await BotHandler.m_Client.GetGuild(Program.m_TempGlobalGuildID).GetTextChannel(p_ChannelID).SendMessageAsync("", false, l_EmbedBuilder.Build(), component: l_ComponentBuilder.Build());
                                            }
                                        }

                                        if (p_ChangeLevel)
                                        {
                                            if (!ChangeMapLevel(l_EditMapFormat, l_MapExistCheck.Level, p_NewLevel))
                                            {
                                                if (Context != null)
                                                {
                                                    await Context.Channel.SendMessageAsync(":warning: An error occured while changing the map's level.");
                                                }
                                                else if (p_ChannelID != default && p_UserID != default && Program.m_TempGlobalGuildID != default)
                                                {
                                                    await BotHandler.m_Client.GetGuild(Program.m_TempGlobalGuildID).GetTextChannel(p_ChannelID).SendMessageAsync(":warning:, An error occured while changing the map's level.");
                                                }
                                            }
                                        }

                                        if (p_RemoveMap)
                                        {
                                            if (!RemoveMap(l_EditMapFormat, l_MapExistCheck.Level))
                                            {
                                                if (Context != null)
                                                {
                                                    await Context.Channel.SendMessageAsync(":warning: An error occured while deleting the map.");
                                                }
                                                else if (p_ChannelID != default && p_UserID != default && Program.m_TempGlobalGuildID != default)
                                                {
                                                    await BotHandler.m_Client.GetGuild(Program.m_TempGlobalGuildID).GetTextChannel(p_ChannelID).SendMessageAsync(":warning:, An error occured while deleting the map.");
                                                }
                                            }
                                            else
                                            {
                                                if (l_Level.m_Level.songs.Count == 0)
                                                {
                                                    l_EmbedBuilder = new EmbedBuilder();
                                                    l_Level.DeleteLevel();
                                                    l_EmbedBuilder.WithTitle("Level Removed!");
                                                    l_EmbedBuilder.AddField("Level:", l_MapExistCheck.Level);
                                                    l_EmbedBuilder.AddField("Reason:", "All maps has been removed.");
                                                }
                                                else
                                                {
                                                   
                                                    l_EmbedBuilder.AddField("Map name:", l_MapExistCheck.Name);
                                                    l_EmbedBuilder.AddField("Difficulty:", p_Characteristic + " - " + p_DifficultyName);
                                                    l_EmbedBuilder.AddField("Level:", l_MapExistCheck.Level);
                                                   
                                                }

                                                if (!l_MapDeleted)
                                                {
                                                    l_EmbedBuilder.WithTitle("Map removed!");
                                                    l_EmbedBuilder.WithThumbnailUrl($"https://cdn.beatsaver.com/{l_Map.versions[^1].hash.ToLower()}.jpg");
                                                    l_EmbedBuilder.AddField("Link:", $"https://beatsaver.com/maps/{l_Map.id}", false);
                                                }
                                                else
                                                {
                                                    l_EmbedBuilder.WithTitle("Map removed! (wasn't on BeatSaver anymore)");
                                                    l_EmbedBuilder.AddField("Old Link:", $"https://beatsaver.com/maps/{l_Map.id}", false);
                                                }
                                                
                                                l_EmbedBuilder.WithColor(Color.Red);
                                                if (Context != null)
                                                {
                                                    l_EmbedBuilder.WithFooter("Operated by " + Context.User.Username);
                                                    await Context.Guild.GetTextChannel(ConfigController.GetConfig().LoggingChannel).SendMessageAsync("", false, l_EmbedBuilder.Build());
                                                }
                                                else if (p_ChannelID != default && p_UserID != default && Program.m_TempGlobalGuildID != default)
                                                {
                                                    l_EmbedBuilder.WithFooter("Operated by " + p_UserID);
                                                    await BotHandler.m_Client.GetGuild(Program.m_TempGlobalGuildID).GetTextChannel(ConfigController.GetConfig().LoggingChannel).SendMessageAsync("", false, l_EmbedBuilder.Build());
                                                }
                                                return;
                                            }
                                        }

                                        if (p_ChangeName)
                                        {
                                            ChangeName(l_EditMapFormat, l_MapExistCheck.Level, p_NewName);
                                        }

                                        if (!l_MapDeleted && (p_ChangeLevel || p_ChangeMinScoreRequirement || p_ChangeCategory || p_ChangeInfoOnGGP || p_ToggleManualWeight || p_ChangeWeight || p_ChangeCustomPassText || p_ToggleAdminConfirmationOnPass)) /// || p_ChangeCustomPassText But i choosed to not display it.
                                        {
                                            if (p_ChangeLevel || p_ToggleAdminConfirmationOnPass || l_MapExistCheck.DifferentMinScore || l_MapExistCheck.DifferentCategory || l_MapExistCheck.DifferentInfoOnGGP || l_MapExistCheck.DifferentPassText || l_MapExistCheck.DifferentForceManualWeight || l_MapExistCheck.DifferentWeight)
                                            {
                                                int l_OldLevel = default;
                                                if (p_ChangeLevel)
                                                {
                                                    l_OldLevel = l_MapExistCheck.Level;
                                                }

                                                if (!p_ChangeMinScoreRequirement)
                                                {
                                                    p_NewMinPercentageRequirement = 100f * (float)l_LevelDiff.customData.minScoreRequirement / l_LevelDiff.customData.maxScore;
                                                }

                                                if (!p_ChangeCategory)
                                                {
                                                    p_NewCategory = l_LevelDiff.customData.category;
                                                }

                                                if (!p_ChangeInfoOnGGP)
                                                {
                                                    p_NewInfoOnGGP = l_LevelDiff.customData.infoOnGGP;
                                                }

                                                if (!p_ChangeCustomPassText)
                                                {
                                                    p_NewCustomPassText = l_LevelDiff.customData.customPassText;
                                                }

                                                if (p_ToggleManualWeight)
                                                {
                                                    l_EditMapFormat.ForceManualWeight = !l_EditMapFormat.ForceManualWeight;
                                                }

                                                if (!p_ChangeWeight)
                                                {
                                                    p_NewWeight = l_EditMapFormat.Weighting;
                                                }

                                                if (p_ToggleAdminConfirmationOnPass)
                                                {
                                                    l_EditMapFormat.adminConfirmationOnPass = !l_LevelDiff.customData.adminConfirmationOnPass;
                                                }

                                                l_MapExistCheck = LevelController.MapExist_Check(l_Map.versions[^1].hash, p_DifficultyName, p_Characteristic, AdminModule.AdminModule.ScoreFromAcc(p_NewMinPercentageRequirement, l_LevelDiff.customData.noteCount), p_NewCategory, p_NewInfoOnGGP, p_NewCustomPassText, l_EditMapFormat.ForceManualWeight, p_NewWeight, l_EditMapFormat.adminConfirmationOnPass);
                                                if (!p_ChangeLevel)
                                                {
                                                    l_Level.AddMap(l_Map, p_DifficultyName, p_Characteristic, AdminModule.AdminModule.ScoreFromAcc(p_NewMinPercentageRequirement, l_LevelDiff.customData.noteCount), p_NewCategory, p_NewInfoOnGGP, p_NewCustomPassText, l_EditMapFormat.ForceManualWeight, p_NewWeight, l_LevelDiff.customData.noteCount, l_EditMapFormat.adminConfirmationOnPass, null);
                                                }

                                                EmbedBuilder l_MapChangeEmbedBuilder = new EmbedBuilder();
                                                l_MapChangeEmbedBuilder.WithTitle("Maps infos changed on:");
                                                l_MapChangeEmbedBuilder.WithDescription(l_MapExistCheck.Name);
                                                l_MapChangeEmbedBuilder.AddField("Difficulty:", p_Characteristic + " - " + p_DifficultyName, true);
                                                if (!p_ChangeLevel)
                                                    l_MapChangeEmbedBuilder.AddField("Level:", $"Lv.{l_MapExistCheck.Level}", true);
                                                else
                                                    l_MapChangeEmbedBuilder.AddField("New Level:", $"Lv.{p_NewLevel} (Old: {l_OldLevel})", false);

                                                if (l_MapExistCheck.DifferentMinScore)
                                                    l_MapChangeEmbedBuilder.AddField("New ScoreRequirement:", $"{AdminModule.AdminModule.ScoreFromAcc(p_NewMinPercentageRequirement, l_LevelDiff.customData.noteCount)} ({p_NewMinPercentageRequirement:n2}%)", false);

                                                if (l_MapExistCheck.DifferentCategory)
                                                    l_MapChangeEmbedBuilder.AddField("New Category:", p_NewCategory, false);

                                                if (l_MapExistCheck.DifferentInfoOnGGP)
                                                    l_MapChangeEmbedBuilder.AddField("New InfoOnGGP:", p_NewInfoOnGGP, false);

                                                if (l_MapExistCheck.DifferentPassText)
                                                {
                                                    if ( l_Config.DisplayCustomPassTextInGetInfo)
                                                    {
                                                        l_MapChangeEmbedBuilder.AddField("New CustomPassText:", p_NewCustomPassText, false);
                                                    }
                                                    else
                                                    {
                                                        l_MapChangeEmbedBuilder.AddField("New secret CustomPassText added", "\u200B", false);
                                                    }
                                                }

                                                if (l_MapExistCheck.DifferentForceManualWeight)
                                                    l_MapChangeEmbedBuilder.AddField("New Manual Weight Preference:", l_EditMapFormat.ForceManualWeight, false);

                                                if (l_MapExistCheck.DifferentAdminConfirmationOnPass)
                                                    l_MapChangeEmbedBuilder.AddField("New Admin Confirmation Preference:", l_EditMapFormat.adminConfirmationOnPass, false);

                                                if (l_MapExistCheck.DifferentWeight)
                                                    l_MapChangeEmbedBuilder.AddField($"New Manual Weight:", $"{p_NewWeight:n3}", false);

                                                l_MapChangeEmbedBuilder.AddField("Link:", $"https://beatsaver.com/maps/{l_Map.id}", false);
                                                if (p_UserID != default) l_EmbedBuilder.WithFooter($"Operated by <@{p_UserID}>");
                                                l_MapChangeEmbedBuilder.WithThumbnailUrl($"https://cdn.beatsaver.com/{l_Map.versions[^1].hash.ToLower()}.jpg");
                                                l_MapChangeEmbedBuilder.WithColor(Color.Blue);

                                                if ((p_ChangeCategory && l_Config.DisplayCategoryEdit) || (p_ChangeCustomPassText && l_Config.DisplayCustomPassTextEdit) || p_ChangeLevel || p_ChangeMinScoreRequirement || p_ChangeInfoOnGGP || p_ToggleManualWeight || p_ChangeWeight || p_ToggleAdminConfirmationOnPass)
                                                {
                                                    foreach (var l_TextChannel in BotHandler.m_Client.GetGuild(Program.m_TempGlobalGuildID).TextChannels)
                                                    {
                                                        if (l_TextChannel.Id == l_Config.LoggingChannel)
                                                        {
                                                            await l_TextChannel.SendMessageAsync("", false, l_MapChangeEmbedBuilder.Build());
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

                    else
                    {
                        l_EmbedBuilder.WithTitle("Sorry this map's difficulty/characteristic isn't in any level.");
                        if (Context != null)
                        {
                            await Context.Channel.SendMessageAsync("", false, l_EmbedBuilder.Build(),
                                component: new ComponentBuilder()
                                    .WithButton(new ButtonBuilder("Close Menu", $"ExitEditMap_{Context.User.Id}", ButtonStyle.Danger))
                                    .Build());
                        }
                        else if (p_ChannelID != default && p_UserID != default && Program.m_TempGlobalGuildID != default)
                        {
                            await BotHandler.m_Client.GetGuild(Program.m_TempGlobalGuildID).GetTextChannel(p_ChannelID).SendMessageAsync("", false, l_EmbedBuilder.Build(),
                                component: new ComponentBuilder()
                                    .WithButton(new ButtonBuilder("Close Menu", $"ExitEditMap_{p_UserID}", ButtonStyle.Danger))
                                    .Build());
                        }
                    }
                }
            }
        }

        public async Task EditMapButtonHandler(SocketMessageComponent p_MessageComponent)
        {
            ulong l_UserID = p_MessageComponent.User.Id;
            ulong l_ChannelID = p_MessageComponent.Message.Channel.Id;
            string[] l_SplicedCustomID = p_MessageComponent.Data.CustomId.Split("_"); /// I choosed to use the button custom ID because adding buttons at a different place or command and making the DiscordID built in footer (or else) of an Embed wouldn't be good (as it would always need an Embed and a Footer).
            bool l_IsCorrectUserID = l_SplicedCustomID.Any(p_SplicedPartCustomID => p_SplicedPartCustomID == l_UserID.ToString());
            if (l_IsCorrectUserID)
            {
                bool l_InteractionResponded = false;
                EmbedBuilder l_EmbedBuilder = p_MessageComponent.Message.Embeds.FirstOrDefault().ToEmbedBuilder();
                ComponentBuilder l_ComponentBuilder = new ComponentBuilder();
                EditMapArgumentFormat l_EditMapArgumentFormat;
                IEnumerable<IMessage> l_Messages;
                string l_LastUserMessage;
                switch (l_SplicedCustomID[0])
                {
                    case "LevelIDChange":
                        List<SelectMenuOptionBuilder> l_SelectMenuOptionBuilders = new List<SelectMenuOptionBuilder>();
                        List<List<SelectMenuOptionBuilder>> l_ListListMenuOptionBuilder = new List<List<SelectMenuOptionBuilder>>() { new List<SelectMenuOptionBuilder>() };
                        LevelControllerFormat l_LevelControllerFormat = LevelController.FetchAndGetLevel();
                        LevelController.ReWriteController(l_LevelControllerFormat);

                        foreach (var l_LevelID in l_LevelControllerFormat.LevelID)
                        {
                            l_SelectMenuOptionBuilders.Add(new SelectMenuOptionBuilder($"Level {l_LevelID}", l_LevelID.ToString()));
                        }

                        int l_Index = 0;
                        int l_ListListMenuIndex = 0;
                        foreach (var l_SelectMenuOptionBuilder in l_SelectMenuOptionBuilders)
                        {
                            if (l_Index > 23)
                            {
                                l_ListListMenuOptionBuilder.Add(new List<SelectMenuOptionBuilder>());
                                l_ListListMenuIndex++;
                                l_Index = 0;
                            }

                            l_ListListMenuOptionBuilder[l_ListListMenuIndex].Add(l_SelectMenuOptionBuilder);
                            l_Index++;
                        }

                        l_Index = 0;
                        foreach (var l_ListMenuOptionBuilder in l_ListListMenuOptionBuilder)
                        {
                            l_ComponentBuilder.WithSelectMenu($"SelectLevelMenu_{l_Index}", options: l_ListMenuOptionBuilder);
                            l_Index++;
                        }

                        l_ComponentBuilder
                            .WithButton(new ButtonBuilder("Back", $"BackToEditMapMenu_{l_UserID}"))
                            .WithButton(new ButtonBuilder("Close Menu", $"ExitEditMap_{l_UserID}", ButtonStyle.Danger));
                        await p_MessageComponent.Message.ModifyAsync(p_MessageProperties => p_MessageProperties.Embed = l_EmbedBuilder
                            .AddField("\u200B", "\u200B")
                            .AddField("__Level-Edit__", "Please choose the level you want this difficulty to be in.").Build());
                        await p_MessageComponent.Message.ModifyAsync(p_MessageProperties => p_MessageProperties.Components = l_ComponentBuilder.Build());
                        l_InteractionResponded = true;
                        break;

                    case "CategoryChange":
                        l_ComponentBuilder
                            .WithButton(new ButtonBuilder("Validate my choice", $"CategoryChangeValidation_{l_UserID}", ButtonStyle.Success))
                            .WithButton(new ButtonBuilder("Back", $"BackToEditMapMenu_{l_UserID}"))
                            .WithButton(new ButtonBuilder("Close Menu", $"ExitEditMap_{l_UserID}", ButtonStyle.Danger));
                        await p_MessageComponent.Message.ModifyAsync(p_MessageProperties => p_MessageProperties.Embed = l_EmbedBuilder
                            .AddField("\u200B", "\u200B")
                            .AddField("__Category-Edit__", "Please type the category you want the map to be in. Then press \"Validate my choice\" => Your next (and last) typed message will be read.\n(Make sure you typed the right category as it's Cap sensitive)").Build());
                        await p_MessageComponent.Message.ModifyAsync(p_MessageProperties => p_MessageProperties.Components = l_ComponentBuilder.Build());
                        l_InteractionResponded = true;
                        break;

                    case "CategoryChangeValidation":
                        l_Messages = await BotHandler.m_Client.GetGuild(Program.m_TempGlobalGuildID).GetTextChannel(p_MessageComponent.Channel.Id).GetMessagesAsync(limit: 20).FlattenAsync();
                        l_LastUserMessage = (from l_Message in l_Messages where l_Message.Author.Id == l_UserID select l_Message.Content).FirstOrDefault();

                        if (l_LastUserMessage != null)
                        {
                            l_EditMapArgumentFormat = GetEditMapArguments(p_MessageComponent);
                            int l_CategoryEditTitleFieldIndex = l_EmbedBuilder.Fields.FindIndex(p_X => p_X.Name.Contains("Category-Edit"));
                            l_EmbedBuilder.Fields.RemoveAt(l_CategoryEditTitleFieldIndex); /// Removing the "Category-Edit" Field
                            int l_CategoryFieldIndex = l_EmbedBuilder.Fields.FindIndex(p_X => p_X.Name.Contains("Category"));
                            if (l_CategoryFieldIndex < 0)
                            {
                                l_EmbedBuilder.AddField("Category", "\u200B");
                                l_CategoryFieldIndex = l_EmbedBuilder.Fields.Count - 1;
                            }

                            string l_OldCategory = l_EmbedBuilder.Fields[l_CategoryFieldIndex].Value.ToString();
                            l_EmbedBuilder.Fields[l_CategoryFieldIndex].Name = l_EmbedBuilder.Fields[l_CategoryFieldIndex].Name = $"New Category";
                            l_EmbedBuilder.Fields[l_CategoryFieldIndex].Value = l_LastUserMessage;

                            await EditMap(l_EditMapArgumentFormat.BSRCode, l_EditMapArgumentFormat.DifficultyName, l_EditMapArgumentFormat.DifficultyCharacteristic, false, p_UserID: l_UserID, p_ChannelID: l_ChannelID, p_ChangeCategory: true, p_NewCategory: l_LastUserMessage);

                            await p_MessageComponent.Message.ModifyAsync(p_MessageProperties => p_MessageProperties.Embed = l_EmbedBuilder.AddField("__Category-Edit Done__", $"Old Category: {l_OldCategory}").Build());
                            await p_MessageComponent.Message.ModifyAsync(p_MessageProperties => p_MessageProperties.Components = new ComponentBuilder()
                                .WithButton(new ButtonBuilder("Back", $"BackToEditMapMenu_{l_UserID}"))
                                .WithButton(new ButtonBuilder("Close Menu", $"ExitEditMap_{l_UserID}", ButtonStyle.Danger)).Build());
                            l_InteractionResponded = true;
                        }

                        break;

                    case "ToggleManualWeight":
                        l_EditMapArgumentFormat = GetEditMapArguments(p_MessageComponent);
                        int l_ManualWeightTitleFieldIndex = l_EmbedBuilder.Fields.FindIndex(p_X => p_X.Name.Contains("Manual Weight"));
                        string l_OldManualWeightPreference = l_EmbedBuilder.Fields[l_ManualWeightTitleFieldIndex].Value.ToString();
                        if (l_OldManualWeightPreference != null)
                        {
                            string l_OldManualWeightPartialText = l_OldManualWeightPreference.Replace("(", "").Replace(")", "");
                            string[] l_SplicedOldManualWeight = l_OldManualWeightPartialText.Split(" ");
                            if (l_SplicedOldManualWeight.Length >= 2)
                            {
                                bool l_NewManualWeightPreference = l_SplicedOldManualWeight[0] is not ("true" or "True");
                                l_EmbedBuilder.Fields[l_ManualWeightTitleFieldIndex].Name = l_EmbedBuilder.Fields[l_ManualWeightTitleFieldIndex].Name = $"New Manual Weight Preference";
                                l_EmbedBuilder.Fields[l_ManualWeightTitleFieldIndex].Value = $"{l_NewManualWeightPreference} ({l_SplicedOldManualWeight[1]})";

                                await EditMap(l_EditMapArgumentFormat.BSRCode, l_EditMapArgumentFormat.DifficultyName, l_EditMapArgumentFormat.DifficultyCharacteristic, false, p_UserID: l_UserID, p_ChannelID: l_ChannelID, p_ToggleManualWeight: true);

                                await p_MessageComponent.Message.ModifyAsync(p_MessageProperties => p_MessageProperties.Embed = l_EmbedBuilder.Build());
                                l_InteractionResponded = true;
                            }
                        }

                        break;

                    case "ToggleAdminConfirmation":
                        l_EditMapArgumentFormat = GetEditMapArguments(p_MessageComponent);
                        int l_AdminConfirmationTitleFieldIndex = l_EmbedBuilder.Fields.FindIndex(p_X => p_X.Name.Contains("Admin Confirmation"));
                        string l_OldAdminConfirmationPreference = l_EmbedBuilder.Fields[l_AdminConfirmationTitleFieldIndex].Value.ToString();
                        if (l_OldAdminConfirmationPreference != null)
                        {
                            bool l_NewAdminConfirmationPreference = l_OldAdminConfirmationPreference is not ("true" or "True");
                            l_EmbedBuilder.Fields[l_AdminConfirmationTitleFieldIndex].Name = l_EmbedBuilder.Fields[l_AdminConfirmationTitleFieldIndex].Name = $"New Admin Confirmation Preference";
                            l_EmbedBuilder.Fields[l_AdminConfirmationTitleFieldIndex].Value = l_NewAdminConfirmationPreference;

                            await EditMap(l_EditMapArgumentFormat.BSRCode, l_EditMapArgumentFormat.DifficultyName, l_EditMapArgumentFormat.DifficultyCharacteristic, false, p_UserID: l_UserID, p_ChannelID: l_ChannelID, p_ToggleAdminConfirmationOnPass: true);

                            await p_MessageComponent.Message.ModifyAsync(p_MessageProperties => p_MessageProperties.Embed = l_EmbedBuilder.Build());
                            l_InteractionResponded = true;
                        }

                        break;

                    case "MinPercentageRequirementChange":
                        l_ComponentBuilder
                            .WithButton(new ButtonBuilder("Validate my choice", $"MinPercentageRequirementChangeValidation_{l_UserID}", ButtonStyle.Success))
                            .WithButton(new ButtonBuilder("Back", $"BackToEditMapMenu_{l_UserID}"))
                            .WithButton(new ButtonBuilder("Close Menu", $"ExitEditMap_{l_UserID}", ButtonStyle.Danger));
                        await p_MessageComponent.Message.ModifyAsync(p_MessageProperties => p_MessageProperties.Embed = l_EmbedBuilder
                            .AddField("\u200B", "\u200B")
                            .AddField("__MinPercentageRequirement-Edit__", $"Please type the Minimum percentage requirement you want this map to have (not including the '%' and respecting the {0.00f:n2} format). Then press \"Validate my choice\" => Your next (and last) typed message will be read.").Build());
                        await p_MessageComponent.Message.ModifyAsync(p_MessageProperties => p_MessageProperties.Components = l_ComponentBuilder.Build());
                        l_InteractionResponded = true;
                        break;

                    case "MinPercentageRequirementChangeValidation":
                        l_Messages = await BotHandler.m_Client.GetGuild(Program.m_TempGlobalGuildID).GetTextChannel(p_MessageComponent.Channel.Id).GetMessagesAsync(limit: 20).FlattenAsync();
                        l_LastUserMessage = (from l_Message in l_Messages where l_Message.Author.Id == l_UserID select l_Message.Content).FirstOrDefault();

                        if (l_LastUserMessage != null)
                        {
                            if (float.TryParse(l_LastUserMessage, out float l_FloatLastUserMessage))
                            {
                                l_EditMapArgumentFormat = GetEditMapArguments(p_MessageComponent);
                                int l_MinPercentageRequirementEditTitleFieldIndex = l_EmbedBuilder.Fields.FindIndex(p_X => p_X.Name.Contains("MinPercentageRequirement-Edit"));
                                l_EmbedBuilder.Fields.RemoveAt(l_MinPercentageRequirementEditTitleFieldIndex); /// Removing the "MinPercentageRequirement-Edit" Field
                                int l_MinPercentageRequirementFieldIndex = l_EmbedBuilder.Fields.FindIndex(p_X => p_X.Name.Contains("Min Score Requirement"));
                                if (l_MinPercentageRequirementFieldIndex < 0)
                                {
                                    l_EmbedBuilder.AddField("Min Score Requirement", "0 (0%)");
                                    l_MinPercentageRequirementFieldIndex = l_EmbedBuilder.Fields.Count - 1;
                                }

                                string l_OldPercentageRequirementFullText = l_EmbedBuilder.Fields[l_MinPercentageRequirementFieldIndex].Value.ToString();
                                if (l_OldPercentageRequirementFullText != null)
                                {
                                    string l_OldPercentageRequirementPartialText = l_OldPercentageRequirementFullText.Replace("(", "").Replace("%)", "");
                                    string[] l_SplicedOldPercentageRequirement = l_OldPercentageRequirementPartialText.Split(" ");
                                    if (l_SplicedOldPercentageRequirement.Length >= 2)
                                    {
                                        if (int.TryParse(l_SplicedOldPercentageRequirement[0], out int l_IntOldScoreRequirement))
                                        {
                                            if (float.TryParse(l_SplicedOldPercentageRequirement[1], out float l_FloatOldPercentageRequirement))
                                            {
                                                float l_MaxScore = l_IntOldScoreRequirement / (l_FloatOldPercentageRequirement / 100f);
                                                int l_NewMinScore = (int)(l_FloatLastUserMessage / 100f * l_MaxScore);
                                                l_EmbedBuilder.Fields[l_MinPercentageRequirementFieldIndex].Name = l_EmbedBuilder.Fields[l_MinPercentageRequirementFieldIndex].Name = $"New Min Score Requirement";


                                                if (l_IntOldScoreRequirement != 0 || l_FloatOldPercentageRequirement != 0)
                                                {
                                                    l_EmbedBuilder.Fields[l_MinPercentageRequirementFieldIndex].Value = $"{l_NewMinScore} ({l_FloatLastUserMessage}%)";
                                                }
                                                else
                                                {
                                                    l_EmbedBuilder.Fields[l_MinPercentageRequirementFieldIndex].Value = $"({l_FloatLastUserMessage}%)";
                                                }

                                                await EditMap(l_EditMapArgumentFormat.BSRCode, l_EditMapArgumentFormat.DifficultyName, l_EditMapArgumentFormat.DifficultyCharacteristic, false, p_UserID: l_UserID, p_ChannelID: l_ChannelID, p_ChangeMinScoreRequirement: true, p_NewMinPercentageRequirement: l_FloatLastUserMessage);

                                                await p_MessageComponent.Message.ModifyAsync(p_MessageProperties => p_MessageProperties.Embed = l_EmbedBuilder.AddField("__MinScoreRequirement-Edit Done__", $"Old MinScoreRequirement: {l_OldPercentageRequirementFullText}").Build());
                                                await p_MessageComponent.Message.ModifyAsync(p_MessageProperties => p_MessageProperties.Components = new ComponentBuilder()
                                                    .WithButton(new ButtonBuilder("Back", $"BackToEditMapMenu_{l_UserID}"))
                                                    .WithButton(new ButtonBuilder("Close Menu", $"ExitEditMap_{l_UserID}", ButtonStyle.Danger)).Build());
                                                l_InteractionResponded = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        break;

                    case "InfoOnGGPChange":
                        l_ComponentBuilder
                            .WithButton(new ButtonBuilder("Validate my choice", $"InfoOnGGPChangeValidation_{l_UserID}", ButtonStyle.Success))
                            .WithButton(new ButtonBuilder("Back", $"BackToEditMapMenu_{l_UserID}"))
                            .WithButton(new ButtonBuilder("Close Menu", $"ExitEditMap_{l_UserID}", ButtonStyle.Danger));
                        await p_MessageComponent.Message.ModifyAsync(p_MessageProperties => p_MessageProperties.Embed = l_EmbedBuilder
                            .AddField("\u200B", "\u200B")
                            .AddField("__InfoOnGGP-Edit__", "Please type the InfoOnGGP you want the map to have. Then press \"Validate my choice\" => Your next (and last) typed message will be read.").Build());
                        await p_MessageComponent.Message.ModifyAsync(p_MessageProperties => p_MessageProperties.Components = l_ComponentBuilder.Build());
                        l_InteractionResponded = true;
                        break;

                    case "InfoOnGGPChangeValidation":
                        l_Messages = await BotHandler.m_Client.GetGuild(Program.m_TempGlobalGuildID).GetTextChannel(p_MessageComponent.Channel.Id).GetMessagesAsync(limit: 20).FlattenAsync();
                        l_LastUserMessage = (from l_Message in l_Messages where l_Message.Author.Id == l_UserID select l_Message.Content).FirstOrDefault();

                        if (l_LastUserMessage != null)
                        {
                            l_EditMapArgumentFormat = GetEditMapArguments(p_MessageComponent);
                            int l_InfoOnGGPEditTitleFieldIndex = l_EmbedBuilder.Fields.FindIndex(p_X => p_X.Name.Contains("InfoOnGGP-Edit"));
                            l_EmbedBuilder.Fields.RemoveAt(l_InfoOnGGPEditTitleFieldIndex); /// Removing the "InfoOnGGP-Edit" Field
                            int l_InfoOnGGPFieldIndex = l_EmbedBuilder.Fields.FindIndex(p_X => p_X.Name.Contains("InfoOnGGP"));
                            if (l_InfoOnGGPFieldIndex < 0)
                            {
                                l_EmbedBuilder.AddField("InfoOnGGP", "\u200B");
                                l_InfoOnGGPFieldIndex = l_EmbedBuilder.Fields.Count - 1;
                            }

                            string l_OldInfoOnGGP = l_EmbedBuilder.Fields[l_InfoOnGGPFieldIndex].Value.ToString();
                            l_EmbedBuilder.Fields[l_InfoOnGGPFieldIndex].Name = l_EmbedBuilder.Fields[l_InfoOnGGPFieldIndex].Name = $"New InfoOnGGP";
                            l_EmbedBuilder.Fields[l_InfoOnGGPFieldIndex].Value = l_LastUserMessage;

                            await EditMap(l_EditMapArgumentFormat.BSRCode, l_EditMapArgumentFormat.DifficultyName, l_EditMapArgumentFormat.DifficultyCharacteristic, false, p_UserID: l_UserID, p_ChannelID: l_ChannelID, p_ChangeInfoOnGGP: true, p_NewInfoOnGGP: l_LastUserMessage);

                            await p_MessageComponent.Message.ModifyAsync(p_MessageProperties => p_MessageProperties.Embed = l_EmbedBuilder.AddField("__InfoOnGGP-Edit Done__", $"Old InfoOnGGP: {l_OldInfoOnGGP}").Build());
                            await p_MessageComponent.Message.ModifyAsync(p_MessageProperties => p_MessageProperties.Components = new ComponentBuilder()
                                .WithButton(new ButtonBuilder("Back", $"BackToEditMapMenu_{l_UserID}"))
                                .WithButton(new ButtonBuilder("Close Menu", $"ExitEditMap_{l_UserID}", ButtonStyle.Danger)).Build());
                            l_InteractionResponded = true;
                        }

                        break;

                    case "ManualWeightChange":
                        l_ComponentBuilder
                            .WithButton(new ButtonBuilder("Validate my choice", $"ManualWeightChangeValidation_{l_UserID}", ButtonStyle.Success))
                            .WithButton(new ButtonBuilder("Back", $"BackToEditMapMenu_{l_UserID}"))
                            .WithButton(new ButtonBuilder("Close Menu", $"ExitEditMap_{l_UserID}", ButtonStyle.Danger));
                        await p_MessageComponent.Message.ModifyAsync(p_MessageProperties => p_MessageProperties.Embed = l_EmbedBuilder
                            .AddField("\u200B", "\u200B")
                            .AddField("__Manual Weight-Edit__", $"Please type the Manual Weight you want the map to have (respecting the {0.000f:n3} format). Then press \"Validate my choice\" => Your next (and last) typed message will be read.").Build());
                        await p_MessageComponent.Message.ModifyAsync(p_MessageProperties => p_MessageProperties.Components = l_ComponentBuilder.Build());
                        l_InteractionResponded = true;
                        break;

                    case "ManualWeightChangeValidation":
                        l_Messages = await BotHandler.m_Client.GetGuild(Program.m_TempGlobalGuildID).GetTextChannel(p_MessageComponent.Channel.Id).GetMessagesAsync(limit: 20).FlattenAsync();
                        l_LastUserMessage = (from l_Message in l_Messages where l_Message.Author.Id == l_UserID select l_Message.Content).FirstOrDefault();

                        if (l_LastUserMessage != null)
                        {
                            if (float.TryParse(l_LastUserMessage, out float l_FloatUserMessage))
                            {
                                l_EditMapArgumentFormat = GetEditMapArguments(p_MessageComponent);
                                int l_ManualWeightEditTitleFieldIndex = l_EmbedBuilder.Fields.FindIndex(p_X => p_X.Name.Contains("Manual Weight-Edit"));
                                l_EmbedBuilder.Fields.RemoveAt(l_ManualWeightEditTitleFieldIndex); /// Removing the "Manual Weight-Edit" Field
                                int l_ManualWeightFieldIndex = l_EmbedBuilder.Fields.FindIndex(p_X => p_X.Name.Contains("Manual Weight"));
                                if (l_ManualWeightFieldIndex < 0)
                                {
                                    l_EmbedBuilder.AddField("Manual Weight", "\u200B");
                                    l_ManualWeightFieldIndex = l_EmbedBuilder.Fields.Count - 1;
                                }

                                string l_OldManualWeightFullText = l_EmbedBuilder.Fields[l_ManualWeightFieldIndex].Value.ToString();
                                if (l_OldManualWeightFullText != null)
                                {
                                    string l_OldManualWeightPartialText = l_OldManualWeightFullText.Replace("(", "").Replace(")", "");
                                    string[] l_SplicedOldManualWeight = l_OldManualWeightPartialText.Split(" ");
                                    if (l_SplicedOldManualWeight.Length >= 2)
                                    {
                                        if (float.TryParse(l_SplicedOldManualWeight[1], out float l_FloatOldManualWeight))
                                        {
                                            l_EmbedBuilder.Fields[l_ManualWeightFieldIndex].Name = l_EmbedBuilder.Fields[l_ManualWeightFieldIndex].Name = $"New Manual Weight";
                                            l_EmbedBuilder.Fields[l_ManualWeightFieldIndex].Value = $"{l_SplicedOldManualWeight[0]} ({l_LastUserMessage})";

                                            await EditMap(l_EditMapArgumentFormat.BSRCode, l_EditMapArgumentFormat.DifficultyName, l_EditMapArgumentFormat.DifficultyCharacteristic, false, p_UserID: l_UserID, p_ChannelID: l_ChannelID, p_ChangeWeight: true, p_NewWeight: l_FloatUserMessage);

                                            await p_MessageComponent.Message.ModifyAsync(p_MessageProperties => p_MessageProperties.Embed = l_EmbedBuilder.AddField("__Manual Weight-Edit Done__", $"Old Manual Weight: {l_OldManualWeightFullText}").Build());
                                            await p_MessageComponent.Message.ModifyAsync(p_MessageProperties => p_MessageProperties.Components = new ComponentBuilder()
                                                .WithButton(new ButtonBuilder("Back", $"BackToEditMapMenu_{l_UserID}"))
                                                .WithButton(new ButtonBuilder("Close Menu", $"ExitEditMap_{l_UserID}", ButtonStyle.Danger)).Build());
                                            l_InteractionResponded = true;
                                        }
                                    }
                                }
                            }
                        }

                        break;

                    case "CustomPassTextChange":
                        l_ComponentBuilder
                            .WithButton(new ButtonBuilder("Validate my choice", $"CustomPassTextChangeValidation_{l_UserID}", ButtonStyle.Success))
                            .WithButton(new ButtonBuilder("Back", $"BackToEditMapMenu_{l_UserID}"))
                            .WithButton(new ButtonBuilder("Close Menu", $"ExitEditMap_{l_UserID}", ButtonStyle.Danger));
                        await p_MessageComponent.Message.ModifyAsync(p_MessageProperties => p_MessageProperties.Embed = l_EmbedBuilder
                            .AddField("\u200B", "\u200B")
                            .AddField("__CustomPassText-Edit__", "Please type the CustomPassText you want the map to have. Then press \"Validate my choice\" => Your next (and last) typed message will be read.").Build());
                        await p_MessageComponent.Message.ModifyAsync(p_MessageProperties => p_MessageProperties.Components = l_ComponentBuilder.Build());
                        l_InteractionResponded = true;
                        break;

                    case "CustomPassTextChangeValidation":
                        l_Messages = await BotHandler.m_Client.GetGuild(Program.m_TempGlobalGuildID).GetTextChannel(p_MessageComponent.Channel.Id).GetMessagesAsync(limit: 20).FlattenAsync();
                        l_LastUserMessage = (from l_Message in l_Messages where l_Message.Author.Id == l_UserID select l_Message.Content).FirstOrDefault();

                        if (l_LastUserMessage != null)
                        {
                            l_EditMapArgumentFormat = GetEditMapArguments(p_MessageComponent);
                            int l_CustomPassTextEditTitleFieldIndex = l_EmbedBuilder.Fields.FindIndex(p_X => p_X.Name.Contains("CustomPassText-Edit"));
                            l_EmbedBuilder.Fields.RemoveAt(l_CustomPassTextEditTitleFieldIndex); /// Removing the "CustomPassText-Edit" Field
                            int l_CustomPassTextFieldIndex = l_EmbedBuilder.Fields.FindIndex(p_X => p_X.Name.Contains("CustomPassText"));
                            if (l_CustomPassTextFieldIndex < 0)
                            {
                                l_EmbedBuilder.AddField("CustomPassTex", "\u200B");
                                l_CustomPassTextFieldIndex = l_EmbedBuilder.Fields.Count - 1;
                            }

                            string l_OldCustomPassText = l_EmbedBuilder.Fields[l_CustomPassTextFieldIndex].Value.ToString();
                            l_EmbedBuilder.Fields[l_CustomPassTextFieldIndex].Name = l_EmbedBuilder.Fields[l_CustomPassTextFieldIndex].Name = $"New CustomPassText";
                            l_EmbedBuilder.Fields[l_CustomPassTextFieldIndex].Value = l_LastUserMessage;

                            await EditMap(l_EditMapArgumentFormat.BSRCode, l_EditMapArgumentFormat.DifficultyName, l_EditMapArgumentFormat.DifficultyCharacteristic, false, p_UserID: l_UserID, p_ChannelID: l_ChannelID, p_ChangeCustomPassText: true, p_NewCustomPassText: l_LastUserMessage);

                            await p_MessageComponent.Message.ModifyAsync(p_MessageProperties => p_MessageProperties.Embed = l_EmbedBuilder.AddField("__CustomPassText-Edit Done__", $"Old CustomPassText: {l_OldCustomPassText}").Build());
                            await p_MessageComponent.Message.ModifyAsync(p_MessageProperties => p_MessageProperties.Components = new ComponentBuilder()
                                .WithButton(new ButtonBuilder("Back", $"BackToEditMapMenu_{l_UserID}"))
                                .WithButton(new ButtonBuilder("Close Menu", $"ExitEditMap_{l_UserID}", ButtonStyle.Danger)).Build());
                            l_InteractionResponded = true;
                        }

                        break;

                    case "NameChange":
                        l_ComponentBuilder
                            .WithButton(new ButtonBuilder("Validate my choice", $"NameChangeValidation_{l_UserID}", ButtonStyle.Success))
                            .WithButton(new ButtonBuilder("Back", $"BackToEditMapMenu_{l_UserID}"))
                            .WithButton(new ButtonBuilder("Close Menu", $"ExitEditMap_{l_UserID}", ButtonStyle.Danger));
                        await p_MessageComponent.Message.ModifyAsync(p_MessageProperties => p_MessageProperties.Embed = l_EmbedBuilder
                            .AddField("\u200B", "\u200B")
                            .AddField("__Name-Edit__", "Please type the name you want the map to have. Then press \"Validate my choice\" => Your next (and last) typed message will be read.").Build());
                        await p_MessageComponent.Message.ModifyAsync(p_MessageProperties => p_MessageProperties.Components = l_ComponentBuilder.Build());
                        l_InteractionResponded = true;
                        break;

                    case "NameChangeValidation":
                        l_Messages = await BotHandler.m_Client.GetGuild(Program.m_TempGlobalGuildID).GetTextChannel(p_MessageComponent.Channel.Id).GetMessagesAsync(limit: 20).FlattenAsync();
                        l_LastUserMessage = (from l_Message in l_Messages where l_Message.Author.Id == l_UserID select l_Message.Content).FirstOrDefault();

                        if (l_LastUserMessage != null)
                        {
                            l_EditMapArgumentFormat = GetEditMapArguments(p_MessageComponent);
                            int l_NameEditTitleFieldIndex = l_EmbedBuilder.Fields.FindIndex(p_X => p_X.Name.Contains("Name-Edit"));
                            l_EmbedBuilder.Fields.RemoveAt(l_NameEditTitleFieldIndex); /// Removing the "Name-Edit" Field
                            string l_OldName = l_EmbedBuilder.Title;
                            l_EmbedBuilder.Title = $"New Name: {l_LastUserMessage}";

                            await EditMap(l_EditMapArgumentFormat.BSRCode, l_EditMapArgumentFormat.DifficultyName, l_EditMapArgumentFormat.DifficultyCharacteristic, false, p_UserID: l_UserID, p_ChannelID: l_ChannelID, p_ChangeName: true, p_NewName: l_LastUserMessage);

                            await p_MessageComponent.Message.ModifyAsync(p_MessageProperties => p_MessageProperties.Embed = l_EmbedBuilder.AddField("__Name-Edit Done__", $"Old Name: {l_OldName}").Build());
                            await p_MessageComponent.Message.ModifyAsync(p_MessageProperties => p_MessageProperties.Components = new ComponentBuilder()
                                .WithButton(new ButtonBuilder("Back", $"BackToEditMapMenu_{l_UserID}"))
                                .WithButton(new ButtonBuilder("Close Menu", $"ExitEditMap_{l_UserID}", ButtonStyle.Danger)).Build());
                            l_InteractionResponded = true;
                        }

                        break;

                    case "RemoveMap":
                        l_ComponentBuilder
                            .WithButton(new ButtonBuilder("Yes", $"RemoveMapValidation_{l_UserID}", ButtonStyle.Danger))
                            .WithButton(new ButtonBuilder("Back", $"BackToEditMapMenu_{l_UserID}"))
                            .WithButton(new ButtonBuilder("Close Menu", $"ExitEditMap_{l_UserID}", ButtonStyle.Danger));
                        await p_MessageComponent.Message.ModifyAsync(p_MessageProperties => p_MessageProperties.Embed = l_EmbedBuilder
                            .AddField("\u200B", "\u200B")
                            .AddField("__RemoveMap__", ":x: Are you 100% sure you want to remove this map?").Build());
                        await p_MessageComponent.Message.ModifyAsync(p_MessageProperties => p_MessageProperties.Components = l_ComponentBuilder.Build());
                        l_InteractionResponded = true;
                        break;

                    case "RemoveMapValidation":
                        l_Messages = await BotHandler.m_Client.GetGuild(Program.m_TempGlobalGuildID).GetTextChannel(p_MessageComponent.Channel.Id).GetMessagesAsync(limit: 20).FlattenAsync();
                        l_LastUserMessage = (from l_Message in l_Messages where l_Message.Author.Id == l_UserID select l_Message.Content).FirstOrDefault();

                        if (l_LastUserMessage != null)
                        {
                            l_EditMapArgumentFormat = GetEditMapArguments(p_MessageComponent);
                            int l_RemoveMapTitleFieldIndex = l_EmbedBuilder.Fields.FindIndex(p_X => p_X.Name.Contains("RemoveMap"));
                            l_EmbedBuilder.Fields.RemoveAt(l_RemoveMapTitleFieldIndex); /// Removing the "RemoveMap" Field

                            await EditMap(l_EditMapArgumentFormat.BSRCode, l_EditMapArgumentFormat.DifficultyName, l_EditMapArgumentFormat.DifficultyCharacteristic, false, p_UserID: l_UserID, p_ChannelID: l_ChannelID, p_RemoveMap: true);
                            
                            await p_MessageComponent.Message.DeleteAsync();
                            l_InteractionResponded = true;
                        }

                        break;

                    case "BackToEditMapMenu":
                        l_EditMapArgumentFormat = GetEditMapArguments(p_MessageComponent);
                        try
                        {
                            await p_MessageComponent.Message.DeleteAsync();
                            l_InteractionResponded = true;
                        }
                        catch (Exception l_E)
                        {
                            Console.WriteLine(l_E);
                        }

                        await EditMap(l_EditMapArgumentFormat.BSRCode, l_EditMapArgumentFormat.DifficultyName, l_EditMapArgumentFormat.DifficultyCharacteristic, p_UserID: l_UserID, p_ChannelID: l_ChannelID);
                        break;

                    case "ExitEditMap":
                        try
                        {
                            await p_MessageComponent.Message.DeleteAsync();
                            l_InteractionResponded = true;
                        }
                        catch (Exception l_E)
                        {
                            Console.WriteLine(l_E);
                        }

                        break;
                }

                if (!l_InteractionResponded)
                {
                    await p_MessageComponent.DeferAsync();
                }
            }
        }

        public async Task EditMapInteraction(SocketInteraction p_Interaction)
        {
            switch (p_Interaction.Type)
            {
                case InteractionType.MessageComponent:
                    SocketMessageComponent l_Interaction = (SocketMessageComponent)p_Interaction;
                    ulong l_UserID = p_Interaction.User.Id;
                    ulong l_ChannelID = p_Interaction.Channel.Id;
                    Embed l_Embed = l_Interaction.Message.Embeds.FirstOrDefault();
                    if (l_Embed != null)
                    {
                        string[] l_SplicedCustomID = l_Embed.Footer.ToString()?.Split("_");
                        bool l_IsCorrectUserID = l_SplicedCustomID != null && l_SplicedCustomID.Any(p_SplicedPartCustomID => p_SplicedPartCustomID == l_UserID.ToString()); /// Find For User's DiscordID in the footer, then compare it to the interaction's UserID.
                        if (l_IsCorrectUserID)
                        {
                            if (l_Interaction.Data.CustomId.Contains("SelectLevelMenu"))
                            {
                                EmbedBuilder l_EmbedBuilder = l_Embed.ToEmbedBuilder();
                                string l_NewLevelID = l_Interaction.Data.Values.FirstOrDefault();
                                int l_LevelEditTitleFieldIndex = l_EmbedBuilder.Fields.FindIndex(p_X => p_X.Name.Contains("Level-Edit"));
                                l_EmbedBuilder.Fields.RemoveAt(l_LevelEditTitleFieldIndex); /// Removing the "Level-Edit" Field
                                int l_LevelFieldIndex = l_EmbedBuilder.Fields.FindIndex(p_X => p_X.Name.Contains("Level"));
                                string l_OldLevelID = l_EmbedBuilder.Fields[l_LevelFieldIndex].Value.ToString();
                                l_EmbedBuilder.Fields[l_LevelFieldIndex].Name = $"New Level";
                                l_EmbedBuilder.Fields[l_LevelFieldIndex].Value = $"Lv.{l_NewLevelID}";
                                await l_Interaction.Message.ModifyAsync(p_MessageProperties => p_MessageProperties.Embed = l_EmbedBuilder.AddField("__Level-Edit Done__", $"Old Level: {l_OldLevelID}").Build());
                                await l_Interaction.Message.ModifyAsync(p_MessageProperties => p_MessageProperties.Components = new ComponentBuilder()
                                    .WithButton(new ButtonBuilder("Back", $"BackToEditMapMenu_{l_UserID}"))
                                    .WithButton(new ButtonBuilder("Close Menu", $"ExitEditMap_{l_UserID}", ButtonStyle.Danger)).Build());
                                EditMapArgumentFormat l_EditMapArgumentFormat = GetEditMapArguments(l_Interaction);

                                if (l_NewLevelID != null && int.TryParse(l_NewLevelID, out int l_IntNewLevelID)) await EditMap(l_EditMapArgumentFormat.BSRCode, l_EditMapArgumentFormat.DifficultyName, l_EditMapArgumentFormat.DifficultyCharacteristic, false, true, l_IntNewLevelID, p_UserID: l_UserID, p_ChannelID: l_ChannelID);
                            }
                        }
                    }
                    await l_Interaction.DeferAsync();
                    break;
            }
        }

        private static EditMapArgumentFormat GetEditMapArguments(SocketMessageComponent p_MessageComponent)
        {
            EmbedBuilder l_EmbedBuilder = p_MessageComponent.Message.Embeds.FirstOrDefault().ToEmbedBuilder();
            int l_DiffFieldIndex = l_EmbedBuilder.Fields.FindIndex(p_X => p_X.Value.ToString() == "Standard" || p_X.Value.ToString() == "Lawless" || p_X.Value.ToString() == "Standard" || p_X.Value.ToString() == "90Degree" || p_X.Value.ToString() == "360Degree" || p_X.Value.ToString() == "NoArrows");
            int l_BSRFieldIndex = l_EmbedBuilder.Fields.FindIndex(p_X => p_X.Name.ToString() == "BSRCode");
            return new EditMapArgumentFormat()
            {
                BSRCode = l_EmbedBuilder.Fields[l_BSRFieldIndex].Value.ToString(),
                DifficultyCharacteristic = l_EmbedBuilder.Fields[l_DiffFieldIndex].Value.ToString(),
                DifficultyName = l_EmbedBuilder.Fields[l_DiffFieldIndex].Name,
            };
        }

        public class EditMapArgumentFormat
        {
            public string BSRCode { get; set; }
            public string DifficultyName { get; set; }
            public string DifficultyCharacteristic { get; set; }
        }

        public class EditMapFormat
        {
            public BeatSaverFormat BeatSaverFormat { get; set; }
            public string SelectedDifficultyName { get; set; }
            public string SelectedCharacteristic { get; set; }
            public int MinScoreRequirement { get; set; }
            public string Category { get; set; }
            public string InfoOnGGP { get; set; }
            public string CustomPassText { get; set; }
            public bool ForceManualWeight { get; set; }
            public float Weighting { get; set; }
            public int NumberOfNote { get; set; }
            public bool adminConfirmationOnPass { get; set; }
        }

        private static bool ChangeMapLevel(EditMapFormat p_EditMapFormat, int p_CurrentLevelID, int p_NewLevelID)
        {
            Level l_CurrentLevel = new Level(p_CurrentLevelID);
            l_CurrentLevel.RemoveMap(p_EditMapFormat.BeatSaverFormat, p_EditMapFormat.SelectedDifficultyName, p_EditMapFormat.SelectedCharacteristic);
            Level l_NewLevel = new Level(p_NewLevelID);
            if (l_CurrentLevel.m_MapRemoved)
                l_NewLevel.AddMap(p_EditMapFormat.BeatSaverFormat, p_EditMapFormat.SelectedDifficultyName, p_EditMapFormat.SelectedCharacteristic, p_EditMapFormat.MinScoreRequirement, p_EditMapFormat.Category, p_EditMapFormat.InfoOnGGP, p_EditMapFormat.CustomPassText, p_EditMapFormat.ForceManualWeight, p_EditMapFormat.Weighting, p_EditMapFormat.NumberOfNote, p_EditMapFormat.adminConfirmationOnPass);

            return l_CurrentLevel.m_MapRemoved && l_NewLevel.m_MapAdded;
        }

        private static bool RemoveMap(EditMapFormat p_EditMapFormat, int p_CurrentLevelID)
        {
            Level l_CurrentLevel = new Level(p_CurrentLevelID);
            l_CurrentLevel.RemoveMap(p_EditMapFormat.BeatSaverFormat, p_EditMapFormat.SelectedDifficultyName, p_EditMapFormat.SelectedCharacteristic);
            return l_CurrentLevel.m_MapRemoved;
        }

        private static void ChangeName(EditMapFormat p_EditMapFormat, int p_LevelID, string p_NewName)
        {
            Level l_Level = new Level(p_LevelID);
            l_Level.AddMap(p_EditMapFormat.BeatSaverFormat, p_EditMapFormat.SelectedDifficultyName, p_EditMapFormat.SelectedCharacteristic, p_EditMapFormat.MinScoreRequirement, p_EditMapFormat.Category, p_EditMapFormat.InfoOnGGP, p_EditMapFormat.CustomPassText, p_EditMapFormat.ForceManualWeight, p_EditMapFormat.Weighting, p_EditMapFormat.NumberOfNote, p_EditMapFormat.adminConfirmationOnPass, null, p_NewName);
        }
    }
}