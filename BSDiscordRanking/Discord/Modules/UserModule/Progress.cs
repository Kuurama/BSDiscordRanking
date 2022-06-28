using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using BSDiscordRanking.Formats.Controller;
using BSDiscordRanking.Formats.Player;
using Discord;
using Discord.Commands;

namespace BSDiscordRanking.Discord.Modules.UserModule
{
    public partial class UserModule : ModuleBase<SocketCommandContext>
    {
        [Command("progress")]
        [Summary("Shows a specific player's progress, or depending on a specific category.")]
        public async Task Progress(ulong p_DiscordOrScoreSaberID, [Remainder] string p_Category = null)
        {
            PlayerFromDiscordOrScoreSaberIDFormat l_DiscordOrScoreSaberIDFormat = PlayerFromDiscordOrScoreSaberID(p_DiscordOrScoreSaberID.ToString(), Context);
            LevelControllerFormat l_LevelControllerFormat = LevelController.GetLevelControllerCache();
            LevelController.ReWriteController(l_LevelControllerFormat);
            Player l_Player = null;
            if (!l_DiscordOrScoreSaberIDFormat.IsDiscordLinked && !l_DiscordOrScoreSaberIDFormat.IsScoreSaberAccount)
                await ReplyAsync("> :x: Sorry, This Discord account isn't linked.");
            else if (LevelController.GetLevelControllerCache().LevelID.Count <= 0!)
                await ReplyAsync($"> :x: Sorry, but please create a level. Please use `{BotHandler.m_Prefix}addmap` command first.");
            else if (l_DiscordOrScoreSaberIDFormat.IsScoreSaberAccount && l_DiscordOrScoreSaberIDFormat.IsDiscordLinked)
                p_DiscordOrScoreSaberID = ulong.Parse(UserController.GetDiscordID(p_DiscordOrScoreSaberID.ToString()));
            else if (l_DiscordOrScoreSaberIDFormat.IsScoreSaberAccount && !l_DiscordOrScoreSaberIDFormat.IsDiscordLinked) l_Player = new Player(p_DiscordOrScoreSaberID.ToString());

            // ReSharper disable once ConstantNullCoalescingCondition
            l_Player ??= new Player(UserController.GetPlayer(l_DiscordOrScoreSaberIDFormat.DiscordID));
            PlayerStatsFormat l_PlayerPassPerLevel = l_Player.GetStats();
            if (l_PlayerPassPerLevel == null)
            {
                await ReplyAsync("> Sorry but user ");
                return;
            }

            int l_MessagesIndex = 0;
            bool l_HaveAPass = false;
            List<string> l_Messages = new List<string> { "" };
            List<string> l_AvailableCategories = new List<string>();
            if (l_PlayerPassPerLevel.Levels != null)
            {
                Color l_Color = GetRoleColor(RoleController.ReadRolesDB().Roles, Context.Guild.Roles, l_Player.GetPlayerLevel());
                EmbedBuilder l_Builder = new EmbedBuilder()
                    .WithTitle($"{l_Player.m_PlayerFull.name}'s Progress Tracker")
                    .WithDescription($"Here is your current progress through the ***{p_Category ?? "map"}*** pools:")
                    .WithThumbnailUrl(l_Player.m_PlayerFull.profilePicture)
                    .WithColor(l_Color);
                foreach (PassedLevel l_PlayerStats in l_PlayerPassPerLevel.Levels)
                {
                    if (l_PlayerStats.NumberOfPass > 0) l_HaveAPass = true;
                    if (p_Category == null)
                    {
                        if (l_PlayerStats.TotalNumberOfMaps > 0)
                        {
                            if (l_Messages[l_MessagesIndex].Length +
                                $"Level {l_PlayerStats.LevelID}: {GenerateProgressBar(l_PlayerStats.NumberOfPass, l_PlayerStats.TotalNumberOfMaps, 10)} {Math.Round(l_PlayerStats.NumberOfPass / (float)l_PlayerStats.TotalNumberOfMaps * 100.0f)}% ({l_PlayerStats.NumberOfPass}/{l_PlayerStats.TotalNumberOfMaps})  {GetTrophyString(false, l_PlayerStats.NumberOfPass, l_PlayerStats.TotalNumberOfMaps)}\n"
                                    .Length > 900)
                                l_MessagesIndex++;

                            if (l_Messages.Count < l_MessagesIndex + 1) l_Messages.Add(""); /// Initialize the next used index.

                            l_Messages[l_MessagesIndex] +=
                                $"Level {l_PlayerStats.LevelID}: {GenerateProgressBar(l_PlayerStats.NumberOfPass, l_PlayerStats.TotalNumberOfMaps, 10)} {Math.Round(l_PlayerStats.NumberOfPass / (float)l_PlayerStats.TotalNumberOfMaps * 100.0f)}% ({l_PlayerStats.NumberOfPass}/{l_PlayerStats.TotalNumberOfMaps})  {GetTrophyString(false, l_PlayerStats.NumberOfPass, l_PlayerStats.TotalNumberOfMaps)}" +
                                Environment.NewLine;
                        }
                    }
                    else
                    {
                        p_Category = FirstCharacterToUpper(p_Category);
                        foreach (CategoryPassed l_Category in l_PlayerStats.Categories)
                        {
                            if (l_AvailableCategories.FindIndex(p_Y => p_Y == l_Category.Category) < 0) l_AvailableCategories.Add(l_Category.Category);
                            if (l_Category.Category == p_Category && l_Category.TotalNumberOfMaps > 0)
                            {
                                if (l_Messages[l_MessagesIndex].Length + $"Level {l_PlayerStats.LevelID}: {GenerateProgressBar(l_Category.NumberOfPass, l_Category.TotalNumberOfMaps, 10)} {Math.Round(l_Category.NumberOfPass / (float)l_Category.TotalNumberOfMaps * 100.0f)}% ({l_Category.NumberOfPass}/{l_Category.TotalNumberOfMaps})  {GetTrophyString(false, l_Category.NumberOfPass, l_Category.TotalNumberOfMaps)}\n".Length > 900) l_MessagesIndex++;

                                if (l_Messages.Count < l_MessagesIndex + 1) l_Messages.Add(""); /// Initialize the next used index.

                                l_Messages[l_MessagesIndex] += $"Level {l_PlayerStats.LevelID}: {GenerateProgressBar(l_Category.NumberOfPass, l_Category.TotalNumberOfMaps, 10)} {Math.Round(l_Category.NumberOfPass / (float)l_Category.TotalNumberOfMaps * 100.0f)}% ({l_Category.NumberOfPass}/{l_Category.TotalNumberOfMaps})  {GetTrophyString(false, l_Category.NumberOfPass, l_Category.TotalNumberOfMaps)}" + Environment.NewLine;
                            }
                        }
                    }
                }

                if (l_HaveAPass)
                    l_Builder.WithTitle($"{l_Player.m_PlayerFull.name}'s Progress Tracker");
                else if (!l_DiscordOrScoreSaberIDFormat.IsDiscordLinked) l_Builder.WithTitle($"{l_Player.m_PlayerFull.name}'s Progress Tracker (Unlinked Account)");

                foreach (string l_Message in l_Messages)
                    if (l_Message != "")
                        l_Builder.AddField("\u200B", l_Message);

                if (l_Builder.Fields.Count > 0)
                {
                    await Context.Channel.SendMessageAsync(null, embed: l_Builder.Build()).ConfigureAwait(false);
                }
                else if (l_Player.m_PlayerStats.Levels[^1].Trophy == null)
                {
                    await Context.Channel.SendMessageAsync($"> :x: Sorry but {l_Player.m_PlayerFull.name} isn't linked/scanned on the bot.");
                }
                else if (l_Builder.Fields.Count <= 0 && p_Category != null)
                {
                    string l_Message = $":x: Sorry but there isn't any categories (stored in your stats) called {p_Category}, here is a list of all the available categories:";
                    l_Message = l_AvailableCategories.Where(p_X => p_X != null).Aggregate(l_Message, (p_Current, p_Y) => p_Current + $"\n> {p_Y}");

                    if (l_Message.Length <= 1980)
                        await ReplyAsync(l_Message);
                    else
                        await ReplyAsync($"> :x: Sorry but there isn't any categories (stored in your stats) called {p_Category},\n+ there is too many categories in that level to send all of them in one message.");
                }
            }
        }

