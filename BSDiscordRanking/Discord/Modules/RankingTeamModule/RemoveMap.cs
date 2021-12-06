using System.Collections.Generic;
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
        [Command("removemap")]
        [Alias("deletemap")]
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
                if (p_DifficultyName is "Easy" or "Normal" or "Hard" or "Expert" or "ExpertPlus")
                {
                    if (p_Characteristic is "Lawless" or "Standard" or "90Degree" or "360Degree")
                    {
                        BeatSaverFormat l_Map = Level.FetchBeatMap(p_Code);
                        bool l_MapDeleted = false;
                        if (l_Map is null)
                        {
                            l_MapDeleted = true;
                            Diff l_Diff = new Diff
                            {
                                characteristic = p_Characteristic,
                                difficulty = p_DifficultyName
                            };
                            Version l_Version = new Version
                            {
                                hash = null,
                                key = p_Code,
                                diffs = new List<Diff> { l_Diff }
                            };
                            l_Map = new BeatSaverFormat
                            {
                                id = p_Code,
                                versions = new List<Version> { l_Version }
                            };
                        }

                        LevelController.MapExistFormat l_MapExistCheck = LevelController.MapExist_Check(l_Map.versions[^1].hash, p_DifficultyName, p_Characteristic, 0, null, null, null, null, false, 1f, false, l_Map.id);
                        if (l_MapExistCheck.MapExist)
                        {
                            Level l_Level = new Level(l_MapExistCheck.Level);
                            l_Level.RemoveMap(l_Map, p_DifficultyName, p_Characteristic, Context);
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
                                if (!l_MapDeleted)
                                {
                                    l_EmbedBuilder.WithTitle("Map removed!");
                                    l_EmbedBuilder.AddField("Link:", $"https://beatsaver.com/maps/{l_Map.id}");
                                    l_EmbedBuilder.WithThumbnailUrl($"https://cdn.beatsaver.com/{l_Map.versions[^1].hash.ToLower()}.jpg");
                                }
                                else
                                {
                                    l_EmbedBuilder.WithTitle("Map removed! (wasn't on BeatSaver anymore)");
                                    l_EmbedBuilder.AddField("Old Link:", $"https://beatsaver.com/maps/{l_Map.id}");
                                }

                                l_EmbedBuilder.AddField("Map name:", l_MapExistCheck.Name);
                                l_EmbedBuilder.AddField("Difficulty:", p_Characteristic + " - " + p_DifficultyName);
                                l_EmbedBuilder.AddField("Level:", l_MapExistCheck.Level);
                            }

                            l_EmbedBuilder.WithFooter("Operated by " + Context.User.Username);
                            l_EmbedBuilder.WithColor(Color.Red);
                            await Context.Guild.GetTextChannel(ConfigController.GetConfig().LoggingChannel).SendMessageAsync("", false, l_EmbedBuilder.Build());
                        }
                        else
                        {
                            await ReplyAsync("> :x: Sorry, this map difficulty isn't in any levels.");
                        }
                    }
                    else
                    {
                        await ReplyAsync("> :x: Seems like you didn't entered the characteristic name correctly. Use: \"`Standard,Lawless,90Degree or 360Degree`\"");
                    }
                }
                else
                {
                    await ReplyAsync("> :x: Seems like you didn't entered the difficulty name correctly. Use: \"`Easy,Normal,Hard,Expert or ExpertPlus`\"");
                }
            }
        }
    }
}