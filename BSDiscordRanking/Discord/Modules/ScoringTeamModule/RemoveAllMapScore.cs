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
        [Command("removeallmapscore")]
        [Alias("deleteallmapscore")]
        [Summary("Removes all player's scores from the maps leaderboards stored scores (trigger the weight recalculation).")]
        public async Task RemoveAllScore(string p_DiscordOrScoreSaberID = null)
        {
            if (string.IsNullOrEmpty(p_DiscordOrScoreSaberID))
            {
                await ReplyAsync($"> :x: Seems like you didn't used the command correctly, use: `{BotHandler.m_Prefix}removeallmapscore [DiscordOrScoreSaberID]`");
                return;
            }

            await ReplyAsync("> The task is being performed..");
            ConfigFormat l_Config = ConfigController.GetConfig();
            int l_ScoreRemoved = 0;
            bool l_IsScoreSaberAccount = UserController.AccountExist(p_DiscordOrScoreSaberID, out _);

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

                if (l_Player.m_PlayerScoreCollection != null)
                {
                    for (int l_Index = 0; l_Index < l_Player.m_PlayerScoreCollection.playerScores.Count; l_Index++)
                    {
                        if (l_Index % 300 == 0)
                        {
                            await ReplyAsync($"> {l_Index}/{l_Player.m_PlayerScoreCollection.playerScores.Count}..");
                        }
                        MapLeaderboardController l_MapLeaderboardController = new MapLeaderboardController(l_Player.m_PlayerScoreCollection.playerScores[l_Index].leaderboard.id, null, 0, true);
                        if (l_MapLeaderboardController.m_MapLeaderboard?.scores != null)
                        {
                            int l_MapLeaderboardIndex = l_MapLeaderboardController.m_MapLeaderboard.scores.FindIndex(p_X => p_X.score.leaderboardPlayerInfo.id == l_ScoreSaberID.ToString());
                            if (l_MapLeaderboardIndex >= 0)
                            {
                                l_MapLeaderboardController.m_MapLeaderboard.scores.RemoveAt(l_MapLeaderboardIndex);
                                l_ScoreRemoved++;
                                l_MapLeaderboardController.m_MapLeaderboard.forceAutoWeightRecalculation = true;
                                l_MapLeaderboardController.ReWriteMapLeaderboard();
                            }
                        }
                    }

                    l_Player.ReWriteScore();
                }

                if (l_ScoreRemoved != 0)
                    l_EmbedBuilder.WithTitle($"{l_ScoreRemoved} score(s) have been removed from the maps leaderboards.");
                else
                    l_EmbedBuilder.WithTitle($"Sorry but {l_Player.m_PlayerFull.name} don't have any Score stored on the maps leaderboards.");

                await ReplyAsync("", embed: l_EmbedBuilder.Build());
            }
        }
    }
}
