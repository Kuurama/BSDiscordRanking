using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace BSDiscordRanking.Discord.Modules.UserModule
{
    [CheckChannel]
    
    public partial class UserModule : ModuleBase<SocketCommandContext>
    {
        [Summary("Sends test command.")]
        [SetSlashCommand("testcommand", "This is the test command description")]
        public static async Task TestCommand(SocketInteraction p_SocketInteraction)
        {
            switch (p_SocketInteraction)
            {
                // Slash Commands
                case SocketSlashCommand l_SlashCommand:
                    if (l_SlashCommand.CommandName == "testcommand")
                        await l_SlashCommand.RespondAsync("Here is a button!", component: new ComponentBuilder()
                            .WithButton(new ButtonBuilder("label", "customidhere"))
                            .Build());
                    break;
            }
        }
    }
}