using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using BSDiscordRanking.Formats;
using BSDiscordRanking.Formats.Level;
using BSDiscordRanking.Formats.Player;
using Discord;
using Discord.Commands;

namespace BSDiscordRanking.Discord.Modules.UserModule
{
    [CheckChannel]
    public partial class UserModule : ModuleBase<SocketCommandContext>
    {
        [Command("ggp")]
        [Alias("getgrindpool")]
        [Summary("Shows the Level's maps while displaying your passes, not giving a specific level will display the next available level you might want to grind.")]
        public async Task GetGrindPool(int p_Level = -1)
        {
            bool l_CheckForLastGGP = false;
            Player l_Player = new Player(UserController.GetPlayer(Context.User.Id.ToString()));
            try
            {
                if (p_Level < 0)
                {
                    int l_PlayerLevel = l_Player.GetPlayerLevel();
                    int l_LevelTemp = int.MaxValue;
                    foreach (var l_ID in LevelController.GetLevelControllerCache().LevelID)
                    {
                        if (l_ID > l_PlayerLevel && l_ID <= l_LevelTemp)
                        {
                            l_LevelTemp = l_ID;
                        }
                    }

                    p_Level = l_LevelTemp;
                    l_CheckForLastGGP = true;
                }
            }
            catch
            {
                // ignored
            }

            bool l_IDExist = false;
            foreach (var l_ID in LevelController.GetLevelControllerCache().LevelID)
            {
                if (l_ID == p_Level)
                    l_IDExist = true;
            }

            if (l_IDExist)
            {
                Level l_Level = new Level(p_Level);
                try
                {
                    bool l_LevelIsPassed = false;
                    List<InPlayerPassFormat> l_ListPassedMap = new List<InPlayerPassFormat>();
                    List<InPlayerPassFormat> l_NotPassedMapList = new List<InPlayerPassFormat>();
                    int l_NumberOfPass = 0;
                    bool l_IsPassed = false;
                    PlayerPassFormat l_PlayerPasses = l_Player.GetPass();
                    l_Player.LoadStats();
                    if (l_Level.m_Level.songs.Count > 0)
                    {
                        foreach (var l_Song in l_Level.m_Level.songs)
                        {
                            l_IsPassed = false;
                            if (l_PlayerPasses != null)
                                foreach (var l_PlayerPass in l_PlayerPasses.PassList)
                                {
                                    if (l_Song.hash == l_PlayerPass.Song.hash)
                                    {
                                        foreach (var l_SongDifficulty in l_Song.difficulties)
                                        {
                                            foreach (var l_PlayerPassDifficulty in l_PlayerPass.Song.difficulties)
                                            {
                                                if (l_SongDifficulty.characteristic == l_PlayerPassDifficulty.characteristic && l_SongDifficulty.name == l_PlayerPassDifficulty.name)
                                                {
                                                    l_LevelIsPassed = true;
                                                    l_IsPassed = true;
                                                    Console.WriteLine($"Pass detected on {l_Song.name} {l_SongDifficulty.name}");
                                                    l_NumberOfPass++;
                                                    l_ListPassedMap.Add(new InPlayerPassFormat
                                                    {
                                                        Song = l_Song,
                                                        Score = l_PlayerPass.Score,
                                                        LeaderboardID = l_PlayerPass.LeaderboardID
                                                    });
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }

                            if (!l_IsPassed)
                            {
                                l_NotPassedMapList.Add(new InPlayerPassFormat
                                {
                                    Song = l_Song,
                                    Score = 0,
                                    LeaderboardID = 0
                                });
                            }
                        }
                    }

                    int l_NumberOfDifficulties = 0;
                    foreach (var l_Song in l_Level.m_Level.songs)
                    {
                        foreach (var l_Difficulty in l_Song.difficulties)
                        {
                            l_NumberOfDifficulties++;
                        }
                    }

                    string l_PlayerTrophy = "";

                    while (l_Player.m_PlayerStats.Trophy.Count <= p_Level - 1)
                    {
                        l_Player.m_PlayerStats.Trophy.Add(new Trophy
                        {
                            Plastic = 0,
                            Silver = 0,
                            Gold = 0,
                            Diamond = 0
                        });
                    }

                    // ReSharper disable once IntDivisionByZero
                    if (l_NumberOfDifficulties != 0)
                    {
                        switch (l_NumberOfPass * 100 / l_NumberOfDifficulties)
                        {
                            case 0:
                            {
                                l_PlayerTrophy = "";
                                break;
                            }
                            case <= 39:
                            {
                                l_Player.m_PlayerStats.Trophy[p_Level - 1].Plastic = 1;
                                l_PlayerTrophy = "<:plastic:874215132874571787>";
                                break;
                            }
                            case <= 69:
                            {
                                l_Player.m_PlayerStats.Trophy[p_Level - 1].Silver = 1;
                                l_PlayerTrophy = "<:silver:874215133197500446>";
                                break;
                            }
                            case <= 99:
                            {
                                l_Player.m_PlayerStats.Trophy[p_Level - 1].Gold = 1;
                                l_PlayerTrophy = "<:gold:874215133147197460>";
                                break;
                            }

                            case 100:
                            {
                                l_Player.m_PlayerStats.Trophy[p_Level - 1].Diamond = 1;
                                l_PlayerTrophy = "<:diamond:874215133289795584>";
                                break;
                            }
                        }
                    }

                    l_Player.ReWriteStats();

                    if (l_ListPassedMap.Count > 0)
                    {
                        EmbedBuilder l_EmbedBuilder = new EmbedBuilder();
                        l_EmbedBuilder.WithTitle($"Passed maps in level {p_Level} {l_PlayerTrophy}");
                        l_EmbedBuilder.WithColor(new Color(0, 255, 0));
                        string l_BigGgp = null;
                        int l_Y = 0;
                        int l_NumbedOfEmbed = 1;

                        if (ConfigController.GetConfig().BigGGP) l_BigGgp = "\n\u200B";

                        foreach (var l_PassedMap in l_ListPassedMap)
                        {
                            foreach (var l_PassedDiff in l_PassedMap.Song.difficulties)
                            {
                                var l_EmbedValue = $"{l_PassedDiff.name} - {l_PassedDiff.characteristic}{l_BigGgp}";
                                if (l_PassedDiff.customData.minScoreRequirement != 0)
                                    l_EmbedValue += $" - MinScore: {l_PassedDiff.customData.minScoreRequirement} ({Math.Round((float)l_PassedDiff.customData.minScoreRequirement / l_PassedDiff.customData.maxScore * 100f * 100f) / 100f}%)";

                                l_EmbedBuilder.AddField($"{l_PassedMap.Song.name} ({Math.Round(l_PassedMap.Score / l_PassedDiff.customData.maxScore * 100f * 100f) / 100f}%)", l_EmbedValue, true);

                                if (l_NumbedOfEmbed % 2 != 0)
                                {
                                    if (l_Y % 2 == 0)
                                        l_EmbedBuilder.AddField("\u200B", "\u200B", true);
                                }
                                else if (l_Y % 2 != 0)
                                    l_EmbedBuilder.AddField("\u200B", "\u200B", true);

                                l_Y++;
                                if (l_Y % 15 == 0)
                                {
                                    await Context.Channel.SendMessageAsync("", false, l_EmbedBuilder.Build());
                                    l_EmbedBuilder = new EmbedBuilder();
                                    l_NumbedOfEmbed++;
                                }
                            }
                        }

                        l_EmbedBuilder.WithFooter(
                            $"To get the playlist file: use {BotHandler.m_Prefix}getplaylist {p_Level}");
                        await Context.Channel.SendMessageAsync("", false, l_EmbedBuilder.Build());

                        l_Player.SetGrindInfo(p_Level, l_LevelIsPassed, -1, l_Player.m_PlayerStats.Trophy[p_Level - 1], -1, -1);
                    }

                    if (l_NotPassedMapList.Count > 0)
                    {
                        EmbedBuilder l_EmbedBuilder = new EmbedBuilder();
                        l_EmbedBuilder.WithTitle($"Not Passed maps in level {p_Level}");
                        l_EmbedBuilder.WithColor(new Color(255, 0, 0));
                        string l_BigGgp = null;
                        int l_Y = 0;
                        int l_NumbedOfEmbed = 1;

                        if (ConfigController.GetConfig().BigGGP) l_BigGgp = "\n\u200B";

                        foreach (var l_NotPassedMap in l_NotPassedMapList)
                        {
                            foreach (var l_NotPassedDiff in l_NotPassedMap.Song.difficulties)
                            {
                                var l_EmbedValue = $"{l_NotPassedDiff.name} - {l_NotPassedDiff.characteristic}{l_BigGgp}";
                                if (l_NotPassedDiff.customData.minScoreRequirement != 0)
                                    l_EmbedValue += $" - MinScore: {l_NotPassedDiff.customData.minScoreRequirement} ({Math.Round((float)l_NotPassedDiff.customData.minScoreRequirement / l_NotPassedDiff.customData.maxScore * 100f * 100f) / 100f}%)";

                                l_EmbedBuilder.AddField($"{l_NotPassedMap.Song.name}", l_EmbedValue, true);

                                if (l_NumbedOfEmbed % 2 != 0)
                                {
                                    if (l_Y % 2 == 0)
                                        l_EmbedBuilder.AddField("\u200B", "\u200B", true);
                                }
                                else if (l_Y % 2 != 0)
                                    l_EmbedBuilder.AddField("\u200B", "\u200B", true);

                                l_Y++;
                                if (l_Y % 15 == 0)
                                {
                                    await Context.Channel.SendMessageAsync("", false, l_EmbedBuilder.Build());
                                    l_EmbedBuilder = new EmbedBuilder();
                                    l_NumbedOfEmbed++;
                                }
                            }
                        }

                        l_EmbedBuilder.WithFooter(
                            $"To get the playlist file: use {BotHandler.m_Prefix}getplaylist {p_Level}");
                        await Context.Channel.SendMessageAsync("", false, l_EmbedBuilder.Build());
                    }
                }
                catch (Exception l_Exception)
                {
                    await ReplyAsync($"> :x: Error occured : {l_Exception.Message}");
                }
            }
            else if (l_CheckForLastGGP)
            {
                await ReplyAsync(
                    "> :white_check_mark: Seems like there isn't any new level to grind for you right now, good job.");
            }
            else
            {
                await ReplyAsync("> :x: This level does not exist.");
            }
        }
    }
}