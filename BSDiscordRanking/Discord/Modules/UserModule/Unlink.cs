using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using Discord.Commands;
using static System.String;

namespace BSDiscordRanking.Discord.Modules.UserModule
{
    public partial class UserModule : ModuleBase<SocketCommandContext>
    {
        [Command("unlink")]
        [Summary("Unlinks your discord accounts from your ScoreSaber's one.")]
        public async Task UnLinkUser()
        {
            if (IsNullOrEmpty(UserController.GetPlayer(Context.User.Id.ToString())))
            {
                await ReplyAsync($"> :x: Sorry, you don't have any account linked. Please use `{BotHandler.m_Prefix}link` instead.\n> (Or to get started with the bot: use the `{BotHandler.m_Prefix}getstarted command`)");
            }
            else
            {
                UserController.RemovePlayer(Context.User.Id.ToString());
                await ReplyAsync("> :white_check_mark: Your account was successfully unlinked!");
            }
        }
    }
}