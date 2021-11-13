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
        
        [SetSlashCommand("button-create", "This is the test command description")]
        [SetStringRequirement("button-name","Pings the specified user.", true)]
        [Summary("Sends button command.")]
        public static async Task TestCommand(SocketInteraction p_SocketInteraction)
        {
            switch (p_SocketInteraction)
            {
                // Slash Commands
                case SocketSlashCommand l_SlashCommand:
                    if (l_SlashCommand.CommandName == "button-create")
                        await l_SlashCommand.RespondAsync("Here is a button with choosed text!", component: new ComponentBuilder()
                            .WithButton(new ButtonBuilder(l_SlashCommand.Data.Options.First().Value.ToString(), "customidhere"))
                            .Build());
                    break;
            }
        }
    }
}