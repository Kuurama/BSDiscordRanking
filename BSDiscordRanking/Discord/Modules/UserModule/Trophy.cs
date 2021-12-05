using System;
using System.Collections.Generic;
using System.Linq;
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
        [Command("trophy")]
        [Summary("Shows your trophy on a Level, or even on a specific level Category.")]
        public async Task ShowTrophy(string p_LevelID = null, [Remainder] string p_Category = null)
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
                        await Context.Channel.SendMessageAsync($"Please enter a Level number: `{ConfigController.GetConfig().CommandPrefix[0]}trophy <Level>`");
                        return;
                    }

                    l_LevelID = int.Parse(p_LevelID);
                }
                catch
                {
                    await Context.Channel.SendMessageAsync($"Please enter a correct Level Number: `{ConfigController.GetConfig().CommandPrefix[0]}trophy <Level>`");
                    return;
                }

                Player l_Player = new Player(UserController.GetPlayer(Context.User.Id.ToString()));
                PlayerStatsFormat l_PlayerStats = l_Player.GetStats();
                if (l_PlayerStats == null)
                {
                    Console.WriteLine($"Player {UserController.GetPlayer(Context.User.Id.ToString())} : l_PlayerStats is null");
                }
                else
                {
                    foreach (PassedLevel l_PerLevelFormat in l_PlayerStats.Levels)
                        if (l_LevelID == l_PerLevelFormat.LevelID)
                        {
                            if (p_Category == null)
                            {
                                if (l_PerLevelFormat.TotalNumberOfMaps == 0)
                                {
                                    await Context.Channel.SendMessageAsync($"Sorry but the level {l_PerLevelFormat.LevelID} doesn't contain any map.");
                                    return;
                                }

                                EmbedBuilder l_Builder = new EmbedBuilder().AddField($"Level {l_PerLevelFormat.LevelID} {GetTrophyString(true, l_PerLevelFormat.NumberOfPass, l_PerLevelFormat.TotalNumberOfMaps)}",
                                    $"{l_PerLevelFormat.NumberOfPass}/{l_PerLevelFormat.TotalNumberOfMaps} ({Math.Round(l_PerLevelFormat.NumberOfPass / (float)l_PerLevelFormat.TotalNumberOfMaps * 100.0f)}%)");
                                Embed l_Embed = l_Builder.Build();
                                await Context.Channel.SendMessageAsync(null, embed: l_Embed).ConfigureAwait(false);
                                return;
                            }

                            p_Category = FirstCharacterToUpper(p_Category);
                            int l_CategoryIndex = l_PerLevelFormat.Categories.FindIndex(p_X => p_X.Category == p_Category);
                            if (l_CategoryIndex >= 0)
                            {
                                if (l_PerLevelFormat.Categories[l_CategoryIndex].TotalNumberOfMaps == 0)
                                {
                                    await Context.Channel.SendMessageAsync($"Sorry but the level {l_PerLevelFormat.LevelID} doesn't contain any map.");
                                    return;
                                }


                                EmbedBuilder l_Builder = new EmbedBuilder().AddField($"Level {l_PerLevelFormat.LevelID} {GetTrophyString(true, l_PerLevelFormat.Categories[l_CategoryIndex].NumberOfPass, l_PerLevelFormat.Categories[l_CategoryIndex].TotalNumberOfMaps)}",
                                    $"{l_PerLevelFormat.Categories[l_CategoryIndex].NumberOfPass}/{l_PerLevelFormat.Categories[l_CategoryIndex].TotalNumberOfMaps} ({Math.Round(l_PerLevelFormat.Categories[l_CategoryIndex].NumberOfPass / (float)l_PerLevelFormat.Categories[l_CategoryIndex].TotalNumberOfMaps * 100.0f)}%)");
                                Embed l_Embed = l_Builder.Build();
                                await Context.Channel.SendMessageAsync(null, embed: l_Embed).ConfigureAwait(false);
                                return;
                            }

                            List<string> l_AvailableCategories = new List<string>();
                            foreach (CategoryPassed l_LevelCategory in from l_Level in l_PlayerStats.Levels where l_Level.Categories != null from l_LevelCategory in l_Level.Categories let l_CategoryFindIndex = l_AvailableCategories.FindIndex(p_X => p_X == l_LevelCategory.Category) where l_CategoryFindIndex < 0 && l_Level.LevelID == 1 select l_LevelCategory) l_AvailableCategories.Add(l_LevelCategory.Category);

                            string l_Message = $":x: Sorry but there isn't any categories (stored in your stats) called {p_Category}, here is a list of all the available categories:";
                            l_Message = l_AvailableCategories.Where(p_X => p_X != null).Where(p_X => p_X != "").Aggregate(l_Message, (p_Current, p_X) => p_Current + $"\n> {p_X}");

                            if (l_Message.Length <= 1980)
                                await ReplyAsync(l_Message);
                            else
                                await ReplyAsync($"> :x: Sorry but there isn't any categories (stored in your stats) called {p_Category},\n+ there is too many categories in that level to send all of them in one message.");

                            return;
                        }

                    await Context.Channel.SendMessageAsync($"Sorry but the level {p_LevelID} doesn't exist.");
                }
            }
        }
    }
}