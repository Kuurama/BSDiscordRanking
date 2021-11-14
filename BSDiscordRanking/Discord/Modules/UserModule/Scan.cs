using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using BSDiscordRanking.Formats.Player;
using Discord;
using Discord.Commands;

namespace BSDiscordRanking.Discord.Modules.UserModule
{
    [CheckChannel]
    public partial class UserModule : ModuleBase<SocketCommandContext>
    {
        [Command("scan")]
        [Alias("dc")]
        [Summary("Scans all your scores & passes. Also update your rank.")]
        public async Task Scan_Scores()
        {
            Player l_Player = new Player(UserController.GetPlayer(Context.User.Id.ToString()));
            int l_OldPlayerLevel = l_Player.GetPlayerLevel(); /// By doing so, as a result => loadstats() inside too.
            if (!UserController.UserExist(Context.User.Id.ToString()))
                await ReplyAsync($"> :x: Sorry, you doesn't have any account linked. Please use `{BotHandler.m_Prefix}link <ScoreSaber link/id>` instead.\n> (Or to get started with the bot: use the `{BotHandler.m_Prefix}getstarted` command)");
            else
            {
                EmbedBuilder l_EmbedBuilder = new EmbedBuilder()
                    .WithTitle(l_Player.m_PlayerFull.playerInfo.playerName)
                    .WithUrl("https://scoresaber.com/u/" + l_Player.m_PlayerFull.playerInfo.playerId)
                    .WithThumbnailUrl("https://new.scoresaber.com" + l_Player.m_PlayerFull.playerInfo.avatar);

                bool l_FirsScan = l_Player.FetchScores(Context); /// FetchScore Return true if it's the first scan.
                var l_FetchPass = l_Player.FetchPass(Context);
                if (l_FetchPass.Result >= 1)
                {
                    l_EmbedBuilder.AddField("\u200B", $"> 🎉 Congratulations! <@{Context.User.Id.ToString()}>, You passed {l_FetchPass.Result} new maps!\n");
                    if (l_FirsScan)
                    {
                        l_EmbedBuilder.AddField("\u200B", $"To see your progress through the pools, *try the* `{ConfigController.GetConfig().CommandPrefix[0]}progress` *command.*\nAnd to check your profile, *use the* `{ConfigController.GetConfig().CommandPrefix[0]}profile` *command.*\n> You might also want to take the pools playlist:");
                    }
                }
                else
                {
                    if (l_FirsScan)
                        l_EmbedBuilder.WithDescription($"> Oh <@{Context.User.Id.ToString()}>, Seems like you didn't pass any maps from the pools.");
                    else
                        l_EmbedBuilder.WithDescription($"> :x: Sorry <@{Context.User.Id.ToString()}>, but you didn't pass any new maps.");
                }
                
                Color l_Color = GetRoleColor(RoleController.ReadRolesDB().Roles, Context.Guild.Roles, l_OldPlayerLevel);

                l_EmbedBuilder.WithColor(l_Color);
                await ReplyAsync(null, embed: l_EmbedBuilder.Build());
                
                if (l_FirsScan)
                {
                    await GetPlaylist("all");
                }
                
                l_EmbedBuilder = new EmbedBuilder();
                string l_Description = "";

                int l_NewPlayerLevel = l_Player.GetPlayerLevel();
                
                if (l_OldPlayerLevel != l_NewPlayerLevel)
                {
                    if (l_OldPlayerLevel < l_NewPlayerLevel)
                        l_Description += $"> <:Stonks:884058036371595294> GG! You are now Level {l_NewPlayerLevel}.\n> To see your new pool, try the `{ConfigController.GetConfig().CommandPrefix[0]}ggp` command.\n";
                    else
                        l_Description += $"> <:NotStonks:884057234886238208> You lost levels. You are now Level {l_NewPlayerLevel}\n";
                    
                    l_Description += "> :clock1: The bot will now update your roles. This step can take a while. `(The bot should now be responsive again)`";
                    l_EmbedBuilder.WithDescription(l_Description);
                    l_Color = GetRoleColor(RoleController.ReadRolesDB().Roles, Context.Guild.Roles, l_NewPlayerLevel);
                    l_EmbedBuilder.WithColor(l_Color); /// Changes the color if their level changed.
                    await ReplyAsync(null, embed: l_EmbedBuilder.Build());
                    var l_RoleUpdate = UserController.UpdateRoleAndSendMessage(Context, Context.User.Id, l_NewPlayerLevel);
                }

                Trophy l_TotalTrophy = new Trophy()
                {
                    Plastic = 0,
                    Silver = 0,
                    Gold = 0,
                    Diamond = 0
                };
                foreach (var l_Trophy in l_Player.m_PlayerStats.Trophy)
                {
                    l_TotalTrophy.Plastic += l_Trophy.Plastic;
                    l_TotalTrophy.Silver += l_Trophy.Silver;
                    l_TotalTrophy.Gold += l_Trophy.Gold;
                    l_TotalTrophy.Diamond += l_Trophy.Diamond;
                }

                /// This will Update the leaderboard (the ManagePlayer, then depending on the Player's decision, ping them for snipe///////////////////////////

                await PassLeaderboardController.SendSnipeMessage(Context, new PassLeaderboardController().ManagePlayer(l_Player.m_PlayerFull.playerInfo.playerName, l_Player.GetPlayerID(), l_Player.m_PlayerStats.PassPoints, l_NewPlayerLevel, l_TotalTrophy, false)); /// Manage the PassLeaderboard
                await AccLeaderboardController.SendSnipeMessage(Context, new AccLeaderboardController().ManagePlayer(l_Player.m_PlayerFull.playerInfo.playerName, l_Player.GetPlayerID(), l_Player.m_PlayerStats.AccPoints, l_NewPlayerLevel, l_TotalTrophy, false)); /// Manage the PassLeaderboard

                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            }
        }
    }
}