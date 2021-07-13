using System;
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using Discord;
using Discord.Commands;

namespace BSDiscordRanking.Discord.Modules
{
    public class Commands : ModuleBase<SocketCommandContext>
    {

        [Command("unlink")]
        public async Task UnLinkUser()
        {
            /// TODO: HANDLE UNLINK SPECIFIC USERS IF ADMIN
            if (string.IsNullOrEmpty(UserController.GetPlayer(Context.User.Id.ToString())))
            {
                ReplyAsync($"Sorry, you doesn't have any account linked. Please use **{BotHandler.m_Prefix}link** instead.");
            }
            else
            {
                UserController.RemovePlayer(Context.User.Id.ToString());
                ReplyAsync("Your account was successfully unlinked!");
            }

        }
        
        [Command("link")]
        public async Task LinkUser(string p_scoreSaberArg)
        {
            if (string.IsNullOrEmpty(UserController.GetPlayer(Context.User.Id.ToString())) && p_scoreSaberArg.Length == 17) ///< check if id is in a correct length
            {
                /// TODO: VERIFY SCORESABER ACCOUNT
                UserController.AddPlayer(Context.User.Id.ToString(), p_scoreSaberArg);
                await ReplyAsync("Your account has been successfully linked.");
            }
            else if(p_scoreSaberArg.Length != 17)
                await ReplyAsync("Sorry, but please enter an correct scoresaber id."); ///< TODO: HANDLE SCORESABER LINKS
            else
                await ReplyAsync($"Sorry, but your account already has been linked. Please use **{BotHandler.m_Prefix}unlink**.");
        }
        
        
        
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
            l_Builder.AddField(BotHandler.m_Prefix + "link **[id]**", "Links your ScoreSaber account", true);
            l_Builder.AddField(BotHandler.m_Prefix + "ggp *[level]*", "Shows you the maps of your level", true);
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