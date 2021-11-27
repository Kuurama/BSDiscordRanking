using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using Discord.Commands;

namespace BSDiscordRanking.Discord.Modules.AdminModule
{
    [PermissionHandler.RequirePermissionAttribute(Permission)]
    public partial class AdminModule : ModuleBase<SocketCommandContext>
    {
        [Command("confirmationchannel")]
        [Summary("Set this channel as the confirmation channel in order to be informed of cheated or AdminPingOnPass passed maps.")]
        public async Task SetConfirmationChannel()
        {
            ConfigController.GetConfig();
            ConfigController.m_ConfigFormat.AdminPingOnPassChannel = Context.Channel.Id;
            ConfigController.ReWriteConfig();
            await ReplyAsync("> :white_check_mark: This channel is now used as the ConfirmationChannel (PingOnPass and cheated score logging).");
        }
    }
}