using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using Discord.Commands;

namespace BSDiscordRanking.Discord.Modules.UserModule
{
    public partial class UserModule : ModuleBase<SocketCommandContext>
    {
        [Command("rerole")]
        [Summary("Updates your discord role in case your roles are incorrect.")]
        public async Task ReRole()
        {
            if (!UserController.UserExist(Context.User.Id.ToString()))
            {
                await ReplyAsync($"> :x: Sorry, you don't have any account linked. Please use `{BotHandler.m_Prefix}link <ScoreSaber link/id>` instead.\n> (Or to get started with the bot: use the `{BotHandler.m_Prefix}getstarted` command)");
            }

            int l_Level = Player.GetStaticPlayerLevel(UserController.GetPlayer(Context.User.Id.ToString()));

            await ReplyAsync($"> :clock1: The bot will now update {Context.User.Username}'s roles. This step can take a while.");
            Task l_RoleUpdate = UserController.UpdateRoleAndSendMessage(Context, Context.User.Id, l_Level);
        }
    }
}
