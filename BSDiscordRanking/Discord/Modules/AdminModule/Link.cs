using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using Discord.Commands;

namespace BSDiscordRanking.Discord.Modules.AdminModule
{
    [PermissionHandler.RequirePermissionAttribute(PERMISSION)]
    public partial class AdminModule : ModuleBase<SocketCommandContext>
    {
        [Command("link")]
        [Summary("links a specific Score Saber account to a specific Discord's one.")]
        public async Task LinkUser(string p_ScoreSaberID = "", string p_DiscordID = "")
        {
            if (string.IsNullOrEmpty(p_DiscordID)) p_DiscordID = Context.User.Id.ToString();

            if (!string.IsNullOrEmpty(UserController.GetPlayer(p_DiscordID)))
            {
                await ReplyAsync(
                    $"> :x: Sorry, but this discord account has already been linked. Please use the admin `{BotHandler.m_Prefix}unlink {p_DiscordID}` command.");
            }
            else if (!string.IsNullOrEmpty(p_ScoreSaberID))
            {
                p_ScoreSaberID = Regex.Match(p_ScoreSaberID, @"\d+").Value;
                if (string.IsNullOrEmpty(UserController.GetPlayer(p_DiscordID)) && UserController.AccountExist(p_ScoreSaberID, out _) && !UserController.SSIsAlreadyLinked(p_ScoreSaberID))
                {
                    UserController.AddPlayer(p_DiscordID, p_ScoreSaberID);
                    await ReplyAsync(
                        $"> :white_check_mark: <@{p_DiscordID}> 's account has been successfully linked.\nLittle tip: use `{BotHandler.m_Prefix}scan` to scan your latest passes!");
                }
                else if (!UserController.AccountExist(p_ScoreSaberID, out _))
                {
                    await ReplyAsync("> :x: Sorry, but please enter a correct ScoreSaber Link/ID.");
                }
                else if (UserController.SSIsAlreadyLinked(p_ScoreSaberID))
                {
                    await ReplyAsync(
                        $"> :x: Sorry but this account is already linked to an other user.\nYou should investigate, then unlink the score saber ID using the admin `{BotHandler.m_Prefix}unlink {p_ScoreSaberID}` command.");
                }
                else
                {
                    await ReplyAsync("> :x: Oopsie, unhandled error.");
                }
            }
            else
            {
                await ReplyAsync("> :x: Please enter a ScoreSaber link/id.");
            }
        }
    }
}
