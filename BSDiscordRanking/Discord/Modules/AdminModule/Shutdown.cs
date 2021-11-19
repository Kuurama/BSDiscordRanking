using System;
using System.Threading.Tasks;
using Discord.Commands;

namespace BSDiscordRanking.Discord.Modules.AdminModule
{
    [PermissionHandler.RequirePermissionAttribute(Permission)]
    public partial class AdminModule : ModuleBase<SocketCommandContext>
    {
        [Command("shutdown")]
        [Summary("Shutdown the bot.")]
        public async Task Shutdown()
        {
            await ReplyAsync("**Shutting Down the bot**");
            Environment.Exit(0);
        }
    }
}