using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using BSDiscordRanking.Formats.Player;
using Discord;
using Discord.Commands;

namespace BSDiscordRanking.Discord.Modules.UserModule
{
    [CheckChannel]
    public partial class UserModule : ModuleBase<SocketCommandContext>
    {
        [Command("progress")]
        [Summary("Shows a specific player's progress.")]
        public async Task Progress(string p_DiscordOrScoreSaberID)
        {
            PlayerFromDiscordOrScoreSaberIDFormat l_DiscordOrScoreSaberIDFormat = PlayerFromDiscordOrScoreSaberID(p_DiscordOrScoreSaberID, Context);
            Player l_Player = null;
            if (!l_DiscordOrScoreSaberIDFormat.IsDiscordLinked && !l_DiscordOrScoreSaberIDFormat.IsScoreSaberAccount)
            {
                await ReplyAsync($"> :x: Sorry, This Discord account isn't linked.");
            }
            else if (LevelController.GetLevelControllerCache().LevelID.Count <= 0!)
            {
                await ReplyAsync($"> :x: Sorry, but please create a level. Please use `{BotHandler.m_Prefix}addmap` command first.");
            }
            else if (l_DiscordOrScoreSaberIDFormat.IsScoreSaberAccount && l_DiscordOrScoreSaberIDFormat.IsDiscordLinked)
            {
                p_DiscordOrScoreSaberID = UserController.GetDiscordID(p_DiscordOrScoreSaberID);
            }
            else if (l_DiscordOrScoreSaberIDFormat.IsScoreSaberAccount && !l_DiscordOrScoreSaberIDFormat.IsDiscordLinked)
            {
                l_Player = new Player(p_DiscordOrScoreSaberID);
            }

            // ReSharper disable once ConstantNullCoalescingCondition
            l_Player ??= new Player(UserController.GetPlayer(l_DiscordOrScoreSaberIDFormat.DiscordID));
            l_Player.LoadPass();
            PlayerPassPerLevelFormat l_PlayerPassPerLevel = l_Player.GetPlayerPassPerLevel();
            if (l_PlayerPassPerLevel == null)
            {
                await ReplyAsync("> Sorry but user ");
                return;
            }
            else
            {
                int l_MessagesIndex = 0;
                bool l_HaveAPass = false;
                List<string> l_Messages = new List<string> { "" };
                if (l_PlayerPassPerLevel.Levels != null)
                {
                    Color l_Color = GetRoleColor(RoleController.ReadRolesDB().Roles, Context.Guild.Roles, l_Player.GetPlayerLevel());
                    var l_Builder = new EmbedBuilder()
                        .WithDescription("Here is your current progress through the map pools:")
                        .WithThumbnailUrl(l_Player.m_PlayerFull.profilePicture)
                        .WithColor(l_Color);
                    foreach (var l_PerLevelFormat in l_PlayerPassPerLevel.Levels)
                    {
                        if (l_PerLevelFormat.NumberOfMapDiffInLevel > 0)
                        {
                            if (l_Messages[l_MessagesIndex].Length +
                                $"Level {l_PerLevelFormat.LevelID}: {GenerateProgressBar(l_PerLevelFormat.NumberOfPass, l_PerLevelFormat.NumberOfMapDiffInLevel, 10)} {Math.Round((float)(l_PerLevelFormat.NumberOfPass / (float)l_PerLevelFormat.NumberOfMapDiffInLevel) * 100.0f)}% ({l_PerLevelFormat.NumberOfPass}/{l_PerLevelFormat.NumberOfMapDiffInLevel})  {l_PerLevelFormat.TrophyString}\n"
                                    .Length > 900)
                            {
                                l_MessagesIndex++;
                            }

                            if (l_Messages.Count < l_MessagesIndex + 1)
                            {
                                l_Messages.Add(""); /// Initialize the next used index.
                            }

                            l_Messages[l_MessagesIndex] +=
                                $"Level {l_PerLevelFormat.LevelID}: {GenerateProgressBar(l_PerLevelFormat.NumberOfPass, l_PerLevelFormat.NumberOfMapDiffInLevel, 10)} {Math.Round((float)(l_PerLevelFormat.NumberOfPass / (float)l_PerLevelFormat.NumberOfMapDiffInLevel) * 100.0f)}% ({l_PerLevelFormat.NumberOfPass}/{l_PerLevelFormat.NumberOfMapDiffInLevel})  {l_PerLevelFormat.TrophyString}" +
                                Environment.NewLine;

                            if (l_PerLevelFormat.NumberOfPass > 0)
                            {
                                l_HaveAPass = true;
                            }
                        }
                    }

                    if (l_HaveAPass)
                    {
                        l_Builder.WithTitle($"{l_Player.m_PlayerFull.name}'s Progress Tracker");
                    }
                    else if (!l_DiscordOrScoreSaberIDFormat.IsDiscordLinked)
                    {
                        l_Builder.WithTitle($"{l_Player.m_PlayerFull.name}'s Progress Tracker (Unlinked Account)");
                    }

                    foreach (var l_Message in l_Messages)
                    {
                        l_Builder.AddField("\u200B", l_Message);
                    }

                    await Context.Channel.SendMessageAsync(null, embed: l_Builder.Build()).ConfigureAwait(false);
                }
            }
        }

