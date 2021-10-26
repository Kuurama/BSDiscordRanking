using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using Discord;
using Discord.Commands;

namespace BSDiscordRanking.Discord.Modules.UserModule
{
    [CheckChannel]
    public partial class UserModule : ModuleBase<SocketCommandContext>
    {
        [Command("leaderboard")]
        [Alias("ld", "lb", "leaderboards")]
        [Summary("Shows the leaderboard.")]
        public async Task Leaderboard(int p_Page = default)
        {
            bool l_PageExist = false;
            EmbedBuilder l_EmbedBuilder = new EmbedBuilder();
            LeaderboardController l_LeaderboardController = new LeaderboardController();
            if (p_Page == default)
            {
                try
                {
                    p_Page = l_LeaderboardController.m_Leaderboard.Leaderboard.FindIndex(x =>
                        x.ScoreSaberID == UserController.GetPlayer(Context.User.Id.ToString())) / 10 + 1;
                }
                catch
                {
                    p_Page = 1;
                }
            }

            for (var l_Index = (p_Page - 1) * 10; l_Index < (p_Page - 1) * 10 + 10; l_Index++)
            {
                try
                {
                    var l_RankedPlayer = l_LeaderboardController.m_Leaderboard.Leaderboard[l_Index];
                    l_EmbedBuilder.AddField(
                        $"#{l_Index + 1} - {l_RankedPlayer.Name} : {l_RankedPlayer.Points} {ConfigController.GetConfig().PointsName}",
                        $"Level: {l_RankedPlayer.Level}. [ScoreSaber Profile](https://scoresaber.com/u/{l_RankedPlayer.ScoreSaberID})");
                    l_PageExist = true;
                }
                catch
                {
                    // ignored
                }
            }

            if (l_PageExist)
            {
                l_EmbedBuilder.WithTitle("Leaderboard:");
                await Context.Channel.SendMessageAsync("", false, l_EmbedBuilder.Build());
            }
            else
                await ReplyAsync("> :x: Sorry, this page doesn't exist");
        }
    }
}