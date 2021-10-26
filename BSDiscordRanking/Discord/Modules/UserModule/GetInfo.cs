using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
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
        public async Task GetInfo(string p_SearchArg)
        {
            p_SearchArg = p_SearchArg.Replace("_", "");
            if (p_SearchArg.Length < 3)
            {
                await ReplyAsync("> :x: Sorry, the minimum for searching a map is 3 characters (excluding '_').");
                return;
            }

            var l_Maps = new List<Tuple<SongFormat, int>>();
            new LevelController().FetchLevel();
            foreach (var l_LevelID in LevelController.GetLevelControllerCache().LevelID)
            {
                var l_Level = new Level(l_LevelID);
                foreach (var l_Map in l_Level.m_Level.songs)
                {
                    if (l_Map.name.Replace(" ", "").Replace("_", "").Contains(p_SearchArg, StringComparison.OrdinalIgnoreCase))
                    {
                        l_Maps.Add(new Tuple<SongFormat, int>(l_Map, l_LevelID));
                    }
                }
            }

            if (l_Maps.Count == 0)
            {
                await ReplyAsync("> :x: Sorry, no maps was found.");
                return;
            }

            foreach (var l_GroupedMaps in l_Maps.GroupBy(p_Maps => p_Maps.Item1.hash))
            {
                EmbedBuilder l_EmbedBuilder = new EmbedBuilder();
                foreach (var (l_SongFormat, l_Item2) in l_GroupedMaps)
                {
                    foreach (var l_MapDifficulty in l_SongFormat.difficulties)
                    {
                        l_EmbedBuilder.AddField(l_MapDifficulty.name, l_MapDifficulty.characteristic, true);
                        l_EmbedBuilder.AddField("Level:", $"Lv.{l_Item2}", true);
                        l_EmbedBuilder.AddField("\u200B", "\u200B", true);
                    }
                }

                var l_Map = l_GroupedMaps.First().Item1;

                l_EmbedBuilder.WithDescription("Ranked difficulties:");
                l_EmbedBuilder.WithTitle(l_Map.name);
                l_EmbedBuilder.WithThumbnailUrl($"https://cdn.beatsaver.com/{l_Map.hash.ToLower()}.jpg");
                l_EmbedBuilder.WithUrl($"https://beatsaver.com/maps/{Level.FetchBeatMapByHash(l_Map.hash, Context).id}");
                await Context.Channel.SendMessageAsync("", false, l_EmbedBuilder.Build());
            }
        }
    }
}