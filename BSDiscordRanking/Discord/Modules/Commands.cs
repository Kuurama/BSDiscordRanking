using System;
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using Discord;
using Discord.Commands;

namespace BSDiscordRanking.Discord.Modules
{
    public class Commands : ModuleBase<SocketCommandContext>
    {
        [Command("reset-config")]
        [RequireOwner]
        public async Task Reset_config()
        {
            await ReplyAsync("After the bot finished to reset the config, it will stops.");
            ConfigController.CreateConfig();
        }

        [Command("help")]
        public async Task Help()
        {
            EmbedBuilder l_Builder = new EmbedBuilder();
            l_Builder.WithTitle("User Commands");
            l_Builder.AddField(BotHandler.m_Prefix + "help", "This message <:flushed:864139721067462697>", true);
            l_Builder.AddField(BotHandler.m_Prefix + "ggp *[level]*", "Shows you the maps of your level", true);
            l_Builder.AddField(BotHandler.m_Prefix + "link **[id]**", "Links your ScoreSaber account", true);
            l_Builder.AddField("oui oui", "baguette", true);
            l_Builder.AddField("ranked", "bad", true);
            l_Builder.AddField("umby", "when will multiplayer be out?", true);
            l_Builder.WithColor(Color.Blue);
            await Context.Channel.SendMessageAsync("", false, l_Builder.Build());
            
            EmbedBuilder l_ModBuilder = new EmbedBuilder();
            l_ModBuilder.WithTitle("Admins Commands");
            l_ModBuilder.AddField(BotHandler.m_Prefix + "addmap", "Add a map the the level", true);
            l_ModBuilder.AddField(BotHandler.m_Prefix + "reset-config", "Owner only: Reset the config file, **the bot will stop!**", true);
            l_ModBuilder.AddField(BotHandler.m_Prefix + "unlink **[player]**", "Unlinks the ScoreSaber account of a player", true);
            l_ModBuilder.WithColor(Color.Red);
            await Context.Channel.SendMessageAsync("", false, l_ModBuilder.Build());
        }
        
    }
}