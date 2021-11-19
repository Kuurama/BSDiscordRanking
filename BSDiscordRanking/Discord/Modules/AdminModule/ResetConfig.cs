using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using Discord.Commands;

namespace BSDiscordRanking.Discord.Modules.AdminModule
{
    [PermissionHandler.RequirePermissionAttribute(Permission)]
    public partial class AdminModule : ModuleBase<SocketCommandContext>
    {
        [Command("reset-config")]
        [Summary("Resets the bot config file, stops the bot.")]
        public async Task Reset_config()
        {
            await ReplyAsync("> :white_check_mark: After the bot finished to reset the config, it will stops.");
            ConfigController.CreateConfig();
        }
    }
}