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

namespace BSDiscordRanking.Discord.Modules.EditorModule
{
    [PermissionHandler.RequirePermissionAttribute(Permission)]
    public partial class EditorModule : ModuleBase<SocketCommandContext>
    {
        [Command("editmap")]
        [Alias("rankedit")]
        [Summary("Open the Edit-Map management menu.")]
        private async Task EditMap(string p_BSRCode = null, string p_DifficultyName = "ExpertPlus", string p_Characteristic = "Standard", [Summary("DoNotDisplayOnHelp")]bool p_DisplayEditMap = true, [Summary("DoNotDisplayOnHelp")]bool p_ChangeLevel = false, [Summary("DoNotDisplayOnHelp")]int p_NewLevel = default, [Summary("DoNotDisplayOnHelp")]bool p_ChangeMinPercentageRequirement = false, [Summary("DoNotDisplayOnHelp")]int p_NewMinPercentageRequirement = default, [Summary("DoNotDisplayOnHelp")]bool p_ChangeCategory = false, [Summary("DoNotDisplayOnHelp")]string p_NewCategory = null, [Summary("DoNotDisplayOnHelp")]bool p_ChangeInfoOnGGP = false, [Summary("DoNotDisplayOnHelp")]string p_NewInfoOnGGP = null, [Summary("DoNotDisplayOnHelp")]bool p_ChangeCustomPassText = false, [Summary("DoNotDisplayOnHelp")]string p_NewCustomPassText = null, [Summary("DoNotDisplayOnHelp")]bool p_ToggleManualWeight = false, [Summary("DoNotDisplayOnHelp")]bool p_ChangeWeight = false, [Summary("DoNotDisplayOnHelp")]float p_NewWeight = default,
            [Summary("DoNotDisplayOnHelp")]ulong p_UserID = default)
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

