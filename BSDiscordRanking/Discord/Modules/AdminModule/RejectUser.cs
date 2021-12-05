using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using Discord.Commands;

namespace BSDiscordRanking.Discord.Modules.AdminModule
{
    [PermissionHandler.RequirePermissionAttribute(PERMISSION)]
    public partial class AdminModule : ModuleBase<SocketCommandContext>
    {
        [Command("rejectuser")]
        [Summary("Removes user it's matching Ranked role.")]
        public async Task RemoveUser(ulong p_DiscordID)
        {
            if (UserController.GiveRemoveBSDRRole(p_DiscordID, Context, true))
                await ReplyAsync($"'{ConfigController.GetConfig().RolePrefix} Ranked' Role removed from user <@{p_DiscordID}>.");
            else
                await ReplyAsync($"This player can't be found/do not have the '{ConfigController.GetConfig().RolePrefix} Ranked' Role.");
        }
    }
}