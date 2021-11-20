using System;
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
                int i = 0;
                foreach (var l_Module in BotHandler.m_Commands.Modules)
                {
                    // This is very bad, should be changed one day but I don't know how to do it
                    if (l_Module.Name == "AdminModule")
                    {
                        if (!(PermissionHandler.GetUserPermLevel(Context) >= 2))
                            break;
                    }
                    else if (l_Module.Name == "EditorModule")
                    {
                        if (!(PermissionHandler.GetUserPermLevel(Context) >= 1))
                            break;
                    }


                    EmbedBuilder l_Builder = new EmbedBuilder();
                    l_Builder.WithTitle(l_Module.Name);
                    foreach (var l_Command in l_Module.Commands)
                    {
                        string l_Title = ConfigController.GetConfig().CommandPrefix.First() + l_Command.Name;
                        foreach (var l_Parameter in l_Command.Parameters)
                        {
                            if (l_Parameter.Summary != null)
                            {
                                if (l_Parameter.Summary != "DoNotDisplayOnHelp")
                                {
                                    if (l_Title.Length + $" [{l_Parameter.Name.Replace("p_", "")}]".Length < 250) /// 250 instead of 256 so i can write "[...]" at the end of the string without being lengh limited by Discord.
                                    {
                                        l_Title += $" [{l_Parameter.Name.Replace("p_", "")}]";
                                    }
                                    else
                                    {
                                        l_Title += "[...]";
                                        break;
                                    }
                                    
                                }
                            }
                            else
                            {
                                if (l_Title.Length + $" [{l_Parameter.Name.Replace("p_", "")}]".Length < 250) /// 250 instead of 256 so i can write "[...]" at the end of the string without being lengh limited by Discord.
                                {
                                    l_Title += $" [{l_Parameter.Name.Replace("p_", "")}]";
                                }
                                else
                                {
                                    l_Title += "[...]";
                                    break;
                                }
                            }
                           
                        }

                        l_Builder.AddField(l_Title, l_Command.Summary, true);
                        l_Builder.WithFooter("Prefix: " + Join(", ", ConfigController.GetConfig().CommandPrefix) + " | Bot made by Kuurama#3423 & Julien#1234");
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
                        if (l_Parameter.Summary != null)
                        {
                            if (l_Parameter.Summary != "DoNotDisplayOnHelp")
                            {
                                if (l_Title.Length + $" [{l_Parameter.Name.Replace("p_", "")}]".Length < 250) /// 250 instead of 256 so i can write "[...]" at the end of the string without being lengh limited by Discord.
                                {
                                    l_Title += $" [{l_Parameter.Name.Replace("p_", "")}]";
                                }
                                else
                                {
                                    l_Title += "[...]";
                                    break;
                                }
                                    
                            }
                        }
                        else
                        {
                            if (l_Title.Length + $" [{l_Parameter.Name.Replace("p_", "")}]".Length < 250) /// 250 instead of 256 so i can write "[...]" at the end of the string without being lengh limited by Discord.
                            {
                                l_Title += $" [{l_Parameter.Name.Replace("p_", "")}]";
                            }
                            else
                            {
                                l_Title += "[...]";
                                break;
                            }
                        }
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