                BeatSaverFormat l_Map = Level.FetchBeatMap(p_BSRCode);
                EmbedBuilder l_EmbedBuilder = new EmbedBuilder();
                if (l_Map is null)
                {
                    l_EmbedBuilder.WithTitle("Sorry this map's BSRCode doesn't exist on BeatSaver.");
                    if (Context != null)
                    {
                        await Context.Channel.SendMessageAsync("", false, l_EmbedBuilder.Build(),
                            component: new ComponentBuilder()
                                .WithButton(new ButtonBuilder("Close Menu", "ExitLevelEdit", ButtonStyle.Danger))
                                .Build());
                    }
                }
                else
                {
                    ConfigFormat l_Config = ConfigController.GetConfig();
                    LevelController.MapExistFormat l_MapExistCheck = LevelController.MapExist_Check(l_Map.versions[^1].hash, p_DifficultyName, p_Characteristic, p_NewMinPercentageRequirement, p_NewCategory, p_NewInfoOnGGP, p_NewCustomPassText, false, p_NewWeight);
                    if (l_MapExistCheck.MapExist)
                    {
                        Level l_Level = new Level(l_MapExistCheck.Level);
                        float l_Weight = l_Level.m_Level.customData.weighting;
                        foreach (var l_LevelSong in l_Level.m_Level.songs)
                        {
                            if (string.Equals(l_LevelSong.hash, l_Map.versions[^1].hash, StringComparison.CurrentCultureIgnoreCase))
                            {
                                foreach (var l_LevelDiff in l_LevelSong.difficulties)
                                {
                                    if (l_LevelDiff.characteristic == p_Characteristic && l_LevelDiff.name == p_DifficultyName)
                                    {
                                        l_EmbedBuilder.AddField("BSRCode:", p_BSRCode, true);
                                        l_EmbedBuilder.AddField(l_LevelDiff.name, l_LevelDiff.characteristic, true);
                                        l_EmbedBuilder.AddField("Level:", $"Lv.{l_MapExistCheck.Level}", true);
                                        if (l_LevelDiff.customData.category != null)
                                        {
                                            l_EmbedBuilder.AddField("Category:", l_LevelDiff.customData.category, true);
                                        }

                                        if (l_LevelDiff.customData.minScoreRequirement > 0)
                                        {
                                            l_EmbedBuilder.AddField("Min Score Requirement:", $"{l_LevelDiff.customData.minScoreRequirement} ({Math.Round((float)l_LevelDiff.customData.minScoreRequirement / l_LevelDiff.customData.maxScore * 100f * 100f) / 100f}%)", true);
                                        }

                                        l_EmbedBuilder.AddField("Manual Weight:", $"{l_LevelDiff.customData.forceManualWeight.ToString()}", true);

                                        bool l_PassWeightAlreadySet = false;
                                        bool l_AccWeightAlreadySet = false;
                                        if (l_LevelDiff.customData.forceManualWeight)
                                        {
                                            if (!l_Config.OnlyAutoWeightForAccLeaderboard && l_Config.EnableAccBasedLeaderboard)
                                            {
                                                l_EmbedBuilder.AddField($"{l_Config.AccPointsName} weight:", l_LevelDiff.customData.manualWeight, true);
                                                l_AccWeightAlreadySet = true;
                                            }

                                            if (!l_Config.OnlyAutoWeightForPassLeaderboard && l_Config.EnablePassBasedLeaderboard)
                                            {
                                                l_EmbedBuilder.AddField($"{l_Config.PassPointsName} weight:", l_LevelDiff.customData.manualWeight, true);
                                                l_PassWeightAlreadySet = true;
                                            }
                                        }

                                        if (l_LevelDiff.customData.AutoWeight > 0 && l_Config.AutomaticWeightCalculation)
                                        {
                                            if (!l_AccWeightAlreadySet && l_Config.OnlyAutoWeightForAccLeaderboard && l_Config.EnableAccBasedLeaderboard)
                                            {
                                                l_EmbedBuilder.AddField($"{l_Config.AccPointsName} weight:", l_LevelDiff.customData.AutoWeight, true);
                                                l_AccWeightAlreadySet = true;
                                            }

                                            if (!l_PassWeightAlreadySet && l_Config.OnlyAutoWeightForPassLeaderboard && l_Config.EnablePassBasedLeaderboard)
                                            {
                                                l_EmbedBuilder.AddField($"{l_Config.PassPointsName} weight:", l_LevelDiff.customData.AutoWeight, true);
                                                l_PassWeightAlreadySet = true;
                                            }
                                        }

                                        if (l_Config.PerPlaylistWeighting)
                                        {
                                            if (!l_Config.OnlyAutoWeightForAccLeaderboard && !l_AccWeightAlreadySet && l_Config.EnableAccBasedLeaderboard)
                                            {
                                                l_EmbedBuilder.AddField($"{l_Config.AccPointsName} weight:", l_Weight, true);
                                            }

                                            if (!l_Config.OnlyAutoWeightForPassLeaderboard && !l_PassWeightAlreadySet && l_Config.EnablePassBasedLeaderboard)
                                            {
                                                l_EmbedBuilder.AddField($"{l_Config.PassPointsName} weight:", l_Weight, true);
                                            }
                                        }

                                        l_EmbedBuilder.WithTitle(l_LevelSong.name);
                                        l_EmbedBuilder.WithThumbnailUrl($"https://cdn.beatsaver.com/{l_LevelSong.hash.ToLower()}.jpg");
                                        l_EmbedBuilder.WithUrl($"https://beatsaver.com/maps/{Level.FetchBeatMapByHash(l_LevelSong.hash, Context).id}");

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
                                            Weighting = l_Weight
                                        };

                                        if (p_DisplayEditMap)
                                        {
                                            if (Context != null)
                                            {
                                                l_EmbedBuilder.WithFooter($"DiscordID_{Context.User.Id}");
                                                await Context.Channel.SendMessageAsync("", false, l_EmbedBuilder.Build(),
                                                    component: new ComponentBuilder()
                                                        .WithButton(new ButtonBuilder("Change Level", $"LevelIDChange_{Context.User.Id}", ButtonStyle.Secondary))
                                                        .WithButton(new ButtonBuilder("Change MinPercentageRequirement", $"MinPercentageRequirementChange_{Context.User.Id}", ButtonStyle.Secondary))
                                                        .WithButton(new ButtonBuilder("Change Category", $"CategoryChange_{Context.User.Id}", ButtonStyle.Secondary))
                                                        .WithButton(new ButtonBuilder("Change InfoOnGGP", $"InfoOnGGPChange_{Context.User.Id}", ButtonStyle.Secondary))
                                                        .WithButton(new ButtonBuilder("Change CustomPassText", $"CustomPassTextChange_{Context.User.Id}", ButtonStyle.Secondary))
                                                        .WithButton(new ButtonBuilder("Toggle Manual Weight", $"ToggleManualWeight_{Context.User.Id}", ButtonStyle.Secondary))
                                                        .WithButton(new ButtonBuilder("Close Menu", $"ExitLevelEdit_{Context.User.Id}", ButtonStyle.Danger))
                                                        .Build());
                                            }
                                        }

                                        if (p_ChangeLevel)
                                        {
                                            ChangeMapLevel(l_EditMapFormat, l_MapExistCheck.Level, p_NewLevel);
                                        }
                                        else if (p_ChangeMinPercentageRequirement || p_ChangeCategory || p_ChangeInfoOnGGP || p_ChangeCustomPassText || p_ToggleManualWeight)
                                        {
                                            if (l_MapExistCheck.DifferentMinScore || l_MapExistCheck.DifferentCategory || l_MapExistCheck.DifferentInfoOnGGP || l_MapExistCheck.DifferentPassText || l_MapExistCheck.DifferentForceManualWeight || l_MapExistCheck.DifferentWeight)
                                            {
                                                if (!p_ChangeMinPercentageRequirement)
                                                {
                                                    p_NewMinPercentageRequirement = l_LevelDiff.customData.minScoreRequirement;
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

                                                l_Level.AddMap(l_Map, p_DifficultyName, p_Characteristic, p_NewMinPercentageRequirement, p_NewCategory, p_NewInfoOnGGP, p_NewCustomPassText, l_EditMapFormat.ForceManualWeight, p_NewWeight, l_LevelDiff.customData.noteCount, null);
                                                EmbedBuilder l_MapChangeEmbedBuilder = new EmbedBuilder();
                                                l_MapChangeEmbedBuilder.WithTitle("Maps infos changed on:");
                                                l_MapChangeEmbedBuilder.WithDescription(l_Map.name);
                                                l_MapChangeEmbedBuilder.AddField("Difficulty:", p_Characteristic + " - " + p_DifficultyName, true);
                                                l_MapChangeEmbedBuilder.AddField("Level:", l_MapExistCheck.Level, true);

                                                if (l_MapExistCheck.DifferentMinScore)
                                                    l_MapChangeEmbedBuilder.AddField("New ScoreRequirement:", $"{p_NewMinPercentageRequirement}% ({AdminModule.AdminModule.ScoreFromAcc(p_NewMinPercentageRequirement, l_LevelDiff.customData.noteCount)})", false);

                                                if (l_MapExistCheck.DifferentCategory)
                                                    l_MapChangeEmbedBuilder.AddField("New Category:", p_NewCategory, false);

                                                if (l_MapExistCheck.DifferentInfoOnGGP)
                                                    l_MapChangeEmbedBuilder.AddField("New InfoOnGGP:", p_NewInfoOnGGP, false);

                                                if (l_MapExistCheck.DifferentPassText)
                                                    l_MapChangeEmbedBuilder.AddField("New CustomPassText:", p_NewCustomPassText, false);

                                                if (l_MapExistCheck.DifferentForceManualWeight)
                                                    l_MapChangeEmbedBuilder.AddField("New ManualWeightPreference:", l_EditMapFormat.ForceManualWeight, false);

                                                if (l_MapExistCheck.DifferentWeight)
                                                    l_MapChangeEmbedBuilder.AddField($"New manual weight:", p_NewWeight, false);

                                                l_MapChangeEmbedBuilder.AddField("Link:", $"https://beatsaver.com/maps/{l_Map.id}", false);
                                                if (p_UserID != default) l_EmbedBuilder.WithFooter($"Operated by <@{p_UserID}>");
                                                l_MapChangeEmbedBuilder.WithThumbnailUrl($"https://cdn.beatsaver.com/{l_Map.versions[^1].hash.ToLower()}.jpg");
                                                l_MapChangeEmbedBuilder.WithColor(Color.Blue);

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
                                    else
                                    {
                                        break;
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
                                    .WithButton(new ButtonBuilder("Close Menu", "ExitLevelEdit", ButtonStyle.Danger))
                                    .Build());
                        }
                    }
                }
            }
        }

        public async Task LevelEditButtonHandler(SocketMessageComponent p_MessageComponent)
        {
            ulong l_UserID = p_MessageComponent.User.Id;
            string[] l_SplicedCustomID = p_MessageComponent.Data.CustomId.Split("_"); /// I choosed to use the button custom ID because adding buttons at a different place or command and making the DiscordID built in footer (or else) of an Embed wouldn't be good (as it would always need an Embed and a Footer).
            bool l_IsCorrectUserID = l_SplicedCustomID.Any(p_SplicedPartCustomID => p_SplicedPartCustomID == l_UserID.ToString());
            if (l_IsCorrectUserID)
            {
                switch (l_SplicedCustomID[0])
                {
                    case "LevelIDChange":
                        List<SelectMenuOptionBuilder> l_SelectMenuOptionBuilders = new List<SelectMenuOptionBuilder>();
                        List<List<SelectMenuOptionBuilder>> l_ListListMenuOptionBuilder = new List<List<SelectMenuOptionBuilder>>() { new List<SelectMenuOptionBuilder>() };
                        ComponentBuilder l_ComponentBuilder = new ComponentBuilder();
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

                        EmbedBuilder l_EmbedBuilder = p_MessageComponent.Message.Embeds.FirstOrDefault().ToEmbedBuilder();
                        l_ComponentBuilder.WithButton(new ButtonBuilder("Close Menu", "ExitLevelEdit", ButtonStyle.Danger));
                        await p_MessageComponent.Message.ModifyAsync(p_MessageProperties => p_MessageProperties.Embed = l_EmbedBuilder.AddField("Level-Edit:", "Please choose the level you want this difficulty to be in.").Build());
                        await p_MessageComponent.Message.ModifyAsync(p_MessageProperties => p_MessageProperties.Components = l_ComponentBuilder.Build());
                        break;

                    case "ExitLevelEdit":
                        await p_MessageComponent.Message.DeleteAsync();
                        break;

                    case "CategoryChange":
                        EditMapArgumentFormat l_EditMapArgumentFormat = GetEditMapArguments(p_MessageComponent);
                        await EditMap(l_EditMapArgumentFormat.BSRCode, l_EditMapArgumentFormat.DifficultyName, l_EditMapArgumentFormat.DifficultyCharacteristic, false, p_ChangeCategory: true, p_NewCategory: "shitpost");
                        break;
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
                                l_EmbedBuilder.Fields.RemoveAt(l_EmbedBuilder.Fields.Count - 1);
                                int l_LevelFieldIndex = l_EmbedBuilder.Fields.FindIndex(p_X => p_X.Name.Contains("Level"));
                                string l_OldLevelID = l_EmbedBuilder.Fields[l_LevelFieldIndex].Value.ToString();
                                l_EmbedBuilder.Fields[l_LevelFieldIndex].Name = $"New Level:";
                                l_EmbedBuilder.Fields[l_LevelFieldIndex].Value = $"Lv.{l_NewLevelID}";
                                await l_Interaction.Message.ModifyAsync(p_MessageProperties => p_MessageProperties.Embed = l_EmbedBuilder.AddField("Level-Edit Done:", $"Old Level: {l_OldLevelID}").Build());
                                await l_Interaction.Message.ModifyAsync(p_MessageProperties => p_MessageProperties.Components = new ComponentBuilder().WithButton(new ButtonBuilder("Close Menu", "ExitLevelEdit", ButtonStyle.Danger)).Build());

                                EditMapArgumentFormat l_EditMapArgumentFormat = GetEditMapArguments(l_Interaction);

                                if (l_NewLevelID != null && int.TryParse(l_NewLevelID, out int l_IntNewLevelID)) await EditMap(l_EditMapArgumentFormat.BSRCode, l_EditMapArgumentFormat.DifficultyName, l_EditMapArgumentFormat.DifficultyCharacteristic, false, true, l_IntNewLevelID);
                            }
                        }
                    }

                    break;
            }
        }

        private static EditMapArgumentFormat GetEditMapArguments(SocketMessageComponent p_MessageComponent)
        {
            EmbedBuilder l_EmbedBuilder = p_MessageComponent.Message.Embeds.FirstOrDefault().ToEmbedBuilder();
            int l_DiffFieldIndex = l_EmbedBuilder.Fields.FindIndex(p_X => p_X.Value.ToString() == "Standard" || p_X.Value.ToString() == "Lawless" || p_X.Value.ToString() == "Standard" || p_X.Value.ToString() == "90Degree" || p_X.Value.ToString() == "360Degree" || p_X.Value.ToString() == "NoArrows");
            int l_BSRFieldIndex = l_EmbedBuilder.Fields.FindIndex(p_X => p_X.Name.ToString() == "BSRCode:");
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
        }

        public static void ChangeMapLevel(EditMapFormat p_EditMapFormat, int p_CurrentLevelID, int p_NewLevelID)
        {
            Level l_CurrentLevel = new Level(p_CurrentLevelID);
            l_CurrentLevel.RemoveMap(p_EditMapFormat.BeatSaverFormat, p_EditMapFormat.SelectedDifficultyName, p_EditMapFormat.SelectedCharacteristic);
            Level l_NewLevel = new Level(p_NewLevelID);
            l_NewLevel.AddMap(p_EditMapFormat.BeatSaverFormat, p_EditMapFormat.SelectedDifficultyName, p_EditMapFormat.SelectedCharacteristic, p_EditMapFormat.MinScoreRequirement, p_EditMapFormat.Category, p_EditMapFormat.InfoOnGGP, p_EditMapFormat.CustomPassText, p_EditMapFormat.ForceManualWeight, p_EditMapFormat.Weighting, p_EditMapFormat.NumberOfNote);
        }
    }
}