        [Command("progress")]
        [Summary("Shows your progress through the map pools.")]
        public async Task Progress()
        {
            if (!UserController.UserExist(Context.User.Id.ToString()))
            {
                await ReplyAsync($"> :x: Sorry, you doesn't have any account linked. Please use `{BotHandler.m_Prefix}link <ScoreSaber link/id>` instead.\n> (Or to get started with the bot: use the `{BotHandler.m_Prefix}getstarted` command)");
            }
            else if (LevelController.GetLevelControllerCache().LevelID.Count <= 0!)
            {
                await ReplyAsync($"> :x: Sorry, but please create a level. Please use `{BotHandler.m_Prefix}addmap` command first.");
            }
            else
            {
                Player l_Player = new Player(UserController.GetPlayer(Context.User.Id.ToString()));
                l_Player.LoadPass();
                PlayerPassPerLevelFormat l_PlayerPassPerLevel = l_Player.GetPlayerPassPerLevel();
                if (l_PlayerPassPerLevel == null)
                {
                    Console.WriteLine($"Player {UserController.GetPlayer(Context.User.Id.ToString())} : l_PlayerPassPerLevel is null");
                    return;
                }
                else
                {
                    int l_MessagesIndex = 0;
                    List<string> l_Messages = new List<string> { "" };
                    if (l_PlayerPassPerLevel.Levels != null)
                    {
                        Color l_Color = GetRoleColor(RoleController.ReadRolesDB().Roles, Context.Guild.Roles, l_Player.GetPlayerLevel());
                        var l_Builder = new EmbedBuilder()
                            .WithTitle($"{l_Player.m_PlayerFull.name}'s Progress Tracker")
                            .WithDescription("Here is your current progress through the map pools:")
                            .WithThumbnailUrl(l_Player.m_PlayerFull.profilePicture)
                            .WithColor(l_Color);
                        foreach (var l_PerLevelFormat in l_PlayerPassPerLevel.Levels)
                        {
                            if (l_PerLevelFormat.NumberOfMapDiffInLevel > 0)
                            {
                                if (l_Messages[l_MessagesIndex].Length +
                                    $"Level {l_PerLevelFormat.LevelID}: {GenerateProgressBar(l_PerLevelFormat.NumberOfPass, l_PerLevelFormat.NumberOfMapDiffInLevel, 10)} {Math.Round((float)(l_PerLevelFormat.NumberOfPass / (float)l_PerLevelFormat.NumberOfMapDiffInLevel) * 100.0f)}% ({l_PerLevelFormat.NumberOfPass}/{l_PerLevelFormat.NumberOfMapDiffInLevel})  {l_PerLevelFormat.TrophyString}\n"
                                        .Length > 900)
                                {
                                    l_MessagesIndex++;
                                }

                                if (l_Messages.Count < l_MessagesIndex + 1)
                                {
                                    l_Messages.Add(""); /// Initialize the next used index.
                                }

                                l_Messages[l_MessagesIndex] +=
                                    $"Level {l_PerLevelFormat.LevelID}: {GenerateProgressBar(l_PerLevelFormat.NumberOfPass, l_PerLevelFormat.NumberOfMapDiffInLevel, 10)} {Math.Round((float)(l_PerLevelFormat.NumberOfPass / (float)l_PerLevelFormat.NumberOfMapDiffInLevel) * 100.0f)}% ({l_PerLevelFormat.NumberOfPass}/{l_PerLevelFormat.NumberOfMapDiffInLevel})  {l_PerLevelFormat.TrophyString}" +
                                    Environment.NewLine;
                            }
                        }

                        foreach (var l_Message in l_Messages)
                        {
                            l_Builder.AddField("\u200B", l_Message);
                        }

                        await Context.Channel.SendMessageAsync(null, embed: l_Builder.Build()).ConfigureAwait(false);
                    }
                }
            }
        }
    }
}