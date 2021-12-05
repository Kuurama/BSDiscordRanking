using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using BSDiscordRanking.Formats.Controller;
using Discord;
using Discord.Commands;

namespace BSDiscordRanking.Discord.Modules.UserModule
{
    public partial class UserModule : ModuleBase<SocketCommandContext>
    {
        [Command("ldacc")]
        [Alias("accld", "acclb", "lbacc", "accleaderboard", "accleaderboards")]
        [Summary("Shows the Acc based leaderboard.")]
        public async Task AccLeaderboard(int p_Page = default(int))
        {
            ConfigFormat l_ConfigFormat = ConfigController.GetConfig();
            EmbedBuilder l_EmbedBuilder = new EmbedBuilder();
            AccLeaderboardController l_AccLeaderboardController = new AccLeaderboardController();

            LeaderboardBuilderFormat l_AccLeaderboardBuilderFormat = BuildLeaderboard(l_AccLeaderboardController.m_Leaderboard, l_ConfigFormat.AccPointsName, l_EmbedBuilder, p_Page);

            if (l_AccLeaderboardBuilderFormat.PageExist)
            {
                l_EmbedBuilder.WithTitle($"{l_ConfigFormat.AccPointsName} Leaderboard:");
                await Context.Channel.SendMessageAsync("", false, l_EmbedBuilder.Build());
            }
            else
            {
                await ReplyAsync("> :x: Sorry, this page doesn't exist");
            }
        }

        [Command("ldpass")]
        [Alias("passld", "passlb", "lbpass", "passleaderboard", "passleaderboards")]
        [Summary("Shows the Pass based leaderboard.")]
        public async Task PassLeaderboard(int p_Page = default(int))
        {
            ConfigFormat l_ConfigFormat = ConfigController.GetConfig();
            EmbedBuilder l_EmbedBuilder = new EmbedBuilder();
            PassLeaderboardController l_PassLeaderboardController = new PassLeaderboardController();

            LeaderboardBuilderFormat l_PassLeaderboardBuilderFormat = BuildLeaderboard(l_PassLeaderboardController.m_Leaderboard, l_ConfigFormat.PassPointsName, l_EmbedBuilder, p_Page);

            if (l_PassLeaderboardBuilderFormat.PageExist)
            {
                l_EmbedBuilder.WithTitle($"{l_ConfigFormat.PassPointsName} Leaderboard:");
                await Context.Channel.SendMessageAsync("", false, l_EmbedBuilder.Build());
            }
            else
            {
                await ReplyAsync("> :x: Sorry, this page doesn't exist");
            }
        }

        private LeaderboardBuilderFormat BuildLeaderboard(LeaderboardControllerFormat p_LeaderboardController, string p_PointsName, EmbedBuilder p_EmbedBuilder, int p_Page)
        {
            if (p_Page == default(int))
                try
                {
                    p_Page = p_LeaderboardController.Leaderboard.FindIndex(p_X => p_X.ScoreSaberID == UserController.GetPlayer(Context.User.Id.ToString())) / 10 + 1;
                }
                catch
                {
                    p_Page = 1;
                }

            bool l_PageExist = false;
            for (int l_Index = (p_Page - 1) * 10; l_Index < (p_Page - 1) * 10 + 10; l_Index++)
                try
                {
                    if (p_LeaderboardController.Leaderboard.Count <= l_Index) continue;

                    RankedPlayer l_RankedPlayer = p_LeaderboardController.Leaderboard[l_Index];
                    p_EmbedBuilder.AddField(
                        $"#{l_Index + 1} - {l_RankedPlayer.Name} : {l_RankedPlayer.Points} {p_PointsName}",
                        $"Level: {l_RankedPlayer.Level}. [ScoreSaber Profile](https://scoresaber.com/u/{l_RankedPlayer.ScoreSaberID})");
                    l_PageExist = true;
                }
                catch
                {
                    // ignored
                }

            return new LeaderboardBuilderFormat
            {
                PageExist = l_PageExist,
                EmbedBuilder = p_EmbedBuilder
            };
        }

        public class LeaderboardBuilderFormat
        {
            public bool PageExist { get; set; }
            public EmbedBuilder EmbedBuilder { get; set; }
        }
    }
}