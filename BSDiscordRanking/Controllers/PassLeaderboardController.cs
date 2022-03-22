using System.Threading.Tasks;
using BSDiscordRanking.Discord;
using BSDiscordRanking.Formats;
using Discord;
using Discord.Commands;

namespace BSDiscordRanking.Controllers
{
    public class PassLeaderboardController : PlayerLeaderboardController
    {
        private const string FILENAME = @"PassLeaderboard";
        private const string LEADERBOARD_TYPE = "pass";
        private static readonly string s_PointName = ConfigController.GetConfig().PassPointsName;

        public PassLeaderboardController()
        {
            m_Filename = FILENAME;
            m_LeaderboardType = LEADERBOARD_TYPE;
            m_PointName = s_PointName;
            LoadLeaderboard();
        }

        public static async Task SendSnipeMessage(SocketCommandContext p_Context, SnipeFormat p_Snipe)
        {
            if (p_Snipe.Player.OldRank == p_Snipe.Player.NewRank) /// Don't send message if player's rank didn't changed.
                return;

            Player l_Player = new Player(p_Snipe.Player.ScoreSaberID);
            bool l_SnipeExist = false;
            EmbedBuilder l_Builder = new EmbedBuilder()
                .WithAuthor(p_Author =>
                {
                    p_Author
                        .WithName(p_Snipe.Player.Name)
                        .WithUrl("https://scoresaber.com/u/" + l_Player.m_PlayerFull.id)
                        .WithIconUrl(l_Player.m_PlayerFull.profilePicture);
                })
                .AddField("\u200B", $"({s_PointName}Leaderboard) Your rank changed from **#{p_Snipe.Player.OldRank}** to **#{p_Snipe.Player.NewRank}**");

            l_Builder.WithDescription($"Your Current ping choice for {s_PointName} leaderboard snipe is **{p_Snipe.Player.IsPingAllowed}**, if you want to change it:\nType the `{BotHandler.m_Prefix}{LEADERBOARD_TYPE}pingtoggle` command");

            l_Builder.WithColor(p_Snipe.Player.OldRank < p_Snipe.Player.NewRank ? new Color(255, 0, 0) : new Color(0, 255, 0));

            Embed l_Embed = l_Builder.Build();
            await p_Context.Channel.SendMessageAsync(null, embed: l_Embed)
                .ConfigureAwait(false);

            bool l_EmbedDone = false;
            string l_MyText = "";
            if (p_Snipe.SnipedByPlayers.Count > 0)
            {
                l_MyText += $"> <:Stonks:884058036371595294> {p_Snipe.Player.Name} #{p_Snipe.Player.OldRank} -> #{p_Snipe.Player.NewRank}\n";
                foreach (Sniped l_SnipedPlayer in p_Snipe.SnipedByPlayers)
                    if (l_SnipedPlayer.IsPingAllowed && l_SnipedPlayer.OldRank != l_SnipedPlayer.NewRank)
                    {
                        l_SnipeExist = true;
                        if (!l_EmbedDone)
                        {
                            l_Builder = new EmbedBuilder()
                                .WithTitle($"({s_PointName} Leaderboard) Get sniped! <:Sniped:898696818093875230> (by {p_Snipe.Player.Name})")
                                .WithColor(new Color(255, 0, 0))
                                .WithFooter(p_Footer =>
                                {
                                    p_Footer
                                        .WithText($"To allow or disallow personal pings on the {s_PointName} Leaderboard, type the `{BotHandler.m_Prefix}{LEADERBOARD_TYPE}pingtoggle` command");
                                });
                            l_EmbedDone = true;
                        }

                        string l_PlayerText = l_SnipedPlayer.DiscordID != null ? $"<@{l_SnipedPlayer.DiscordID}>" : l_SnipedPlayer.Name;

                        l_MyText += $"> {l_PlayerText} #{l_SnipedPlayer.OldRank} -> #{l_SnipedPlayer.NewRank}\n";
                    }
            }

            if (l_SnipeExist)
            {
                l_Embed = l_Builder.Build();
                await p_Context.Channel.SendMessageAsync(l_MyText, embed: l_Embed)
                    .ConfigureAwait(false);
            }
        }
    }
}