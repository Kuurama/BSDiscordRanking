using System.Linq;
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using Discord.Commands;

namespace BSDiscordRanking.Discord.Modules.AdminModule
{
    [PermissionHandler.RequirePermissionAttribute(Permission)]
    public partial class AdminModule : ModuleBase<SocketCommandContext>
    {
        [Command("removechannel")]
        [Summary("Remove the bot's permission to answer player's commands in this channel.")]
        public async Task RemoveChannel()
        {
            foreach (var l_Channel in ConfigController.m_ConfigFormat.AuthorizedChannels.Where(l_Channel => Context.Message.Channel.Id == l_Channel))
            {
                ConfigController.m_ConfigFormat.AuthorizedChannels.Remove(l_Channel);
            }

            ConfigController.ReWriteConfig();
            await ReplyAsync("> :white_check_mark: This channel cannot longer be used for user commands");
        }
    }
}