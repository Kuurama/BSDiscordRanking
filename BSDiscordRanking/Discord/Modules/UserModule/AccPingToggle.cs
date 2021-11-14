using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using Discord.Commands;

namespace BSDiscordRanking.Discord.Modules.UserModule
{
    [CheckChannel]
    public partial class UserModule : ModuleBase<SocketCommandContext>
    {
        [Command("accpingtoggle")]
        [Alias("acctoggleping","pingacctoggle","toogleaccping")]
        [Summary("Toggles personal ping for leaderboard snipe.")]
        public async Task AccPingToggle()
        {
            if (!UserController.UserExist(Context.User.Id.ToString()))
            {
                await ReplyAsync($"> :x: Sorry, you doesn't have any account linked. Please use `{BotHandler.m_Prefix}link <ScoreSaber link/id>` instead.\n> (Or to get started with the bot: use the `{BotHandler.m_Prefix}getstarted` command)");
            }
            else
            {
                AccLeaderboardController l_AccLeaderboardController = new AccLeaderboardController();
                int l_Index = l_AccLeaderboardController.m_Leaderboard.Leaderboard.FindIndex(p_X => p_X.ScoreSaberID == UserController.GetPlayer(Context.User.Id.ToString()));
                l_AccLeaderboardController.m_Leaderboard.Leaderboard[l_Index].IsPingAllowed = !l_AccLeaderboardController.m_Leaderboard.Leaderboard[l_Index].IsPingAllowed;
                l_AccLeaderboardController.ReWriteLeaderboard();
                await ReplyAsync($"> Your Acc Leaderboard Ping preference has been changed from **{!l_AccLeaderboardController.m_Leaderboard.Leaderboard[l_Index].IsPingAllowed}** to **{l_AccLeaderboardController.m_Leaderboard.Leaderboard[l_Index].IsPingAllowed}**");
            }
        }
    }
}