using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using BSDiscordRanking.Formats.API;
using BSDiscordRanking.Formats.Controller;
using BSDiscordRanking.Formats.Level;
using Discord;
using Discord.Commands;

namespace BSDiscordRanking.Discord.Modules.UserModule
{
    [CheckChannel]
    public partial class UserModule : ModuleBase<SocketCommandContext>
    {
        [Command("getinfo")]
        [Summary("Shows informations about available maps found.")]
        public async Task GetInfo([Remainder] string p_SearchArg)
        {
            p_SearchArg = p_SearchArg.Replace("_", "").Replace(" ", "");
            if (p_SearchArg.Length < 3)
            {
                await ReplyAsync("> :x: Sorry, the minimum for searching a map is 3 characters (excluding '_').");
                return;
            }

            LevelControllerFormat l_LevelControllerFormat = LevelController.GetLevelControllerCache();
            ConfigFormat l_Config = ConfigController.GetConfig();
            List<Tuple<SongFormat, int, float>> l_Maps = new List<Tuple<SongFormat, int, float>>();
            foreach (int l_LevelID in l_LevelControllerFormat.LevelID)
            {
                Level l_Level = new Level(l_LevelID);
                foreach (SongFormat l_Map in l_Level.m_Level.songs)
                    if (l_Map.name.Replace(" ", "").Replace("_", "").Contains(p_SearchArg, StringComparison.OrdinalIgnoreCase))
                        l_Maps.Add(new Tuple<SongFormat, int, float>(l_Map, l_LevelID, l_Level.m_Level.customData.weighting));
            }

            if (l_Maps.Count == 0)
            {
                await ReplyAsync("> :x: Sorry, no maps were found.");
                return;
            }

            int l_NumberOfMapFound = 0;
            int l_NumberOfNotDisplayedMaps = 0;
            string l_NotDisplayedMaps = "";
            foreach (IGrouping<string, Tuple<SongFormat, int, float>> l_GroupedMaps in l_Maps.GroupBy(p_Maps => p_Maps.Item1.hash))
            {
                SongFormat l_Map = l_GroupedMaps.First().Item1;
                if (l_NumberOfMapFound <= l_Config.MaximumNumberOfMapInGetInfo)
                {
                    EmbedBuilder l_EmbedBuilder = new EmbedBuilder();
                    bool l_NewDiff = false;
                    bool l_TooManyFields = false;
                    foreach ((SongFormat l_SongFormat, int l_MapLevelID, float l_Weight) in l_GroupedMaps)
                    {
                        l_TooManyFields = false;
                        BeatSaverFormat l_BeatSaverFormat = Level.FetchBeatMap(l_Map.key);
                        if (l_BeatSaverFormat != null)
                        {
                            if (!string.Equals(l_BeatSaverFormat.versions[^1].hash, l_Map.hash, StringComparison.CurrentCultureIgnoreCase)) l_EmbedBuilder.AddField(":warning: Map hash changed", "The mapper must have changed the currently uploaded map, consider removing it then adding the newest version.");
                        }
                        else
                        {
                            l_EmbedBuilder.AddField(":warning: This map has been deleted from beatsaver", $"The mapper must have deleted the map (key doesn't exist anymore), consider removing that map: (key is {l_Map.key}).");
                        }


                        foreach (Difficulty l_MapDifficulty in l_SongFormat.difficulties)
                        {
                            if (l_EmbedBuilder.Fields.Count > 15)
                            {
                                l_EmbedBuilder.WithDescription("Ranked difficulties:");
                                l_EmbedBuilder.WithTitle($"{l_Map.name} (key-{l_Map.key})");
                                l_EmbedBuilder.WithThumbnailUrl($"https://cdn.beatsaver.com/{l_Map.hash.ToLower()}.jpg");
                                l_EmbedBuilder.WithUrl($"https://beatsaver.com/maps/{l_Map.key}");
                                await Context.Channel.SendMessageAsync("", false, l_EmbedBuilder.Build());
                                l_EmbedBuilder = new EmbedBuilder();
                                l_TooManyFields = true;
                            }

                            if (l_NewDiff) l_EmbedBuilder.AddField("\u200B", "\u200B");

                            l_NewDiff = true;
                            l_EmbedBuilder.AddField(l_MapDifficulty.name, l_MapDifficulty.characteristic, true);
                            l_EmbedBuilder.AddField("Level", $"Lv.{l_MapLevelID}", true);
                            if (l_MapDifficulty.customData.category != null) l_EmbedBuilder.AddField("Category", l_MapDifficulty.customData.category, true);

                            if (l_MapDifficulty.customData.infoOnGGP != null) l_EmbedBuilder.AddField("InfoOnGGP", l_MapDifficulty.customData.infoOnGGP, true);

                            if (l_MapDifficulty.customData.minScoreRequirement > 0) l_EmbedBuilder.AddField("Min Score Requirement", $"{l_MapDifficulty.customData.minScoreRequirement} ({Math.Round((float)l_MapDifficulty.customData.minScoreRequirement / l_MapDifficulty.customData.maxScore * 100f * 100f) / 100f}%)", true);

                            if (l_MapDifficulty.customData.customPassText != null && l_Config.DisplayCustomPassTextInGetInfo) l_EmbedBuilder.AddField("Custom Pass Text", $"{l_MapDifficulty.customData.customPassText}", true);

                            bool l_PassWeightAlreadySet = false;
                            bool l_AccWeightAlreadySet = false;
                            if (l_MapDifficulty.customData.forceManualWeight)
                            {
                                if (!l_Config.OnlyAutoWeightForAccLeaderboard && l_Config.EnableAccBasedLeaderboard)
                                {
                                    l_EmbedBuilder.AddField($"{l_Config.AccPointsName} weight", l_MapDifficulty.customData.manualWeight.ToString("n3"), true);
                                    l_AccWeightAlreadySet = true;
                                }

                                if (!l_Config.OnlyAutoWeightForPassLeaderboard && l_Config.EnablePassBasedLeaderboard)
                                {
                                    l_EmbedBuilder.AddField($"{l_Config.PassPointsName} weight", l_MapDifficulty.customData.manualWeight.ToString("n3"), true);
                                    l_PassWeightAlreadySet = true;
                                }
                            }

                            if (l_Config.AutomaticWeightCalculation)
                            {
                                if (!l_AccWeightAlreadySet && l_Config.OnlyAutoWeightForAccLeaderboard && l_Config.EnableAccBasedLeaderboard)
                                {
                                    l_EmbedBuilder.AddField($"{l_Config.AccPointsName} weight", l_MapDifficulty.customData.AutoWeight.ToString("n3"), true);
                                    l_AccWeightAlreadySet = true;
                                }

                                if (!l_PassWeightAlreadySet && l_Config.OnlyAutoWeightForPassLeaderboard && l_Config.EnablePassBasedLeaderboard)
                                {
                                    l_EmbedBuilder.AddField($"{l_Config.PassPointsName} weight", l_MapDifficulty.customData.AutoWeight.ToString("n3"), true);
                                    l_PassWeightAlreadySet = true;
                                }
                            }

                            if (l_Config.PerPlaylistWeighting)
                            {
                                if (!l_Config.OnlyAutoWeightForAccLeaderboard && !l_AccWeightAlreadySet && l_Config.EnableAccBasedLeaderboard) l_EmbedBuilder.AddField($"{l_Config.AccPointsName} weight", l_Weight.ToString("n3"), true);

                                if (!l_Config.OnlyAutoWeightForPassLeaderboard && !l_PassWeightAlreadySet && l_Config.EnablePassBasedLeaderboard) l_EmbedBuilder.AddField($"{l_Config.PassPointsName} weight", l_Weight.ToString("n3"), true);
                            }

                            l_EmbedBuilder.AddField("Manual Weight", $"{l_MapDifficulty.customData.forceManualWeight.ToString()} ({l_MapDifficulty.customData.manualWeight:n3})", true);

                            l_EmbedBuilder.AddField("Admin Confirmation", l_MapDifficulty.customData.adminConfirmationOnPass.ToString(), true);
                        }
                    }

                    if (l_TooManyFields == false)
                    {
                        l_EmbedBuilder.WithDescription("Ranked difficulties:");
                        l_EmbedBuilder.WithTitle($"{l_Map.name} (key-{l_Map.key})");
                        l_EmbedBuilder.WithThumbnailUrl($"https://cdn.beatsaver.com/{l_Map.hash.ToLower()}.jpg");
                        l_EmbedBuilder.WithUrl($"https://beatsaver.com/maps/{l_Map.key}");
                        await Context.Channel.SendMessageAsync("", false, l_EmbedBuilder.Build());
                    }
                    else
                    {
                        await Context.Channel.SendMessageAsync("", false, l_EmbedBuilder.Build());
                    }
                }
                else
                {
                    foreach ((SongFormat l_SongFormat, int l_MapLevelID, float l_Weight) in l_GroupedMaps)
                    foreach (Difficulty l_MapDifficulty in l_SongFormat.difficulties)
                    {
                        l_NotDisplayedMaps += $"> {l_Map.name}: *`({l_MapDifficulty.name} - {l_MapDifficulty.characteristic})`* in Lv.{l_MapLevelID}\n";
                        l_NumberOfNotDisplayedMaps++;
                    }
                }

                l_NumberOfMapFound++;
            }

            if (l_NumberOfMapFound > l_Config.MaximumNumberOfMapInGetInfo)
            {
                EmbedBuilder l_EmbedBuilder = new EmbedBuilder();
                l_EmbedBuilder.WithColor(new Color(255, 0, 0));
                if (l_NotDisplayedMaps.Length > 3800)
                    l_EmbedBuilder.WithDescription($"{l_NumberOfNotDisplayedMaps} More maps containing those characters were found.\nTo find them: increase the number of characters in your research or increase the MaximumNumberOfMapInGetInfo setting in the config file.\n> Due to too many maps being researched, no map list will be send.");
                else
                    l_EmbedBuilder.WithDescription($"{l_NumberOfNotDisplayedMaps} More maps containing those characters were found\n\nTo find them:\n-increase the number of characters in your research or increase the MaximumNumberOfMapInGetInfo setting in the config file.\n\nHere is a list of the maps you are missing:\n\n" + l_NotDisplayedMaps);

                await Context.Channel.SendMessageAsync("", false, l_EmbedBuilder.Build());
            }
        }
    }
}