using System;
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using Discord.Commands;

namespace BSDiscordRanking.Discord.Modules
{
    public class Commands : ModuleBase<SocketCommandContext>
    {
        [Command("reset-config")]
        [RequireOwner]
        public async Task reset_config()
        {
            await ReplyAsync("After the bot finished to reset the config, it will stops.");
            ConfigController.CreateConfig();
        }
    }
}