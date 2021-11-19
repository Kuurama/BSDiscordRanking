using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using Discord.Commands;

namespace BSDiscordRanking.Discord.Modules.AdminModule
{
    [PermissionHandler.RequirePermissionAttribute(Permission)]
    public partial class AdminModule : ModuleBase<SocketCommandContext>
    {
        [Command("loggingchannel")]
        [Alias("logchannel")]
        [Summary("Set this channel as a logs's message sending channel (Map Added/Deleted etc).")]
        public async Task SetChannel()
        {
            ConfigController.GetConfig();
            ConfigController.m_ConfigFormat.LoggingChannel = Context.Channel.Id;
            ConfigController.ReWriteConfig();
            await ReplyAsync("> :white_check_mark: This channel is now used as log-channel.");
        }
    }
}