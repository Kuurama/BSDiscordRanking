using System.Linq;
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using BSDiscordRanking.Formats.API;
using BSDiscordRanking.Formats.Controller;
using Discord;
using Discord.Commands;

namespace BSDiscordRanking.Discord.Modules.ScoringTeamModule
{
    [PermissionHandler.RequirePermissionAttribute(PERMISSION)]
    public partial class ScoringTeamModule : ModuleBase<SocketCommandContext>
    {
        [Command("downloadscore")]
        [Alias("scoredownload", "redownloadscore")]
        [Summary("Downloads a player's score from a map leaderboard, adding it to their Stored Scores, passes, and the map stored leaderboard (trigger the weight recalculation).")]
        public async Task DownloadScore(string p_DiscordOrScoreSaberID = null, string p_LeaderboardID = null)
        {
            if (string.IsNullOrEmpty(p_DiscordOrScoreSaberID) || string.IsNullOrEmpty(p_LeaderboardID))
            {
                await ReplyAsync($"> :x: Seems like you didn't used the command correctly, use: `{BotHandler.m_Prefix}downloadscore [DiscordOrScoreSaberID] [LeaderboardID]`");
                return;
            }

            if (int.TryParse(p_LeaderboardID, out int l_LeaderboardID))
            {
                ConfigFormat l_Config = ConfigController.GetConfig();
                bool l_ScoreRedownloaded = false;
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
                    EmbedBuilder l_EmbedBuilder = new EmbedBuilder().WithColor(Color.Green);
                    Player l_Player = new Player(p_DiscordOrScoreSaberID);
                    l_Player.LoadPass();

                    ApiScoreCollection l_ApiScoreCollection;
                    ApiScore l_DownloadedPlayerScore = null;
                    ApiLeaderboardInfo l_ApiLeaderboardInfo = null;
                    l_ApiLeaderboardInfo = MapLeaderboardController.GetInfos(l_LeaderboardID);
                    if (l_ApiLeaderboardInfo == null)
                    {
                        l_EmbedBuilder.WithTitle("Sorry but it seems like this LeaderboardID is invalid.");
                        l_EmbedBuilder.WithUrl($"https://scoresaber.com/leaderboard/{l_LeaderboardID}");
                        l_EmbedBuilder.WithColor(Color.Red);
                        await ReplyAsync("", embed: l_EmbedBuilder.Build());
                        return;
                    }

                    int l_Page = 1;
                    do
                    {
                        l_ApiScoreCollection = MapLeaderboardController.GetLeaderboardScores(l_LeaderboardID, l_Page);
                        if (l_ApiScoreCollection != null)
                        {
                            if (!l_ApiScoreCollection.scores.Any())
                            {
                                l_ApiScoreCollection = null;
                                break;
                            }

                            foreach (ApiScore l_Score in l_ApiScoreCollection.scores.Where(p_X => p_X.leaderboardPlayerInfo.id == p_DiscordOrScoreSaberID))
                            {
                                l_DownloadedPlayerScore = l_Score;
                                break;
                            }
                        }

                        l_Page++;
                    } while (l_ApiScoreCollection?.scores != null && l_DownloadedPlayerScore == null);

                    if (l_DownloadedPlayerScore == null)
                    {
                        l_EmbedBuilder.WithTitle($"Sorry but {l_Player.m_PlayerFull.name} don't have any Score on this leaderboard.");
                        l_EmbedBuilder.WithUrl($"https://scoresaber.com/leaderboard/{l_LeaderboardID}");
                        l_EmbedBuilder.WithColor(Color.Red);
                        await ReplyAsync("", embed: l_EmbedBuilder.Build());
                        return;
                    }


                    if (l_Player.m_PlayerScoreCollection != null)
                    {
                        int l_ScoreIndex = l_Player.m_PlayerScoreCollection.playerScores.FindIndex(p_X => p_X.leaderboard.id == l_LeaderboardID);
                        if (l_ScoreIndex >= 0)
                        {
                            l_EmbedBuilder.AddField($"{l_Player.m_PlayerFull.name}'s score", $"(#{l_Player.m_PlayerScoreCollection.playerScores[l_ScoreIndex].score.rank}) - {l_Player.m_PlayerScoreCollection.playerScores[l_ScoreIndex].score.baseScore} => (#{l_DownloadedPlayerScore.rank}) - {l_DownloadedPlayerScore.baseScore}.");
                            l_Player.m_PlayerScoreCollection.playerScores[l_ScoreIndex].score = l_DownloadedPlayerScore;
                            l_Player.m_PlayerScoreCollection.playerScores[l_ScoreIndex].leaderboard = l_ApiLeaderboardInfo;
                            l_ScoreRedownloaded = true;
                            l_Player.ReWriteScore();
                        }
                        else
                        {
                            l_EmbedBuilder.AddField($"{l_Player.m_PlayerFull.name}'s score", $"(#{l_DownloadedPlayerScore.rank}) - {l_DownloadedPlayerScore.baseScore}.");
                            l_Player.m_PlayerScoreCollection.playerScores.Insert(0, new ApiPlayerScore { leaderboard = l_ApiLeaderboardInfo, score = l_DownloadedPlayerScore });
                            l_Player.ReWriteScore();
                        }
                    }

                    if (l_ScoreRedownloaded)
                    {
                        l_EmbedBuilder.WithTitle("The score have been ReDownloaded and Replaced.");
                        l_EmbedBuilder.WithUrl($"https://scoresaber.com/leaderboard/{l_LeaderboardID}");
                    }
                    else
                    {
                        l_EmbedBuilder.WithTitle("The score have been Downloaded and added.");
                        l_EmbedBuilder.WithUrl($"https://scoresaber.com/leaderboard/{l_LeaderboardID}");
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