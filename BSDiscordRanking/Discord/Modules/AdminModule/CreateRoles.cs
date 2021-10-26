using System;
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using Discord.Commands;

namespace BSDiscordRanking.Discord.Modules.AdminModule
{
    [RequireManagerRole]
    public partial class AdminModule : ModuleBase<SocketCommandContext>
    {
        [Command("createroles")]
        [Summary("Creates or updates level's roles. (discord roles)")]
        public async Task CreateRoles()
        {
            await new RoleController().CreateAllRoles(Context, false);
        }

        [Command("allowuser")]
        [Summary("Gives user the matching Ranked role.")]
        public async Task AllowUser(ulong p_DiscordID)
        {
            if (UserController.GiveRemoveBSDRRole(p_DiscordID, Context, false))
            {
                await ReplyAsync(
                    $"'{ConfigController.GetConfig().RolePrefix} Ranked' Role added to user <@{p_DiscordID}>,{Environment.NewLine}You might want to **check the pins** for *answers*, use the `{ConfigController.GetConfig().CommandPrefix[0]}getstarted` command to get started.");
            }
            else
            {
                await ReplyAsync($"This player can't be found/already have the '{ConfigController.GetConfig().RolePrefix} Ranked' Role.");
            }
        }
    }
}