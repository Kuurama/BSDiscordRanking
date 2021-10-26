using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using Discord;
using Discord.Commands;
using static System.String;

namespace BSDiscordRanking.Discord.Modules.UserModule
{
    [CheckChannel]
    public partial class UserModule : ModuleBase<SocketCommandContext>
    {
        [Command("getstarted")]
        [Summary("Displays informations about how you could get started using the bot.")]
        public async Task GetStarted()
        {
            var l_Builder = new EmbedBuilder()
                .WithTitle("How to get started with the ranking bot? :thinking:")
                .WithFooter("Prefix: " + Join(", ", ConfigController.GetConfig().CommandPrefix) + " | Bot made by Kuurama#3423 & Julien#1234")
                .AddField("Step 1", $"The first command you wanna use is the link command:\n```{ConfigController.GetConfig().CommandPrefix[0]}link [ScoreSaberLink]```")
                .AddField("Step 2", "Once you account is linked, (that mean the bot registered your score saber ID on the database),\n" +
                                    "You might want to scan your profile first:\n" +
                                    "> Use the scan command to start the download of your scoresaber's infos/scores and check if you already passed maps from the different map pools:\n" +
                                    $"```{ConfigController.GetConfig().CommandPrefix[0]}scan```")
                .AddField("Oh that's it?", $"> Yes, but there is much more to discover!\n\nYou can try the help command to find new command to try!\n```{ConfigController.GetConfig().CommandPrefix[0]}help```")
                .AddField("How to see the map pools?", $"To see the map pool you are at:\n```{ConfigController.GetConfig().CommandPrefix[0]}ggp``` \nor by adding a pool number:\n```{ConfigController.GetConfig().CommandPrefix[0]}ggp [PoolNumber]```To see a specific pool.", true)
                .AddField("How do i get the maps?",
                    $"To get a specific playlist's pool:\n```{ConfigController.GetConfig().CommandPrefix[0]}gpl [MapPoolNumber]```\n(stands for getplaylist) or even:```{ConfigController.GetConfig().CommandPrefix[0]}gpl all``` to get all the playlist pools! The playlist you get are always up to date.",
                    true)
                .AddField("Can i get playlist with only unpassed maps?",
                    $"Yes you can! To get them, *do the* `{ConfigController.GetConfig().CommandPrefix[0]}gupl all` *command!* (stands for !getunpassedplaylist [MapPoolNumber]")
                .AddField("About the 'ranking'?",
                    $"There is a leaderboard using the `{ConfigController.GetConfig().CommandPrefix[0]}ld` command! (or use `{ConfigController.GetConfig().CommandPrefix[0]}leaderboard`)\nEach pass you do give you `{ConfigController.GetConfig().PointsName}`, those points are used to sort you on the leaderboard, the further you progress in the pools, the harder the maps are, the more points you get!")
                .AddField("To see your progress through the ranking:", $"Type `{ConfigController.GetConfig().CommandPrefix[0]}progress`")
                .AddField("How do i look at my profile?", $"```{ConfigController.GetConfig().CommandPrefix[0]}profile```");
            var l_Embed = l_Builder.Build();
            await Context.Channel.SendMessageAsync(null, embed: l_Embed).ConfigureAwait(false);
        }
    }
}