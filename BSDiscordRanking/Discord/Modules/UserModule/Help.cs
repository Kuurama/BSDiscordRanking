using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using BSDiscordRanking.Formats;
using BSDiscordRanking.Formats.Controller;
using Discord;
using Discord.Commands;
using static System.String;

namespace BSDiscordRanking.Discord.Modules.UserModule
{
    public partial class UserModule : ModuleBase<SocketCommandContext>
    {
        [Command("help")]
        [Summary("Shows all the commands & their summaries.")]
        public async Task Help(string p_Command = null)
        {
            ConfigFormat l_Config = ConfigController.GetConfig();
            List<int> l_PermLevel = PermissionHandler.GetUserPermLevel(Context);
            if (p_Command == null)
            {
                foreach (ModuleInfo l_Module in BotHandler.m_Commands.Modules)
                {
                    EmbedBuilder l_Builder = new EmbedBuilder();
                    if (l_Module.Name == "AdminModule")
                    {
                        if (l_PermLevel.FindIndex(p_X => p_X >= 3) < 0)
                            continue;
                        l_Builder.WithColor(GetRoleColor(RoleController.ReadRolesDB().Roles, Context.Guild.Roles, 0, l_Config.BotAdminRoleID));
                    }
                    else if (l_Module.Name == "ScoringTeamModule")
                    {
                        if (l_PermLevel.FindIndex(p_X => p_X == 2) < 0)
                            continue;
                        l_Builder.WithColor(GetRoleColor(RoleController.ReadRolesDB().Roles, Context.Guild.Roles, 0, l_Config.ScoringTeamRoleID));
                    }
                    else if (l_Module.Name == "RankingTeamModule")
                    {
                        if (l_PermLevel.FindIndex(p_X => p_X == 1) < 0)
                            continue;
                        l_Builder.WithColor(GetRoleColor(RoleController.ReadRolesDB().Roles, Context.Guild.Roles, 0, l_Config.RankingTeamRoleID));
                    }
                    else if (l_Module.Name == "UserModule")
                    {
                        RoleFormat l_RoleFormat = RoleController.ReadRolesDB().Roles.Find(p_X => p_X.LevelID == 0);
                        if (l_RoleFormat != null)
                        {
                            l_Builder.WithColor(GetRoleColor(RoleController.ReadRolesDB().Roles, Context.Guild.Roles, 0, l_RoleFormat.RoleID));
                        }
                    }


                    
                    l_Builder.WithTitle(l_Module.Name);
                    foreach (CommandInfo l_Command in l_Module.Commands)
                    {
                        string l_Title = ConfigController.GetConfig().CommandPrefix.First() + l_Command.Name;
                        foreach (ParameterInfo l_Parameter in l_Command.Parameters)
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

                        l_Builder.AddField(l_Title, l_Command.Summary, true);
                        l_Builder.WithFooter("Prefix: " + Join(", ", ConfigController.GetConfig().CommandPrefix) + " | Bot made by Kuurama#3423 & Julien#1234");
                    }

                    await Context.Channel.SendMessageAsync("", false, l_Builder.Build());
                }
            }
            else
            {
                CommandInfo l_FoundCommand = BotHandler.m_Commands.Commands.ToList().Find(p_X =>
                    p_X.Name == p_Command || p_X.Aliases.ToList().Find(p_Y => p_Y == p_Command) == p_Command);
                if (l_FoundCommand != null)
                {
                    EmbedBuilder l_Builder = new EmbedBuilder();
                    if (l_FoundCommand.Module.Name == "AdminModule")
                    {
                        if (l_PermLevel.FindIndex(p_X => p_X == 3) < 0)
                            return;
                        l_Builder.WithColor(GetRoleColor(RoleController.ReadRolesDB().Roles, Context.Guild.Roles, 0, l_Config.BotAdminRoleID));
                    }
                    else if (l_FoundCommand.Module.Name == "ScoringTeamModule")
                    {
                        if (l_PermLevel.FindIndex(p_X => p_X == 2) < 0)
                            return;
                        l_Builder.WithColor(GetRoleColor(RoleController.ReadRolesDB().Roles, Context.Guild.Roles, 0, l_Config.ScoringTeamRoleID));
                    }
                    else if (l_FoundCommand.Module.Name == "RankingTeamModule")
                    {
                        if (l_PermLevel.FindIndex(p_X => p_X == 1) < 0)
                            return;
                        l_Builder.WithColor(GetRoleColor(RoleController.ReadRolesDB().Roles, Context.Guild.Roles, 0, l_Config.RankingTeamRoleID));
                    }
                    else if (l_FoundCommand.Module.Name == "UserModule")
                    {
                        RoleFormat l_RoleFormat = RoleController.ReadRolesDB().Roles.Find(p_X => p_X.LevelID == 0);
                        if (l_RoleFormat != null)
                        {
                            l_Builder.WithColor(GetRoleColor(RoleController.ReadRolesDB().Roles, Context.Guild.Roles, 0, l_RoleFormat.RoleID));
                        }
                    }
                    string l_Title = ConfigController.GetConfig().CommandPrefix.First() + l_FoundCommand.Name;
                    foreach (ParameterInfo l_Parameter in l_FoundCommand.Parameters)
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