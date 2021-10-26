using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace BSDiscordRanking.Discord.Modules.UserModule
{
    [CheckChannel]
    public partial class UserModule : ModuleBase<SocketCommandContext>
    {
        [Command("ping")]
        [Summary("Shows the latency from Discord & ScoreSaber")]
        public async Task Ping()
        {
            EmbedBuilder l_EmbedBuilder = new EmbedBuilder();
            l_EmbedBuilder.AddField("Discord: ", new Ping().Send("discord.com").RoundtripTime + "ms");
            l_EmbedBuilder.AddField("ScoreSaber: ", new Ping().Send("scoresaber.com").RoundtripTime + "ms");
            l_EmbedBuilder.WithFooter("#LoveArche",
                "https://images.genius.com/d4b8905048993e652aba3d8e105b5dbf.1000x1000x1.jpg");

            l_EmbedBuilder.WithColor(Color.Blue);
            await Context.Channel.SendMessageAsync("", false, l_EmbedBuilder.Build());
        }
    }
}