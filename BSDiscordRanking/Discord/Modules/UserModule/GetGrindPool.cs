using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
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
        public async Task GetGrindPool(int p_Level = -1, string p_Embed1Or0 = null)
        {
            bool l_CheckForLastGGP = false;
            bool l_FullEmbeddedGGP = false;
            float l_EarnedPoints = 0f;
            float l_MaximumPoints = 0f;
            if (Int32.TryParse(p_Embed1Or0, out int l_EmbedIntValue))
            {
                l_FullEmbeddedGGP = l_EmbedIntValue > 0;
            }
            else
                l_FullEmbeddedGGP = ConfigController.m_ConfigFormat.FullEmbeddedGGP;

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
                    PlayerPassFormat l_PlayerPassFormat = new PlayerPassFormat()
                    {
                        SongList = new List<InPlayerSong>()
                    };
                    int l_NumberOfPass = 0;
                    bool l_AlreadyHaveThumbnail = UserController.GetPlayer(Context.User.Id.ToString()) == null;

                    PlayerPassFormat l_PlayerPasses = l_Player.GetPass();
                    l_Player.LoadStats();
                    if (l_Level.m_Level.songs.Count > 0)
                    {
                        foreach (var l_Song in l_Level.m_Level.songs.Select((p_Value, p_Index) => new { value = p_Value, index = p_Index }))
                        {
                            l_PlayerPassFormat.SongList.Add(new InPlayerSong()
                            {
                                hash = l_Song.value.hash,
                                key = l_Song.value.key,
                                name = l_Song.value.name,
                                DiffList = new List<InPlayerPassFormat>()
                            });

                            foreach (var l_SongDifficulty in l_Song.value.difficulties)
                            {
                                l_PlayerPassFormat.SongList[l_Song.index].DiffList.Add(
                                    new InPlayerPassFormat()
                                    {
                                        Difficulty = new Difficulty
                                        {
                                            name = l_SongDifficulty.name,
                                            characteristic = l_SongDifficulty.characteristic,
                                            customData = l_SongDifficulty.customData
                                        },
                                        LeaderboardID = 0,
                                        Score = 0,
                                        Rank = 0
                                    });
                                l_MaximumPoints += l_Level.m_Level.customData.weighting * 0.375f;
                            }

                            if (l_PlayerPasses != null)
                                foreach (var l_PlayerPass in l_PlayerPasses.SongList)
                                {
                                    if (l_Song.value.hash == l_PlayerPass.hash)
                                    {
                                        foreach (var l_SongDifficulty in l_Song.value.difficulties)
                                        {
                                            foreach (var l_PlayerPassDifficulty in l_PlayerPass.DiffList)
                                            {
                                                if (l_SongDifficulty.characteristic == l_PlayerPassDifficulty.Difficulty.characteristic && l_SongDifficulty.name == l_PlayerPassDifficulty.Difficulty.name)
                                                {
                                                    l_LevelIsPassed = true;

                                                    Console.WriteLine($"Pass detected on {l_Song.value.name} {l_SongDifficulty.name} - {l_SongDifficulty.characteristic}");
                                                    l_NumberOfPass++;

                                                    int l_DiffIndex = l_PlayerPassFormat.SongList[l_Song.index].DiffList.FindIndex(p_X => p_X.Difficulty.characteristic == l_SongDifficulty.characteristic && p_X.Difficulty.name == l_SongDifficulty.name);
                                                    if (l_DiffIndex >= 0)
                                                    {
                                                        l_PlayerPassFormat.SongList[l_Song.index].DiffList[l_DiffIndex].Difficulty.customData = l_PlayerPassDifficulty.Difficulty.customData;
                                                        l_PlayerPassFormat.SongList[l_Song.index].DiffList[l_DiffIndex].LeaderboardID = l_PlayerPassDifficulty.LeaderboardID;
                                                        l_PlayerPassFormat.SongList[l_Song.index].DiffList[l_DiffIndex].Score = l_PlayerPassDifficulty.Score;
                                                        l_PlayerPassFormat.SongList[l_Song.index].DiffList[l_DiffIndex].Rank = l_PlayerPassDifficulty.Rank;
                                                        l_EarnedPoints += l_Level.m_Level.customData.weighting * 0.375f;
                                                    }

                                                    break;
                                                }
                                            }
                                        }
                                    }
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
                    if (l_NumberOfPass > 0)
                    {
                        EmbedBuilder l_EmbedBuilder = new EmbedBuilder();
                        l_EmbedBuilder.WithTitle($"Passed maps in level {p_Level} ({ConfigController.GetConfig().PointsName} earned: {l_EarnedPoints}/{l_MaximumPoints}) {l_PlayerTrophy}");
                        l_EmbedBuilder.WithColor(new Color(0, 255, 0));
                        if (!l_AlreadyHaveThumbnail)
                        {
                            l_EmbedBuilder.WithThumbnailUrl("https://new.scoresaber.com" + l_Player.m_PlayerFull.playerInfo.avatar);
                            l_AlreadyHaveThumbnail = true;
                        }

                        if (l_NumberOfDifficulties - l_NumberOfPass <= 0)
                            l_EmbedBuilder.WithFooter($"To get the playlist file: use {BotHandler.m_Prefix}getplaylist {p_Level}");

                        GGPFormat l_GGP = await BuildGGP(l_PlayerPassFormat, l_EmbedBuilder, l_FullEmbeddedGGP, true);

                        if (l_GGP.EmbedBuilder != null)
                        {
                            await Context.Channel.SendMessageAsync("", false, l_GGP.EmbedBuilder.Build());
                        }

                        l_Player.SetGrindInfo(p_Level, l_LevelIsPassed, -1, l_Player.m_PlayerStats.Trophy[p_Level - 1], -1, -1);
                    }

                    List<string> l_Messages = new List<string> { $"" }; /// Reset the Message between Passed and Unpassed maps

                    if (l_NumberOfDifficulties - l_NumberOfPass > 0)
                    {
                        EmbedBuilder l_EmbedBuilder = new EmbedBuilder();
                        l_EmbedBuilder.WithTitle($"Unpassed maps in level {p_Level}");
                        l_EmbedBuilder.WithColor(new Color(255, 0, 0));
                        if (!l_AlreadyHaveThumbnail)
                        {
                            l_EmbedBuilder.WithThumbnailUrl("https://new.scoresaber.com" + l_Player.m_PlayerFull.playerInfo.avatar);
                            l_AlreadyHaveThumbnail = true;
                        }

                        if (l_NumberOfPass >= 0)
                            l_EmbedBuilder.WithFooter($"To get the playlist file: use {BotHandler.m_Prefix}getplaylist {p_Level} (or {BotHandler.m_Prefix}getplaylist all) to get all of them.");

                        GGPFormat l_GGP = await BuildGGP(l_PlayerPassFormat, l_EmbedBuilder, l_FullEmbeddedGGP, false);

                        if (l_GGP.EmbedBuilder != null)
                        {
                            await Context.Channel.SendMessageAsync("", false, l_GGP.EmbedBuilder.Build());
                        }
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

        private async Task<GGPFormat> BuildGGP(PlayerPassFormat p_PlayerPassFormat, EmbedBuilder p_EmbedBuilder, bool p_FullEmbeddedGGP, bool p_OnlySendPasses)
        {
            int l_Y = 0;
            int l_NumbedOfEmbed = 1;
            string l_LastMessage = null;
            int l_MessagesIndex = 0;
            List<string> l_Messages = new List<string> { "" };
            int l_MessageTotalLength = 0;
            foreach (var l_Map in p_PlayerPassFormat.SongList)
            {
                foreach (var l_Diff in l_Map.DiffList)
                {
                    if (p_OnlySendPasses) /// Sort passed maps from unpassed maps with score.
                    {
                        if (l_Diff.Score <= 0)
                            continue;
                    }
                    else
                    {
                        if (l_Diff.Score > 0)
                            continue;
                    }

                    string l_ScoreOnMap = (l_Diff.Score != 0 ? $"- {Math.Round(l_Diff.Score / l_Diff.Difficulty.customData.maxScore * 100f * 100f) / 100f}%" : null);
                    string l_RankOnMap = (l_Diff.Rank != 0 ? $"(#{l_Diff.Rank})" : null);
                    string l_CustomText = (l_Diff.Difficulty.customData.infoOnGGP != null ? $"- {l_Diff.Difficulty.customData.infoOnGGP.Replace("_", " ")}" : "");
                    if (p_FullEmbeddedGGP)
                    {
                        var l_EmbedValue = $"{l_Diff.Difficulty.name} - {l_Diff.Difficulty.characteristic}";
                        if (l_Diff.Difficulty.customData.minScoreRequirement != 0)
                            l_EmbedValue += $" - MinScore: {l_Diff.Difficulty.customData.minScoreRequirement} ({Math.Round((float)l_Diff.Difficulty.customData.minScoreRequirement / l_Diff.Difficulty.customData.maxScore * 100f * 100f) / 100f}%) {l_CustomText}";
                        else
                            l_EmbedValue += l_CustomText;

                        p_EmbedBuilder.AddField($"{l_Map.name} {l_ScoreOnMap} {l_RankOnMap}", l_EmbedValue, true);

                        if (l_NumbedOfEmbed % 2 != 0)
                        {
                            if (l_Y % 2 == 0)
                                p_EmbedBuilder.AddField("\u200B", "\u200B", true);
                        }
                        else if (l_Y % 2 != 0)
                            p_EmbedBuilder.AddField("\u200B", "\u200B", true);

                        l_Y++;
                        if (l_Y % 15 == 0)
                        {
                            await Context.Channel.SendMessageAsync("", false, p_EmbedBuilder.Build());
                            p_EmbedBuilder = new EmbedBuilder();

                            p_EmbedBuilder.WithColor(l_Diff.Score != 0 ? new Color(0, 255, 0) : new Color(255, 0, 0));
                            l_NumbedOfEmbed++;
                        }
                    }
                    else
                    {
                        if (l_Messages[l_MessagesIndex].Length + $"{l_Map.name} - {l_Diff.Difficulty.name} - {l_Diff.Difficulty.characteristic}  - MinScore: {l_Diff.Difficulty.customData.minScoreRequirement} ({Math.Round((float)l_Diff.Difficulty.customData.minScoreRequirement / l_Diff.Difficulty.customData.maxScore * 100f * 100f) / 100f}%) {l_CustomText} {l_ScoreOnMap} {l_RankOnMap}\n"
                            .Length > 1000)
                        {
                            l_MessageTotalLength += l_Messages[l_MessagesIndex].Length;
                            l_MessagesIndex++;
                        }

                        if (l_Messages.Count < l_MessagesIndex + 1 && l_MessageTotalLength <= 5600)
                        {
                            l_Messages.Add(""); /// Initialize the next used index.
                            p_EmbedBuilder.AddField("\u200B", l_Messages[l_MessagesIndex - 1]);
                        }
                        else if (l_MessageTotalLength > 3900) /// Description/Field lenght limit
                        {
                            l_Messages.Add(""); /// Initialize the next used index.
                            p_EmbedBuilder.AddField("\u200B", l_Messages[l_MessagesIndex - 1]);
                            var l_Embed = p_EmbedBuilder.Build();
                            await Context.Channel.SendMessageAsync(null, embed: l_Embed).ConfigureAwait(false);
                            p_EmbedBuilder = new EmbedBuilder();
                            p_EmbedBuilder.WithColor(l_Diff.Score != 0 ? new Color(0, 255, 0) : new Color(255, 0, 0));
                            l_MessageTotalLength = 0;
                        }

                        l_Messages[l_MessagesIndex] += $"***`{l_Map.name.Replace("`", @"\`").Replace("*", @"\*")}`***";

                        if (l_Diff.Difficulty.characteristic == "Standard")
                            l_Messages[l_MessagesIndex] += $" *`({l_Diff.Difficulty.name})`*";
                        else
                            l_Messages[l_MessagesIndex] += $" *`({l_Diff.Difficulty.name} - {l_Diff.Difficulty.characteristic})`*";

                        if (l_Diff.Difficulty.customData.minScoreRequirement != 0)

                            l_Messages[l_MessagesIndex] += $" *`- MinScore: {l_Diff.Difficulty.customData.minScoreRequirement} ({Math.Round((float)l_Diff.Difficulty.customData.minScoreRequirement / l_Diff.Difficulty.customData.maxScore * 100f * 100f) / 100f}%)`*";


                        if (l_CustomText != "")
                            l_Messages[l_MessagesIndex] += $" *`{l_CustomText}`*";

                        if (l_ScoreOnMap != null)
                        {
                            l_Messages[l_MessagesIndex] += $" *`{l_ScoreOnMap}`*";
                        }

                        if (l_RankOnMap != null)
                        {
                            l_Messages[l_MessagesIndex] += $" *`{l_RankOnMap}`*";
                        }

                        l_Messages[l_MessagesIndex] += "\n";

                        l_LastMessage = l_Messages[l_MessagesIndex];
                    }
                }
            }

            if (!p_FullEmbeddedGGP && l_LastMessage != "")
            {
                p_EmbedBuilder.AddField("\u200B", l_LastMessage);
            }

            if (p_EmbedBuilder.Fields.Count <= 0)
            {
                p_EmbedBuilder = null;
            }

            return new GGPFormat
            {
                Messages = l_Messages,
                EmbedBuilder = p_EmbedBuilder
            };
        }
    }


    public class GGPFormat
    {
        public List<string> Messages { get; set; }
        public EmbedBuilder EmbedBuilder { get; set; }
    }
}