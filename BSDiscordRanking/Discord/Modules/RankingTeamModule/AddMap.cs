﻿using System;
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using BSDiscordRanking.Formats.API;
using Discord;
using Discord.Commands;

namespace BSDiscordRanking.Discord.Modules.RankingTeamModule
{
    [PermissionHandler.RequirePermissionAttribute(PERMISSION)]
    public partial class RankingTeamModule : ModuleBase<SocketCommandContext>
    {
        [Command("addmap")]
        [Alias("rankmap")]
        [Summary("Adds a map or updates it from a desired level. Not used fields can be wrote null or 0 depending on their types. (or even ignored if you don't need any of the next ones)")]
        public async Task AddMap(int p_Level = 0, string p_BSRCode = "", string p_DifficultyName = "ExpertPlus", string p_Characteristic = "Standard", float p_MinPercentageRequirement = 0f, string p_Category = null, string p_InfoOnGGP = null, string p_CustomPassText = null, string p_CustomCategoryInfo = null, bool p_AdminPingOnPass = false, bool p_ForceManualWeight = false, float p_Weight = default(float))
        {
            if (string.IsNullOrEmpty(p_BSRCode) || string.IsNullOrEmpty(p_Characteristic) ||
                string.IsNullOrEmpty(p_DifficultyName))
            {
                await ReplyAsync($"> :x: Seems like you didn't used the command correctly, use: `{BotHandler.m_Prefix}addmap [level] [key] [ExpertPlus/Hard..] (Standard/Lawless..)`");
            }
            else
            {
                if (p_Category == "null")
                    p_Category = null;

                if (p_InfoOnGGP == "null")
                    p_InfoOnGGP = null;

                if (p_CustomPassText == "null")
                    p_CustomPassText = null;

                if (p_CustomCategoryInfo == "null")
                    p_CustomCategoryInfo = null;

                p_CustomPassText = p_CustomPassText?.Replace("_", " ");
                p_InfoOnGGP = p_InfoOnGGP?.Replace("_", " ");
                p_Category = p_Category?.Replace("_", " ");


                p_Characteristic = UserModule.UserModule.FirstCharacterToUpper(p_Characteristic);
                p_DifficultyName = UserModule.UserModule.FirstCharacterToUpper(p_DifficultyName);

                switch (p_DifficultyName.ToLower())
                {
                    case "expertplus":
                        p_DifficultyName = "ExpertPlus";
                        break;
                    case "expert":
                        p_DifficultyName = "Expert";
                        break;
                    case "hard":
                        p_DifficultyName = "Hard";
                        break;
                    case "normal":
                        p_DifficultyName = "Normal";
                        break;
                    case "easy":
                        p_DifficultyName = "Easy";
                        break;
                }

                switch (p_Characteristic)
                {
                    // ReSharper disable once StringLiteralTypo
                    case "90degree" or "90degres" or "90degre":
                        p_Characteristic = "90Degree";
                        break;
                    // ReSharper disable once StringLiteralTypo
                    case "360degree" or "360degres" or "360degre":
                        p_Characteristic = "360Degree";
                        break;
                    // ReSharper disable once StringLiteralTypo
                    case "Noarrows" or "Noarrow" or "noarrows" or "noarrow":
                        p_Characteristic = "NoArrows";
                        break;
                }

                if (p_Characteristic is "Lawless" or "Standard" or "90Degree" or "360Degree" or "NoArrows")
                {
                    if (p_DifficultyName is "Easy" or "Normal" or "Hard" or "Expert" or "ExpertPlus")
                    {
                        Level l_Level = new Level(p_Level);
                        BeatSaverFormat l_Map = Level.FetchBeatMap(p_BSRCode, Context);
                        if (l_Map is null)
                        {
                            return;
                        }
                        int l_NumberOfNote = 0;
                        int l_MaxScore = 0;
                        bool l_DiffExist = false;
                        foreach (Diff l_Diff in l_Map.versions[^1].diffs)
                            if (l_Diff.characteristic == p_Characteristic && l_Diff.difficulty == p_DifficultyName)
                            {
                                l_NumberOfNote = l_Diff.notes;
                                l_MaxScore = l_Diff.maxScore;
                                l_DiffExist = true;
                            }

                        if (l_DiffExist)
                        {
                            LevelController.MapExistFormat l_MapExistCheck = LevelController.MapExist_Check(l_Map.versions[^1].hash, p_DifficultyName, p_Characteristic, (int)(l_MaxScore * p_MinPercentageRequirement/100f), p_Category, p_CustomCategoryInfo, p_InfoOnGGP, p_CustomPassText, p_ForceManualWeight, p_Weight, p_AdminPingOnPass);
                            if (!l_MapExistCheck.MapExist && !l_MapExistCheck.DifferentMinScore)
                            {
                                l_Level.AddMap(l_Map, p_DifficultyName, p_Characteristic, (int)(l_MaxScore * p_MinPercentageRequirement/100f), p_Category, p_CustomCategoryInfo, p_InfoOnGGP, p_CustomPassText, p_ForceManualWeight, p_Weight, l_MaxScore, p_AdminPingOnPass);
                                if (l_Level.m_MapAdded)
                                {
                                    EmbedBuilder l_EmbedBuilder = new EmbedBuilder();
                                    l_EmbedBuilder.WithTitle("Map Added:");
                                    l_EmbedBuilder.WithDescription(l_Map.name);
                                    l_EmbedBuilder.AddField("Difficulty:", p_Characteristic + " - " + p_DifficultyName, true);
                                    l_EmbedBuilder.AddField("Level:", p_Level, true);

                                    await Context.Channel.SendMessageAsync("", false, l_EmbedBuilder.Build());

                                    if (Math.Abs(p_MinPercentageRequirement - default(float)) > 0.01)
                                        l_EmbedBuilder.AddField("ScoreRequirement:", $"{p_MinPercentageRequirement}% ({(int)(l_MaxScore * p_MinPercentageRequirement/100f)})");

                                    if (p_Category != null)
                                        l_EmbedBuilder.AddField("Category:", p_Category);

                                    if (p_CustomCategoryInfo != null)
                                        l_EmbedBuilder.AddField("CustomCategoryInfo:", p_CustomCategoryInfo);

                                    if (p_InfoOnGGP != null)
                                        l_EmbedBuilder.AddField("InfoOnGGP:", p_InfoOnGGP);

                                    if (p_CustomPassText != null)
                                        l_EmbedBuilder.AddField("CustomPassText:", p_CustomPassText);

                                    if (p_ForceManualWeight)
                                        l_EmbedBuilder.AddField("ManualWeightPreference:", true);

                                    if (Math.Abs(p_Weight - default(float)) > 0.01)
                                        l_EmbedBuilder.AddField("Weight:", p_Weight);

                                    if (p_AdminPingOnPass)
                                        l_EmbedBuilder.AddField("Admin Confirmation Preference:", true);

                                    l_EmbedBuilder.AddField("Link:", $"https://beatsaver.com/maps/{l_Map.id}");
                                    l_EmbedBuilder.WithFooter("Operated by " + Context.User.Username);
                                    l_EmbedBuilder.WithThumbnailUrl($"https://cdn.beatsaver.com/{l_Map.versions[^1].hash.ToLower()}.jpg");
                                    l_EmbedBuilder.WithColor(Color.Blue);
                                    await Context.Guild.GetTextChannel(ConfigController.GetConfig().LoggingChannel).SendMessageAsync("", false, l_EmbedBuilder.Build());
                                }
                            }
                            else if (l_MapExistCheck.MapExist && l_MapExistCheck.Level == p_Level && (l_MapExistCheck.DifferentMinScore || l_MapExistCheck.DifferentCategory || l_MapExistCheck.DifferentCustomCategoryInfo || l_MapExistCheck.DifferentInfoOnGGP || l_MapExistCheck.DifferentPassText || l_MapExistCheck.DifferentForceManualWeight || l_MapExistCheck.DifferentWeight))
                            {
                                l_Level.AddMap(l_Map, p_DifficultyName, p_Characteristic, (int)(l_MaxScore * p_MinPercentageRequirement/100f), p_Category, p_CustomCategoryInfo, p_InfoOnGGP, p_CustomPassText, p_ForceManualWeight, p_Weight, l_MaxScore, p_AdminPingOnPass);
                                EmbedBuilder l_EmbedBuilder = new EmbedBuilder();
                                l_EmbedBuilder.WithTitle("Maps infos changed on:");
                                l_EmbedBuilder.WithDescription(l_Map.name);
                                l_EmbedBuilder.AddField("Difficulty:", p_Characteristic + " - " + p_DifficultyName, true);
                                l_EmbedBuilder.AddField("Level:", p_Level, true);

                                if (l_MapExistCheck.DifferentMinScore)
                                    l_EmbedBuilder.AddField("New ScoreRequirement:", $"{p_MinPercentageRequirement}% ({(int)(l_MaxScore * p_MinPercentageRequirement/100f)})");

                                if (l_MapExistCheck.DifferentCategory && p_Category != null)
                                    l_EmbedBuilder.AddField("New Category:", p_Category);

                                if (l_MapExistCheck.DifferentCustomCategoryInfo && p_CustomCategoryInfo != null)
                                    l_EmbedBuilder.AddField("New CustomCategoryInfo:", p_CustomCategoryInfo);

                                if (l_MapExistCheck.DifferentInfoOnGGP && p_InfoOnGGP != null)
                                    l_EmbedBuilder.AddField("New InfoOnGGP:", p_InfoOnGGP);

                                if (l_MapExistCheck.DifferentPassText && p_CustomPassText != null)
                                    l_EmbedBuilder.AddField("New CustomPassText:", p_CustomPassText);

                                if (l_MapExistCheck.DifferentForceManualWeight)
                                    l_EmbedBuilder.AddField("New ManualWeightPreference:", p_ForceManualWeight);

                                if (l_MapExistCheck.DifferentWeight)
                                    l_EmbedBuilder.AddField("New Weight:", p_Weight);

                                l_EmbedBuilder.AddField("Link:", $"https://beatsaver.com/maps/{l_Map.id}");
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
                            await Context.Channel.SendMessageAsync($"The diff {p_DifficultyName} - {p_Characteristic} doesn't exist in this BeatMap");
                        }
                    }
                    else
                    {
                        await ReplyAsync("> :x: Seems like you didn't entered the difficulty name correctly. Use: \"`Easy,Normal,Hard,Expert or ExpertPlus`\"");
                    }
                }
                else
                {
                    await ReplyAsync("> :x: Seems like you didn't entered the characteristic name correctly. Use: \"`Standard,Lawless,90Degree or 360Degree or NoArrows`\"");
                }
            }
        }
    }
}
