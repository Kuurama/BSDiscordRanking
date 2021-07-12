using System;
using System.Reflection;
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace BSDiscordRanking.Discord
{
    public class BotHandler
    {
        public static void StartBot(ConfigFormat p_config) => new BotHandler().RunBotAsync(p_config).GetAwaiter().GetResult();

        public static string m_Prefix;
        private DiscordSocketClient m_client;
        private CommandService m_commands;

        public async Task RunBotAsync(ConfigFormat p_config)
        {
            m_Prefix = p_config.CommandPrefix;
            m_client = new DiscordSocketClient();
            m_commands = new CommandService();

            await m_client.SetGameAsync(p_config.DiscordStatus);
            m_client.Log += _client_Log;
            await RegisterCommandsAsync();
            await m_client.LoginAsync(TokenType.Bot, p_config.DiscordToken);
            await m_client.StartAsync();
            await Task.Delay(-1);
        }

        private Task _client_Log(LogMessage p_arg)
        {
            Console.WriteLine(p_arg);
            return Task.CompletedTask;
        }

        public async Task RegisterCommandsAsync()
        {
            m_client.MessageReceived += HandleCommandAsync;
            await m_commands.AddModulesAsync(Assembly.GetEntryAssembly(), null);
        }

        private async Task HandleCommandAsync(SocketMessage p_arg)
        {
            var l_message = p_arg as SocketUserMessage;
            var context = new SocketCommandContext(m_client, l_message);
            if (l_message.Author.IsBot) return;
            
            int l_argPos = 0;
        
            if (l_message.HasStringPrefix(m_Prefix, ref l_argPos))
            {
                var l_result = await m_commands.ExecuteAsync(context, l_argPos, null);
                if (!l_result.IsSuccess) Console.WriteLine(l_result.ErrorReason);
                if (l_result.Error.Equals(CommandError.UnmetPrecondition)) await l_message.Channel.SendMessageAsync(l_result.ErrorReason);
            }
        }
    }
}
/// Most of BotHandler() is from @Inspectiver