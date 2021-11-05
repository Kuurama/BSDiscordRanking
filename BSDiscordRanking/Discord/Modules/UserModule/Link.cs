using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using Discord.Commands;
using static System.String;


namespace BSDiscordRanking.Discord.Modules.UserModule
{
    [CheckChannel]
    public partial class UserModule : ModuleBase<SocketCommandContext>
    {
        [Command("link")]
        [Alias("register")]
        [Summary("Links your ScoreSaber account to your Discord's one.")]
        public async Task LinkUser(string p_ScoreSaberLink = "")
        {
            if (!IsNullOrEmpty(UserController.GetPlayer(Context.User.Id.ToString())))
                await ReplyAsync(
                    $"> :x: Sorry, but your account already has been linked. Please use `{BotHandler.m_Prefix}unlink`.");
            else if (!IsNullOrEmpty(p_ScoreSaberLink))
            {
                p_ScoreSaberLink = Regex.Match(p_ScoreSaberLink, @"\d+").Value;
                if (IsNullOrEmpty(UserController.GetPlayer(Context.User.Id.ToString())) &&
                    UserController.AccountExist(p_ScoreSaberLink) && !UserController.SSIsAlreadyLinked(p_ScoreSaberLink))
                {
                    UserController.AddPlayer(Context.User.Id.ToString(), p_ScoreSaberLink);
                    await ReplyAsync(
                        $"> :white_check_mark: Your account has been successfully linked.\nLittle tip: use `{BotHandler.m_Prefix}scan` to scan your latest passes!");
                }
                else if (!UserController.AccountExist(p_ScoreSaberLink))
                    await ReplyAsync("> :x: Sorry, but please enter a correct ScoreSaber Link/ID.");
                else if (UserController.SSIsAlreadyLinked(p_ScoreSaberLink))
                {
                    await ReplyAsync(
                        $"> :x: Sorry but this account is already linked to an other user.\nIf you entered the correct id and you didn't linked it on an other discord account\nPlease Contact an administrator.");
                }
                else
                    await ReplyAsync("> :x: Oopsie, unhandled error.");
            }
            else
                await ReplyAsync("> :x: Please enter a ScoreSaber link/id.");
        }
    }
}