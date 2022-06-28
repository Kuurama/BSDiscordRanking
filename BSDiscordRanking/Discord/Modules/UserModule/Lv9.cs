using System.Threading.Tasks;
using Discord.Commands;

namespace BSDiscordRanking.Discord.Modules.UserModule
{
    public partial class UserModule : ModuleBase<SocketCommandContext>
    {
        [Command("lvl9")]
        [Summary("Sends tips.")]
        public async Task SendTipsLVL9()
        {
            await Context.Channel.SendMessageAsync("Here.. (take it, but it's secret) : ||https://youtu.be/JlqR_A2vGi8||");
        }
    }
}
