using System;
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using Discord;
using Discord.Commands;

namespace BSDiscordRanking.Discord.Modules
{
    public class Commands : ModuleBase<SocketCommandContext>
    {
        [Command("scan")]
        public async Task Scan_Scores()
        {
            if (!UserController.UserExist(Context.User.Id.ToString()))
                ReplyAsync($"> :x: Sorry, you doesn't have any account linked. Please use `{BotHandler.m_Prefix}link` instead.");
            else
            {
                Player l_player = new Player(UserController.GetPlayer(Context.User.Id.ToString()));
                l_player.FetchScores();
                l_player.FetchPass();
            }
        }
        
        
        [Command("unlink")]
        public async Task UnLinkUser()
        {
            /// TODO: HANDLE UNLINK SPECIFIC USERS IF ADMIN
            if (string.IsNullOrEmpty(UserController.GetPlayer(Context.User.Id.ToString())))
            {
                ReplyAsync($"> :x: Sorry, you doesn't have any account linked. Please use `{BotHandler.m_Prefix}link` instead.");
            }
            else
            {
                UserController.RemovePlayer(Context.User.Id.ToString());
                ReplyAsync("> :white_check_mark: Your account was successfully unlinked!");
            }

        }
        
        [Command("link")]
        public async Task LinkUser(string p_scoreSaberArg)
        {
            if (string.IsNullOrEmpty(UserController.GetPlayer(Context.User.Id.ToString())) && p_scoreSaberArg.Length == 17) ///< check if id is in a correct length
            {
                /// TODO: VERIFY SCORESABER ACCOUNT
                UserController.AddPlayer(Context.User.Id.ToString(), p_scoreSaberArg);
                await ReplyAsync($"> :white_check_mark: Your account has been successfully linked.\nLittle tip: use `{BotHandler.m_Prefix}scan` to scan your latest pass!");
            }
            else if(p_scoreSaberArg.Length != 17)
                await ReplyAsync("> :x: Sorry, but please enter an correct scoresaber id."); ///< TODO: HANDLE SCORESABER LINKS
            else
                await ReplyAsync($"> :x: Sorry, but your account already has been linked. Please use `{BotHandler.m_Prefix}unlink`.");
        }
        
        
        
        [Command("reset-config")]
        [RequireOwner]
        public async Task Reset_config()
        {
            await ReplyAsync("> :white_check_mark: After the bot finished to reset the config, it will stops.");
            ConfigController.CreateConfig();
        }

        [Command("help")]
        public async Task Help()
        {
            EmbedBuilder l_Builder = new EmbedBuilder();
            l_Builder.WithTitle("User Commands");
            l_Builder.AddField(BotHandler.m_Prefix + "help", "This message.", true);
            l_Builder.AddField(BotHandler.m_Prefix + "link **[id]**", "Links your ScoreSaber account.", true);
            l_Builder.AddField(BotHandler.m_Prefix + "unlink", "Unlinks your ScoreSaber account", true);
            l_Builder.AddField(BotHandler.m_Prefix + "ggp *[level]*", "Shows you the maps of your level.", true);
            l_Builder.AddField(BotHandler.m_Prefix + "scan", "Scans all your latest scores.", true);
            l_Builder.AddField(BotHandler.m_Prefix + "", "do something", true);
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