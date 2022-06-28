using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using Discord.Commands;

namespace BSDiscordRanking.Discord.Modules.UserModule
{
    public partial class UserModule : ModuleBase<SocketCommandContext>
    {
        [Command("passpingtoggle")]
        [Alias("passtoggleping", "pingpasstoggle", "tooglepassping")]
        [Summary("Toggles personal ping for leaderboard snipe.")]
        public async Task PassPingToggle()
        {
            if (!UserController.UserExist(Context.User.Id.ToString()))
            {
                await ReplyAsync($"> :x: Sorry, you don't have any account linked. Please use `{BotHandler.m_Prefix}link <ScoreSaber link/id>` instead.\n> (Or to get started with the bot: use the `{BotHandler.m_Prefix}getstarted` command)");
            }
            else
            {
                PassLeaderboardController l_PassLeaderboardController = new PassLeaderboardController();
                int l_Index = l_PassLeaderboardController.m_Leaderboard.Leaderboard.FindIndex(p_X => p_X.ScoreSaberID == UserController.GetPlayer(Context.User.Id.ToString()));
                l_PassLeaderboardController.m_Leaderboard.Leaderboard[l_Index].IsPingAllowed = !l_PassLeaderboardController.m_Leaderboard.Leaderboard[l_Index].IsPingAllowed;
                l_PassLeaderboardController.ReWriteLeaderboard();
                await ReplyAsync($"> Your Pass Leaderboard Ping preference has been changed from **{!l_PassLeaderboardController.m_Leaderboard.Leaderboard[l_Index].IsPingAllowed}** to **{l_PassLeaderboardController.m_Leaderboard.Leaderboard[l_Index].IsPingAllowed}**");
            }
        }
    }
}
