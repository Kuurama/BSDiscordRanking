using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using Discord.Commands;

namespace BSDiscordRanking.Discord.Modules.AdminModule
{
    [PermissionHandler.RequirePermissionAttribute(PERMISSION)]
    public partial class AdminModule : ModuleBase<SocketCommandContext>
    {
        [Command("addchannel")]
        [Summary("Allow the bot to answer player's commands in this channel.")]
        public async Task AddChannel()
        {
            ConfigController.m_ConfigFormat.AuthorizedChannels ??= new List<ulong>();
            if (ConfigController.m_ConfigFormat.AuthorizedChannels.Any(p_Channel => Context.Message.Channel.Id == p_Channel))
            {
                await ReplyAsync("> :x: Sorry, this channel can already be used for user commands");
                return;
            }

            ConfigController.m_ConfigFormat.AuthorizedChannels.Add(Context.Message.Channel.Id);
            ConfigController.ReWriteConfig();
            await ReplyAsync("> :white_check_mark: This channel can now be used for user commands");
        }
    }
}
