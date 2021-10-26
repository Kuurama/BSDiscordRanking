using System.Threading.Tasks;
using Discord.Commands;

namespace BSDiscordRanking.Discord.Modules.UserModule
{
    [CheckChannel]
    public partial class UserModule : ModuleBase<SocketCommandContext>
    {
        [Command("lvl9")]
        [Summary("Sends tips.")]
        public async Task SendTipsLVL9()
        {
            await Context.Channel.SendMessageAsync("Here.. (take it, but it's secret) : ||http://prntscr.com/soylt9||", false);
        }

    }
}