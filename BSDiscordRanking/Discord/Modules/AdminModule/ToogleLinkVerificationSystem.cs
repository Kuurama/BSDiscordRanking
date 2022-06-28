using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using Discord.Commands;

namespace BSDiscordRanking.Discord.Modules.AdminModule
{
    [PermissionHandler.RequirePermissionAttribute(PERMISSION)]
    public partial class AdminModule
    {
        [Command("togglelinkverificationsystem")]
        [Summary("Toggle the Link Verification System.")]
        public async Task ToggleLinkVerificationSystem()
        {
            ConfigController.m_ConfigFormat.EnableLinkVerificationSystem = !ConfigController.m_ConfigFormat.EnableLinkVerificationSystem;
            ConfigController.ReWriteConfig();
            await ReplyAsync($"> :white_check_mark: Link Verification System has been toggled from {!ConfigController.m_ConfigFormat.EnableLinkVerificationSystem} to {ConfigController.m_ConfigFormat.EnableLinkVerificationSystem}.");
        }
    }
}
