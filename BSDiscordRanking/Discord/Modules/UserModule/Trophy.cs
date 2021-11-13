using System;
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
        [Summary("Shows your trophy on a Level.")]
        public async Task ShowTrophy(string p_LevelID = null)
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

                            var l_Builder = new EmbedBuilder().AddField($"Level {l_PerLevelFormat.LevelID} {l_PerLevelFormat.TrophyString}",
                                $"{l_PerLevelFormat.NumberOfPass}/{l_PerLevelFormat.NumberOfMapDiffInLevel} ({Math.Round((float)(l_PerLevelFormat.NumberOfPass / (float)l_PerLevelFormat.NumberOfMapDiffInLevel) * 100.0f)}%)");
                            var l_Embed = l_Builder.Build();
                            await Context.Channel.SendMessageAsync(null, embed: l_Embed).ConfigureAwait(false);
                            return;
                        }
                    }

                    await Context.Channel.SendMessageAsync($"Sorry but the level {p_LevelID} doesn't exist.");
                }
            }
        }
    }
}