using System.Linq;
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using static System.String;

namespace BSDiscordRanking.Discord.Modules.UserModule
{
    [CheckChannel]
    public partial class UserModule : ModuleBase<SocketCommandContext>
    {
        [Command("help")]
        [Summary("Shows all the commands & their summaries.")]
        public async Task Help(string p_Command = null)
        {
            if (p_Command == null)
            {
                bool l_IsAdmin = false;
                if (Context.User is SocketGuildUser l_User)
                    if (l_User.Roles.Any(p_Role => p_Role.Id == ConfigController.GetConfig().BotManagementRoleID))
                        l_IsAdmin = true;


                int i = 0;
                foreach (var l_Module in BotHandler.m_Commands.Modules)
                {
                    EmbedBuilder l_Builder = new EmbedBuilder();
                    l_Builder.WithTitle(l_Module.Name);
                    foreach (var l_Command in l_Module.Commands)
                    {
                        string l_Title = ConfigController.GetConfig().CommandPrefix.First() + l_Command.Name;
                        foreach (var l_Parameter in l_Command.Parameters)
                        {
                            l_Title += " [" + l_Parameter.Name.Replace("p_", "") + "]";
                        }

                        l_Builder.AddField(l_Title, l_Command.Summary, true);
                        l_Builder.WithFooter("Prefix: " + Join(", ", ConfigController.GetConfig().CommandPrefix) + " | Bot made by Kuurama#3423 & Julien#1234");
                    }

                    if (l_Module.Name.Contains("Admin"))
                    {
                        if (!l_IsAdmin)
                            continue;
                        l_Builder.WithColor(Color.Red);
                        l_Builder.Footer = null;
                    }

                    await Context.Channel.SendMessageAsync("", false, l_Builder.Build());
                    i++;
                }
            }
            else
            {
                var l_FoundCommand = BotHandler.m_Commands.Commands.ToList().Find(x =>
                    x.Name == p_Command || x.Aliases.ToList().Find(x => x == p_Command) == p_Command);
                if (l_FoundCommand != null)
                {
                    EmbedBuilder l_Builder = new EmbedBuilder();
                    string l_Title = ConfigController.GetConfig().CommandPrefix.First() + l_FoundCommand.Name;
                    foreach (var l_Parameter in l_FoundCommand.Parameters)
                    {
                        l_Title += " [" + l_Parameter.Name.Replace("p_", "") + "]";
                    }

                    l_Builder.AddField(l_Title, l_FoundCommand.Summary, true);
                    await Context.Channel.SendMessageAsync("", false, l_Builder.Build());
                }
                else
                {
                    await ReplyAsync("> :x: Sorry, command was not found.");
                }
            }
        }
    }
}