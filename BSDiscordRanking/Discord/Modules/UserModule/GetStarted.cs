using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using BSDiscordRanking.Formats.Controller;
using Discord;
using Discord.Commands;
using static System.String;

namespace BSDiscordRanking.Discord.Modules.UserModule
{
    public partial class UserModule : ModuleBase<SocketCommandContext>
    {
        [Command("getstarted")]
        [Alias("setup")]
        [Summary("Displays informations about how you could get started using the bot.")]
        public async Task GetStarted()
        {
            ConfigFormat l_Config = ConfigController.GetConfig();
            EmbedBuilder l_Builder = new EmbedBuilder()
                .WithTitle("How to get started with the ranking bot? :thinking:")
                .WithFooter("Prefix: " + Join(", ", l_Config.CommandPrefix) + " | Bot made by Kuurama#3423 & Julien#1234")
                .AddField("Step 1", $"The first command you wanna use is the link command:\n```{BotHandler.m_Prefix}link [ScoreSaberLink]```")
                .AddField("Step 2", "Once you account is linked, (that mean the bot registered your score saber ID on the database),\n" +
                                    "You might want to scan your profile first:\n" +
                                    "> Use the scan command to start the download of your scoresaber's infos/scores and check if you already passed maps from the different map pools:\n" +
                                    $"```{BotHandler.m_Prefix}scan```")
                .AddField("Oh that's it?", $"> Yes, but there is much more to discover!\n\nYou can try the help command to find new command to try!\n```{BotHandler.m_Prefix}help```")
                .AddField("How to see the map pools?", $"To see the map pool you are at:\n```{BotHandler.m_Prefix}ggp``` \nor by adding a pool number:\n```{BotHandler.m_Prefix}ggp [PoolNumber]```To see a specific pool.", true)
                .AddField("How do i get the maps?",
                    $"To get a specific playlist's pool:\n```{BotHandler.m_Prefix}gpl [MapPoolNumber]```\n(stands for getplaylist) or even:```{BotHandler.m_Prefix}gpl all``` to get all the playlist pools! The playlist you get are always up to date.",
                    true)
                .AddField("Can i get playlist with only unpassed maps?",
                    $"Yes you can! To get them, *do the* `{BotHandler.m_Prefix}gupl all` *command!* (stands for !getunpassedplaylist [MapPoolNumber]")
                .AddField("About the 'ranking'?",
                    $"There two different leaderboard using the `{BotHandler.m_Prefix}ldpass` and the `{BotHandler.m_Prefix}ldacc` command! (or use `{BotHandler.m_Prefix}leaderboard`)\nEach pass you do gives you `{l_Config.PassPointsName}` and/or `{l_Config.AccPointsName}` , those points are used to sort you on the leaderboards, the further you progress in the pools, the harder the maps are, the more points you get!")
                .AddField($"How are calculated the {l_Config.AccPointsName}?", $"Each map in the ranking have an accuracy weight which is calculated with an algorithm taking account the average of the {l_Config.MinimumNumberOfScoreForAutoWeight} first scores and the difficulty worth of the map (usually the level number), it's then multiplied by your accuracy and give you those sweet points.")
                .AddField("To see your progress through the ranking:", $"Type `{BotHandler.m_Prefix}progress`")
                .AddField("How do i look at my profile?", $"```{BotHandler.m_Prefix}profile```");
            Embed l_Embed = l_Builder.Build();
            await Context.Channel.SendMessageAsync(null, embed: l_Embed).ConfigureAwait(false);
        }
    }
}