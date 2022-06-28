
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using Discord.Commands;

namespace BSDiscordRanking.Discord.Modules.AdminModule
{
    [PermissionHandler.RequirePermissionAttribute(PERMISSION)]
    public partial class AdminModule
    {
        [Command("setlinkverificationchannel")]
        [Summary("Set this channel as the Link Verification Channel (that will be used if the setting is enabled.")]
        public async Task SetLinkVerificationChannel()
        {
            ConfigController.GetConfig();
            ConfigController.m_ConfigFormat.LinkVerificationChannel = Context.Channel.Id;
            ConfigController.ReWriteConfig();
            await ReplyAsync("> :white_check_mark: This channel is now used as the LinkConfirmationChannel.");
        }
    }
}