        [Command("progress")]
        [Summary("Shows your progress through the map pools, or depending on a specific category.")]
        public async Task Progress([Remainder] string p_Category = null)
        {
            if (!UserController.UserExist(Context.User.Id.ToString()))
            {
                await ReplyAsync($"> :x: Sorry, you don't have any account linked. Please use `{BotHandler.m_Prefix}link <ScoreSaber link/id>` instead.\n> (Or to get started with the bot: use the `{BotHandler.m_Prefix}getstarted` command)");
            }
            else if (LevelController.GetLevelControllerCache().LevelID.Count <= 0!)
            {
                await ReplyAsync($"> :x: Sorry, but please create a level. Please use `{BotHandler.m_Prefix}addmap` command first.");
            }
            else
            {
                Player l_Player = new Player(UserController.GetPlayer(Context.User.Id.ToString()));
                PlayerStatsFormat l_PlayerPassPerLevel = l_Player.GetStats();
                if (l_PlayerPassPerLevel == null)
                {
                    Console.WriteLine($"Player {UserController.GetPlayer(Context.User.Id.ToString())} : l_PlayerPassPerLevel is null");
                    return;
                }

                int l_MessagesIndex = 0;
                List<string> l_Messages = new List<string> { "" };
                List<string> l_AvailableCategories = new List<string>();
                if (l_PlayerPassPerLevel.Levels != null)
                {
                    Color l_Color = GetRoleColor(RoleController.ReadRolesDB().Roles, Context.Guild.Roles, l_Player.GetPlayerLevel());
                    EmbedBuilder l_Builder = new EmbedBuilder()
                        .WithTitle($"{l_Player.m_PlayerFull.name}'s Progress Tracker")
                        .WithDescription($"Here is your current progress through the ***{p_Category ?? "map"}*** pools:")
                        .WithThumbnailUrl(l_Player.m_PlayerFull.profilePicture)
                        .WithColor(l_Color);
                    foreach (PassedLevel l_PlayerStats in l_PlayerPassPerLevel.Levels)
                        if (p_Category == null)
                        {
                            if (l_PlayerStats.TotalNumberOfMaps > 0)
                            {
                                if (l_Messages[l_MessagesIndex].Length +
                                    $"Level {l_PlayerStats.LevelID}: {GenerateProgressBar(l_PlayerStats.NumberOfPass, l_PlayerStats.TotalNumberOfMaps, 10)} {Math.Round(l_PlayerStats.NumberOfPass / (float)l_PlayerStats.TotalNumberOfMaps * 100.0f)}% {GetTrophyString(false, l_PlayerStats.NumberOfPass, l_PlayerStats.TotalNumberOfMaps)} ({l_PlayerStats.NumberOfPass}/{l_PlayerStats.TotalNumberOfMaps})\n"
                                        .Length > 900)
                                    l_MessagesIndex++;

                                if (l_Messages.Count < l_MessagesIndex + 1) l_Messages.Add(""); /// Initialize the next used index.

                                l_Messages[l_MessagesIndex] +=
                                    $"Level {l_PlayerStats.LevelID}: {GenerateProgressBar(l_PlayerStats.NumberOfPass, l_PlayerStats.TotalNumberOfMaps, 10)} {Math.Round(l_PlayerStats.NumberOfPass / (float)l_PlayerStats.TotalNumberOfMaps * 100.0f)}% {GetTrophyString(false, l_PlayerStats.NumberOfPass, l_PlayerStats.TotalNumberOfMaps)} ({l_PlayerStats.NumberOfPass}/{l_PlayerStats.TotalNumberOfMaps})" +
                                    Environment.NewLine;
                            }
                        }
                        else
                        {
                            p_Category = FirstCharacterToUpper(p_Category);
                            foreach (CategoryPassed l_Category in l_PlayerStats.Categories)
                            {
                                if (l_AvailableCategories.FindIndex(p_Y => p_Y == l_Category.Category) < 0) l_AvailableCategories.Add(l_Category.Category);
                                if (l_Category.Category == p_Category && l_Category.TotalNumberOfMaps > 0)
                                {
                                    if (l_Messages[l_MessagesIndex].Length + $"Level {l_PlayerStats.LevelID}: {GenerateProgressBar(l_Category.NumberOfPass, l_Category.TotalNumberOfMaps, 10)} {Math.Round(l_Category.NumberOfPass / (float)l_Category.TotalNumberOfMaps * 100.0f)}% {GetTrophyString(false, l_Category.NumberOfPass, l_Category.TotalNumberOfMaps)} ({l_Category.NumberOfPass}/{l_Category.TotalNumberOfMaps})\n".Length > 900) l_MessagesIndex++;

                                    if (l_Messages.Count < l_MessagesIndex + 1) l_Messages.Add(""); /// Initialize the next used index.

                                    l_Messages[l_MessagesIndex] += $"Level {l_PlayerStats.LevelID}: {GenerateProgressBar(l_Category.NumberOfPass, l_Category.TotalNumberOfMaps, 10)} {Math.Round(l_Category.NumberOfPass / (float)l_Category.TotalNumberOfMaps * 100.0f)}% {GetTrophyString(false, l_Category.NumberOfPass, l_Category.TotalNumberOfMaps)} ({l_Category.NumberOfPass}/{l_Category.TotalNumberOfMaps})" + Environment.NewLine;
                                }
                            }
                        }

                    foreach (string l_Message in l_Messages)
                        if (l_Message != "")
                            l_Builder.AddField("\u200B", l_Message);

                    if (l_Builder.Fields.Count <= 0 && p_Category != null)
                    {
                        string l_Message = $":x: Sorry but there isn't any categories (stored in your stats) called {p_Category}, here is a list of all the available categories:";
                        l_Message = l_AvailableCategories.Where(p_X => p_X != null).Aggregate(l_Message, (p_Current, p_Y) => p_Current + $"\n> {p_Y}");

                        if (l_Message.Length <= 1980)
                            await ReplyAsync(l_Message);
                        else
                            await ReplyAsync($"> :x: Sorry but there isn't any categories (stored in your stats) called {p_Category},\n+ there is too many categories in that level to send all of them in one message.");
                    }
                    else if (l_Builder.Fields.Count > 0)
                    {
                        await Context.Channel.SendMessageAsync(null, embed: l_Builder.Build()).ConfigureAwait(false);
                    }
                }
            }
        }
    }
}
