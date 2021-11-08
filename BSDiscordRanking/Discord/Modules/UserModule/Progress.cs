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
        [Summary("Shows your progress on a specific map pools.")]
        public async Task Progress(string p_LevelID)
        {
            if (!UserController.UserExist(Context.User.Id.ToString()))
            {
                await ReplyAsync($"> :x: Sorry, you doesn't have any account linked. Please use `{BotHandler.m_Prefix}link <ScoreSaber link/id>` instead.\n> (Or to get started with the bot: use the `{BotHandler.m_Prefix}getstarted` command)");
            }
            else
            {
                int l_LevelID;
                try
                {
                    if (p_LevelID == null)
                    {
                        await Context.Channel.SendMessageAsync($"Please enter a Level number: `{ConfigController.GetConfig().CommandPrefix[0]}progress <Level>`");
                        return;
                    }

                    l_LevelID = int.Parse(p_LevelID);
                }
                catch
                {
                    await Context.Channel.SendMessageAsync($"Please enter a correct Level Number: `{ConfigController.GetConfig().CommandPrefix[0]}progress <Level>`");
                    return;
                }

                Player l_Player = new Player(UserController.GetPlayer(Context.User.Id.ToString()));
                PlayerPassPerLevelFormat l_PlayerPassPerLevel = l_Player.GetPlayerPassPerLevel();
                if (l_PlayerPassPerLevel == null)
                {
                    Console.WriteLine($"Player {UserController.GetPlayer(Context.User.Id.ToString())} : l_PlayerPassPerLevel is null");
                }
                else
                {
                    foreach (var l_PerLevelFormat in l_PlayerPassPerLevel.Levels)
                    {
                        if (l_LevelID == l_PerLevelFormat.LevelID)
                        {
                            if (l_PerLevelFormat.NumberOfMapDiffInLevel == 0)
                            {
                                await Context.Channel.SendMessageAsync($"Sorry but the level {l_PerLevelFormat.LevelID} doesn't contain any map.");
                                return;
                            }

                            var l_Builder = new EmbedBuilder()
                                .AddField("Pool", $"Level {l_PerLevelFormat.LevelID}", true)
                                .AddField("Progress Bar", GenerateProgressBar(l_PerLevelFormat.NumberOfPass, l_PerLevelFormat.NumberOfMapDiffInLevel, 10), true)
                                .AddField("Progress Amount", $"{Math.Round((float) (l_PerLevelFormat.NumberOfPass / (float) l_PerLevelFormat.NumberOfMapDiffInLevel) * 100.0f)}% ({l_PerLevelFormat.NumberOfPass}/{l_PerLevelFormat.NumberOfMapDiffInLevel})  {l_PerLevelFormat.TrophyString}", true);
                            var l_Embed = l_Builder.Build();
                            await Context.Channel.SendMessageAsync(null, embed: l_Embed).ConfigureAwait(false);
                            return;
                        }
                    }

                    await Context.Channel.SendMessageAsync($"Sorry but the level {p_LevelID} doesn't exist.");
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
                PlayerPassPerLevelFormat l_PlayerPassPerLevel = l_Player.GetPlayerPassPerLevel();

                if (l_PlayerPassPerLevel == null)
                {
                    Console.WriteLine($"Player {UserController.GetPlayer(Context.User.Id.ToString())} : l_PlayerPassPerLevel is null");
                    return;
                }
                else
                {
                    int l_MessagesIndex = 0;
                    List<string> l_Messages = new List<string> {""};
                    if (l_PlayerPassPerLevel.Levels != null)
                    {
                        var l_Builder = new EmbedBuilder()
                            .WithTitle($"{l_Player.m_PlayerFull.playerInfo.playerName}'s Progress Tracker")
                            .WithDescription("Here is your current progress through the map pools:")
                            .WithThumbnailUrl("https://new.scoresaber.com" + l_Player.m_PlayerFull.playerInfo.avatar);
                        foreach (var l_PerLevelFormat in l_PlayerPassPerLevel.Levels)
                        {
                            if (l_PerLevelFormat.NumberOfMapDiffInLevel > 0)
                            {
                                if (l_Messages[l_MessagesIndex].Length +
                                    $"Level {l_PerLevelFormat.LevelID}: {GenerateProgressBar(l_PerLevelFormat.NumberOfPass, l_PerLevelFormat.NumberOfMapDiffInLevel, 10)} {Math.Round((float) (l_PerLevelFormat.NumberOfPass / (float) l_PerLevelFormat.NumberOfMapDiffInLevel) * 100.0f)}% ({l_PerLevelFormat.NumberOfPass}/{l_PerLevelFormat.NumberOfMapDiffInLevel})  {l_PerLevelFormat.TrophyString}\n"
                                        .Length > 900)
                                {
                                    l_MessagesIndex++;
                                }

                                if (l_Messages.Count < l_MessagesIndex + 1)
                                {
                                    l_Messages.Add(""); /// Initialize the next used index.
                                }

                                l_Messages[l_MessagesIndex] +=
                                    $"Level {l_PerLevelFormat.LevelID}: {GenerateProgressBar(l_PerLevelFormat.NumberOfPass, l_PerLevelFormat.NumberOfMapDiffInLevel, 10)} {Math.Round((float) (l_PerLevelFormat.NumberOfPass / (float) l_PerLevelFormat.NumberOfMapDiffInLevel) * 100.0f)}% ({l_PerLevelFormat.NumberOfPass}/{l_PerLevelFormat.NumberOfMapDiffInLevel})  {l_PerLevelFormat.TrophyString}" +
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