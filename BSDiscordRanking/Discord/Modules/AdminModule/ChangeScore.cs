using System;
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using BSDiscordRanking.Formats.Controller;
using Discord;
using Discord.Commands;

namespace BSDiscordRanking.Discord.Modules.AdminModule
{
    [PermissionHandler.RequirePermissionAttribute(Permission)]
    public partial class AdminModule : ModuleBase<SocketCommandContext>
    {
        [Command("editscore")]
        [Alias("changescore")]
        [Summary("Edit/Change a player's score from their stored scores, their passes, and the map stored leaderboard (trigger the weight recalculation).")]
        public async Task EditScore(string p_DiscordOrScoreSaberID, string p_LeaderboardID, int p_Score)
        {
            if (int.TryParse(p_LeaderboardID, out int l_LeaderboardID))
            {
                ConfigFormat l_Config = ConfigController.GetConfig();
                bool l_ScoreChanged = false;
                bool l_IsScoreSaberAccount = UserController.AccountExist(p_DiscordOrScoreSaberID);

                if (UserController.UserExist(p_DiscordOrScoreSaberID))
                {
                    p_DiscordOrScoreSaberID = UserController.GetPlayer(p_DiscordOrScoreSaberID);
                }
                else if (!UserController.UserExist(p_DiscordOrScoreSaberID) && !l_IsScoreSaberAccount)
                {
                    await ReplyAsync("> :x: Sorry, this Discord User doesn't have any ScoreSaber account linked/isn't a correct ScoreSaberID.");
                    return;
                }

                if (ulong.TryParse(p_DiscordOrScoreSaberID, out ulong l_ScoreSaberID))
                {
                    EmbedBuilder l_EmbedBuilder = new EmbedBuilder().WithColor(Color.Purple);
                    Player l_Player = new Player(p_DiscordOrScoreSaberID);
                    l_Player.LoadPass();
                    if (l_Player.m_PlayerPass.SongList != null)
                    {
                        int l_SongIndex = l_Player.m_PlayerPass.SongList.FindIndex(p_X => p_X.DiffList.FindIndex(p_Y => p_Y.Difficulty.customData.leaderboardID == l_LeaderboardID) >= 0);
                        if (l_SongIndex >= 0)
                        {
                            int l_DiffIndex = l_Player.m_PlayerPass.SongList[l_SongIndex].DiffList.FindIndex(p_X => p_X.Difficulty.customData.leaderboardID == l_LeaderboardID);
                            if (l_DiffIndex >= 0)
                            {
                                l_EmbedBuilder.AddField($"{l_Player.m_PlayerFull.name}'s pass", $"(key-{l_Player.m_PlayerPass.SongList[l_SongIndex].key}) - {l_Player.m_PlayerPass.SongList[l_SongIndex].name}: {l_Player.m_PlayerPass.SongList[l_SongIndex].DiffList[l_DiffIndex].Score} => {p_Score}.");
                                if (l_Player.m_PlayerPass.SongList[l_SongIndex].DiffList.Count > 1)
                                {
                                    l_Player.m_PlayerPass.SongList[l_SongIndex].DiffList[l_DiffIndex].Score = p_Score;
                                    l_Player.m_PlayerPass.SongList[l_SongIndex].DiffList[l_DiffIndex].Rank = -1;
                                }

                                l_ScoreChanged = true;
                                l_Player.ReWritePass();
                            }
                            else
                            {
                                Console.WriteLine("Wait what the fuck?");
                            }
                        }
                    }

                    if (l_Player.m_PlayerScore != null)
                    {
                        int l_ScoreIndex = l_Player.m_PlayerScore.FindIndex(p_X => p_X.leaderboard.id == l_LeaderboardID);
                        if (l_ScoreIndex >= 0)
                        {
                            l_EmbedBuilder.AddField($"{l_Player.m_PlayerFull.name}'s score", $"(#{l_Player.m_PlayerScore[l_ScoreIndex].score.rank}) - {l_Player.m_PlayerScore[l_ScoreIndex].score.baseScore} => (#{-1}) - {p_Score}.");
                            l_Player.m_PlayerScore[l_ScoreIndex].score.baseScore = p_Score;
                            l_Player.m_PlayerScore[l_ScoreIndex].score.rank = -1;
                            l_ScoreChanged = true;
                            l_Player.ReWriteScore();
                        }
                    }

                    MapLeaderboardController l_MapLeaderboardController = new MapLeaderboardController(l_LeaderboardID);
                    if (l_MapLeaderboardController.m_MapLeaderboard?.scores != null)
                    {
                        int l_MapLeaderboardIndex = l_MapLeaderboardController.m_MapLeaderboard.scores.FindIndex(p_X => p_X.score.leaderboardPlayerInfo.id == l_ScoreSaberID.ToString());
                        if (l_MapLeaderboardIndex >= 0)
                        {
                            l_MapLeaderboardController.m_MapLeaderboard.scores[l_MapLeaderboardIndex].score.baseScore = p_Score;
                            l_MapLeaderboardController.m_MapLeaderboard.scores[l_MapLeaderboardIndex].score.rank = -1;
                            l_ScoreChanged = true;
                            l_MapLeaderboardController.m_MapLeaderboard.forceAutoWeightRecalculation = true;
                            l_MapLeaderboardController.ReWriteMapLeaderboard();
                            l_EmbedBuilder.AddField("Map Leaderboard", "AutoWeight will be recalculated on scan (if the person scanning have a pass on it).");
                        }
                    }

                    if (l_ScoreChanged)
                    {
                        l_EmbedBuilder.WithTitle("The score have been edited from the following:");
                        l_EmbedBuilder.WithUrl($"https://scoresaber.com/leaderboard/{l_LeaderboardID}");
                    }
                    else
                    {
                        l_EmbedBuilder.WithTitle($"Sorry but {l_Player.m_PlayerFull.name} don't have any Score or Pass matching this LevelID stored on the bot.");
                    }

                    await ReplyAsync("", embed: l_EmbedBuilder.Build());
                }
            }
            else
            {
                await ReplyAsync("> :x: Please enter a correct LeaderboardID.");
            }
        }
    }
}