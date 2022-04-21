using System;
using System.Threading;
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using BSDiscordRanking.Formats.API;
using BSDiscordRanking.Formats.Controller;
using BSDiscordRanking.Formats.Level;
using Discord;
using Discord.Commands;

namespace BSDiscordRanking.Discord.Modules.RankingTeamModule
{
    [PermissionHandler.RequirePermissionAttribute(PERMISSION)]
    public partial class RankingTeamModule : ModuleBase<SocketCommandContext>
    {
        [Command("checkallmaps")]
        [Summary("Check and show all deleted maps, or all maps in which their hash changed inside the ranking.")]
        public async Task CheckAllMaps()
        {
            int l_DeletedMapCount = 0, l_HashChangedMapCount = 0, l_LevelCount = 0;
            string l_DeletedMap = "";
            string l_HashChangedMap = "";
            LevelControllerFormat l_LevelController = LevelController.GetLevelControllerCache();
            if (l_LevelController.LevelID.Count == 0)
            {
                await Context.Channel.SendMessageAsync($"> Add level first lmao.");
                return;
            }

            await Context.Channel.SendMessageAsync($"> Fetching {l_LevelController.LevelID.Count} Level(s)..");
            foreach (int l_LevelID in l_LevelController.LevelID)
            {
                l_LevelCount++;
                
                await Context.Channel.SendMessageAsync($"> {l_LevelCount}/{l_LevelController.LevelID.Count}");
                Level l_Level = new Level(l_LevelID);
                foreach (SongFormat l_Song in l_Level.m_Level.songs)
                {
                    Thread.Sleep(110); /// Act as a rate limiter.
                    BeatSaverFormat l_BeatMap = Level.FetchBeatMap(l_Song.key);
                    if (l_BeatMap == null)
                    {
                        foreach (Difficulty l_SongDifficulty in l_Song.difficulties)
                        {
                            l_DeletedMapCount++;
                            l_DeletedMap += $"> key-{l_Song.key} - {l_Song.name}: *`({l_SongDifficulty.name} - {l_SongDifficulty.characteristic})`* in Lv.{l_LevelID}\n";
                        }
                    }
                    else
                    {
                        if (l_BeatMap.versions.Count != 0)
                        {
                            if (string.Equals(l_Song.hash, l_BeatMap.versions[0].hash, StringComparison.CurrentCultureIgnoreCase)) continue; 

                            foreach (Difficulty l_SongDifficulty in l_Song.difficulties)
                            {
                                l_HashChangedMapCount++;
                                l_HashChangedMap += $"> key-{l_Song.key} - {l_Song.name}: *`({l_SongDifficulty.name} - {l_SongDifficulty.characteristic})`* in Lv.{l_LevelID}\n";
                            }
                        }
                    }
                }
            }

            EmbedBuilder l_EmbedBuilder = new EmbedBuilder();
            

            if (l_DeletedMapCount != 0 || l_HashChangedMapCount != 0)
            {
                if (l_DeletedMapCount != 0)
                {
                    l_EmbedBuilder.WithTitle($"{l_DeletedMapCount} Deleted Map found");
                    l_EmbedBuilder.WithColor(Color.Red);
                    if (l_DeletedMap.Length > 3800)
                    {
                        l_DeletedMap = l_DeletedMap.Substring(0, 3800);
                        l_EmbedBuilder.WithDescription(l_DeletedMap);
                    }
                    else
                    {
                        l_EmbedBuilder.WithDescription(l_DeletedMap);
                    }
                    await Context.Channel.SendMessageAsync(null, false, l_EmbedBuilder.Build());
                }

                if (l_HashChangedMapCount != 0)
                {
                    l_EmbedBuilder.WithTitle($"{l_HashChangedMapCount} Hash changed Map found");
                    l_EmbedBuilder.WithColor(Color.DarkBlue);
                    if (l_HashChangedMap.Length > 3800)
                    {
                        l_HashChangedMap = l_HashChangedMap.Substring(0, 3800);
                        l_EmbedBuilder.WithDescription(l_HashChangedMap);
                    }
                    else
                    {
                        l_EmbedBuilder.WithDescription(l_HashChangedMap);
                    }
                    await Context.Channel.SendMessageAsync(null, false, l_EmbedBuilder.Build());
                }
            }
            else
            {
                l_EmbedBuilder.WithTitle($"No deleted maps / changed hash maps have been found");
                l_EmbedBuilder.WithColor(Color.Green);
                l_EmbedBuilder.WithDescription("Nice job");
                await Context.Channel.SendMessageAsync(null, false, l_EmbedBuilder.Build());
            }
        }
    }
}