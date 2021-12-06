using System;
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using BSDiscordRanking.Formats.Controller;
using Discord;
using Discord.Commands;

namespace BSDiscordRanking.Discord.Modules.ScoringTeamModule
{
    [PermissionHandler.RequirePermissionAttribute(PERMISSION)]
    public partial class ScoringTeamModule : ModuleBase<SocketCommandContext>
    {
        [Command("removescore")]
        [Alias("deletescore")]
        [Summary("Removes a player's score from their stored scores, their passes, and the map stored leaderboard (trigger the weight recalculation).")]
        public async Task RemoveScore(string p_DiscordOrScoreSaberID = null, string p_LeaderboardID = null)
        {
            if (string.IsNullOrEmpty(p_DiscordOrScoreSaberID) || string.IsNullOrEmpty(p_LeaderboardID))
            {
                await ReplyAsync($"> :x: Seems like you didn't used the command correctly, use: `{BotHandler.m_Prefix}removescore [DiscordOrScoreSaberID] [LeaderboardID]`");
                return;
            }

            if (int.TryParse(p_LeaderboardID, out int l_LeaderboardID))
            {
                ConfigFormat l_Config = ConfigController.GetConfig();
                bool l_ScoreRemoved = false;
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
                    EmbedBuilder l_EmbedBuilder = new EmbedBuilder().WithColor(new Color(255, 0, 0));
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
                                l_EmbedBuilder.AddField($"{l_Player.m_PlayerFull.name}'s pass", $"(key-{l_Player.m_PlayerPass.SongList[l_SongIndex].key}) - {l_Player.m_PlayerPass.SongList[l_SongIndex].name}.");
                                if (l_Player.m_PlayerPass.SongList[l_SongIndex].DiffList.Count > 1)
                                    l_Player.m_PlayerPass.SongList[l_SongIndex].DiffList.RemoveAt(l_DiffIndex);
                                else
                                    l_Player.m_PlayerPass.SongList.RemoveAt(l_SongIndex);
                                l_ScoreRemoved = true;
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
                            l_EmbedBuilder.AddField($"{l_Player.m_PlayerFull.name}'s score", $"(#{l_Player.m_PlayerScore[l_ScoreIndex].score.rank}) - {l_Player.m_PlayerScore[l_ScoreIndex].score.baseScore}.");
                            l_Player.m_PlayerScore.RemoveAt(l_ScoreIndex);
                            l_ScoreRemoved = true;
                            l_Player.ReWriteScore();
                        }
                    }

                    MapLeaderboardController l_MapLeaderboardController = new MapLeaderboardController(l_LeaderboardID);
                    if (l_MapLeaderboardController.m_MapLeaderboard?.scores != null)
                    {
                        int l_MapLeaderboardIndex = l_MapLeaderboardController.m_MapLeaderboard.scores.FindIndex(p_X => p_X.score.leaderboardPlayerInfo.id == l_ScoreSaberID.ToString());
                        if (l_MapLeaderboardIndex >= 0)
                        {
                            l_MapLeaderboardController.m_MapLeaderboard.scores.RemoveAt(l_MapLeaderboardIndex);
                            l_ScoreRemoved = true;
                            l_MapLeaderboardController.m_MapLeaderboard.forceAutoWeightRecalculation = true;
                            l_MapLeaderboardController.ReWriteMapLeaderboard();
                            l_EmbedBuilder.AddField("Map Leaderboard", "AutoWeight will be recalculated on scan (if the person scanning have a pass on it).");
                        }
                    }

                    if (l_ScoreRemoved)
                        l_EmbedBuilder.WithTitle("The score have been removed from the following:");
                    else
                        l_EmbedBuilder.WithTitle($"Sorry but {l_Player.m_PlayerFull.name} don't have any Score or Pass matching this LevelID stored on the bot.");

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