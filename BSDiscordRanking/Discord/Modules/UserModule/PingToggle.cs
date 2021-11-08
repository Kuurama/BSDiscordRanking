using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using Discord.Commands;

namespace BSDiscordRanking.Discord.Modules.UserModule
{
    [CheckChannel]
    public partial class UserModule : ModuleBase<SocketCommandContext>
    {
        [Command("pingtoggle")]
        [Alias("toggleping")]
        [Summary("Toggles personal ping for leaderboard snipe.")]
        public async Task PingToggle()
        {
            if (!UserController.UserExist(Context.User.Id.ToString()))
            {
                await ReplyAsync($"> :x: Sorry, you doesn't have any account linked. Please use `{BotHandler.m_Prefix}link <ScoreSaber link/id>` instead.\n> (Or to get started with the bot: use the `{BotHandler.m_Prefix}getstarted` command)");
            }
            else
            {
                LeaderboardController l_LeaderboardController = new LeaderboardController();
                int l_Index = l_LeaderboardController.m_Leaderboard.Leaderboard.FindIndex(p_X => p_X.ScoreSaberID == UserController.GetPlayer(Context.User.Id.ToString()));
                l_LeaderboardController.m_Leaderboard.Leaderboard[l_Index].IsPingAllowed = !l_LeaderboardController.m_Leaderboard.Leaderboard[l_Index].IsPingAllowed;
                l_LeaderboardController.ReWriteLeaderboard();
                await ReplyAsync($"> Your Ping preference has been changed from **{!l_LeaderboardController.m_Leaderboard.Leaderboard[l_Index].IsPingAllowed}** to **{l_LeaderboardController.m_Leaderboard.Leaderboard[l_Index].IsPingAllowed}**");
            }
        }
    }
}