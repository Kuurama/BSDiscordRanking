using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace BSDiscordRanking.Discord.Modules.UserModule
{
    [CheckChannel]
    public partial class UserModule : ModuleBase<SocketCommandContext>
    {
        [SetSlashCommand("ping-user", "It will ping the defined user.")]
        [SetUserRequirement("user","Pings the specified user.", true)]
        [Summary("Sends UserRequirement test command.")]
        public static async Task PingUser(SocketInteraction p_SocketInteraction)
        {
            switch (p_SocketInteraction)
            {
                // Slash Commands
                case SocketSlashCommand l_SlashCommand:
                    if (l_SlashCommand.CommandName == "ping-user")
                    {
                        var l_GuildUser = (SocketGuildUser)l_SlashCommand.Data.Options.First().Value;
                        
                        await l_SlashCommand.RespondAsync($"Take that ping! <@{l_GuildUser.Id}> ", embed: new EmbedBuilder()
                            .WithDescription($"Pinged by: <@{l_SlashCommand.User.Id}>")
                            .Build());
                    }

                    break;
            }
        }
    }
}