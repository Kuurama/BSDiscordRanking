using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
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
            try
            {
                if (p_Level < 0)
                {
                    int l_PlayerLevel =
                        new Player(UserController.GetPlayer(Context.User.Id.ToString())).GetPlayerLevel();
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
                    List<bool> l_Passed = new List<bool>();
                    int l_NumberOfPass = 0;
                    Player l_Player = new Player(UserController.GetPlayer(Context.User.Id.ToString()));
                    PlayerPassFormat l_PlayerPasses = l_Player.GetPass();
                    l_Player.LoadStats();
                    int l_I = 0;
                    if (l_Level.m_Level.songs.Count > 0)
                    {
                        foreach (var l_Song in l_Level.m_Level.songs)
                        {
                            if (l_PlayerPasses != null)
                                foreach (var l_PlayerPass in l_PlayerPasses.songs)
                                {
                                    if (l_Song.hash == l_PlayerPass.hash)
                                    {
                                        foreach (var l_SongDifficulty in l_Song.difficulties)
                                        {
                                            foreach (var l_PlayerPassDifficulty in l_PlayerPass.difficulties)
                                            {
                                                if (l_SongDifficulty.characteristic ==
                                                    l_PlayerPassDifficulty.characteristic && l_SongDifficulty.name ==
                                                    l_PlayerPassDifficulty.name)
                                                {
                                                    Console.WriteLine(
                                                        $"Pass detected on {l_Song.name} {l_SongDifficulty.name}");
                                                    l_NumberOfPass++;
                                                    l_Passed.Insert(l_I, true);
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                            
                            for (int l_N = 0; l_N < l_Song.difficulties.Count; l_N++)
                            {
                                if (l_Passed.Count <= l_I)
                                {
                                    l_Passed.Insert(l_I, false);
                                }

                                l_I++;
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

                    EmbedBuilder l_EmbedBuilder = new EmbedBuilder();
                    l_EmbedBuilder.WithTitle($"Maps for Level {p_Level} {l_PlayerTrophy}");
                    string l_BigGgp = null;
                    int l_Y = 0;
                    int l_NumbedOfEmbed = 1;

                    if (ConfigController.GetConfig().BigGGP) l_BigGgp = "\n\u200B";
                    foreach (var l_Song in l_Level.m_Level.songs)
                    {
                        foreach (var l_SongDifficulty in l_Song.difficulties)
                        {
                            var l_EmbedValue = $"{l_SongDifficulty.name} - {l_SongDifficulty.characteristic}{l_BigGgp}";
                            if (l_SongDifficulty.customData.minScoreRequirement != 0)
                                l_EmbedValue += $" - MinScore: {l_SongDifficulty.customData.minScoreRequirement}";

                            if (!l_Passed[l_Y])
                                l_EmbedBuilder.AddField(l_Song.name, l_EmbedValue, true);
                            else
                                l_EmbedBuilder.AddField($"~~{l_Song.name}~~", $"~~{l_EmbedValue}~~", true);

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

                    l_Player.SetGrindInfo(p_Level, l_Passed, -1, l_Player.m_PlayerStats.Trophy[p_Level - 1], -1, -1);
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