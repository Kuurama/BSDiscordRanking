using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using BSDiscordRanking.Formats.API;
using Discord;
using Discord.Commands;

namespace BSDiscordRanking.Discord.Modules.AdminModule
{
    [RequireManagerRole]
    public partial class AdminModule : ModuleBase<SocketCommandContext>
    {
        [Command("addmap")]
        [Alias("editmap", "rankmap")]
        [Summary("Adds a map or updates it from a desired level. Not used fields can be wrote null or 0 depending on their types. (or even ignored if you don't need any of the next ones)")]
        public async Task AddMap(int p_Level = 0, string p_BSRCode = "", string p_DifficultyName = "", string p_Characteristic = "Standard", float p_MinPercentageRequirement = 0f, string p_Category = null, string p_InfoOnGGP = null, string p_CustomPassText = null, bool p_ForceManualWeight = false, float p_Weight = 1f)
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

                if (p_Characteristic is "Lawless" or "Standard" or "90Degree" or "360Degree" or "NoArrows")
                {
                    if (p_DifficultyName is "Easy" or "Normal" or "Hard" or "Expert" or "ExpertPlus")
                    {
                        Level l_Level = new Level(p_Level);
                        BeatSaverFormat l_Map = Level.FetchBeatMap(p_BSRCode, Context);
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
                            LevelController.MapExistFormat l_MapExistCheck = new LevelController().MapExist_Check(l_Map.versions[^1].hash, p_DifficultyName, p_Characteristic, ScoreFromAcc(p_MinPercentageRequirement, l_NumberOfNote), p_Category, p_InfoOnGGP, p_CustomPassText, p_ForceManualWeight, p_Weight);
                            if (!l_MapExistCheck.MapExist && !l_MapExistCheck.DifferentMinScore)
                            {
                                l_Level.AddMap(l_Map, p_DifficultyName, p_Characteristic, ScoreFromAcc(p_MinPercentageRequirement, l_NumberOfNote), p_Category, p_InfoOnGGP, p_CustomPassText, p_ForceManualWeight, p_Weight, l_NumberOfNote, Context);
                                if (!l_Level.m_MapAdded)
                                {
                                    EmbedBuilder l_EmbedBuilder = new EmbedBuilder();
                                    l_EmbedBuilder.WithTitle("Map Added:");
                                    l_EmbedBuilder.WithDescription(l_Map.name);
                                    l_EmbedBuilder.AddField("Difficulty:", p_Characteristic + " - " + p_DifficultyName, true);
                                    l_EmbedBuilder.AddField("Level:", p_Level, true);
                                    l_EmbedBuilder.AddField("ScoreRequirement:", $"{p_MinPercentageRequirement}% ({ScoreFromAcc(p_MinPercentageRequirement, l_NumberOfNote)})", true);
                                    l_EmbedBuilder.AddField("Link:", $"https://beatsaver.com/maps/{l_Map.id}", false);
                                    l_EmbedBuilder.WithFooter("Operated by " + Context.User.Username);
                                    l_EmbedBuilder.WithThumbnailUrl($"https://cdn.beatsaver.com/{l_Map.versions[^1].hash.ToLower()}.jpg");
                                    l_EmbedBuilder.WithColor(Color.Blue);
                                    await Context.Guild.GetTextChannel(ConfigController.GetConfig().LoggingChannel).SendMessageAsync("", false, l_EmbedBuilder.Build());
                                }
                            }
                            else if (l_MapExistCheck.DifferentMinScore || l_MapExistCheck.DifferentCategory || l_MapExistCheck.DifferentInfoOnGGP || l_MapExistCheck.DifferentPassText || l_MapExistCheck.DifferentForceManualWeight || l_MapExistCheck.DifferentWeight)
                            {
                                l_Level.AddMap(l_Map, p_DifficultyName, p_Characteristic, ScoreFromAcc(p_MinPercentageRequirement, l_NumberOfNote), p_Category, p_InfoOnGGP, p_CustomPassText, p_ForceManualWeight, p_Weight, l_NumberOfNote, Context);
                                EmbedBuilder l_EmbedBuilder = new EmbedBuilder();
                                l_EmbedBuilder.WithTitle("Maps infos changed on:");
                                l_EmbedBuilder.WithDescription(l_Map.name);
                                l_EmbedBuilder.AddField("Difficulty:", p_Characteristic + " - " + p_DifficultyName, true);
                                l_EmbedBuilder.AddField("Level:", p_Level, true);

                                if (l_MapExistCheck.DifferentMinScore)
                                    l_EmbedBuilder.AddField("New ScoreRequirement:", $"{p_MinPercentageRequirement}% ({ScoreFromAcc(p_MinPercentageRequirement, l_NumberOfNote)})", false);

                                if (l_MapExistCheck.DifferentCategory)
                                    l_EmbedBuilder.AddField("New Category:", p_Category, false);

                                if (l_MapExistCheck.DifferentInfoOnGGP)
                                    l_EmbedBuilder.AddField("New InfoOnGGP:", p_InfoOnGGP, false);

                                if (l_MapExistCheck.DifferentPassText)
                                    l_EmbedBuilder.AddField("New CustomPassText:", p_CustomPassText, false);

                                if (l_MapExistCheck.DifferentForceManualWeight)
                                    l_EmbedBuilder.AddField("New ManualWeightPreference:", p_ForceManualWeight, false);

                                if (l_MapExistCheck.DifferentWeight)
                                    l_EmbedBuilder.AddField("New Weight:", p_Weight, false);

                                l_EmbedBuilder.AddField("Link:", $"https://beatsaver.com/maps/{l_Map.id}", false);
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
                        await ReplyAsync("> :x: Seems like you didn't entered the difficulty name correctly. Use: \"`Easy,Normal,Hard,Expert or ExpertPlus`\"");
                }
                else
                    await ReplyAsync("> :x: Seems like you didn't entered the characteristic name correctly. Use: \"`Standard,Lawless,90Degree or 360Degree or NoArrows`\"");
                    
            }
        }
    }
}