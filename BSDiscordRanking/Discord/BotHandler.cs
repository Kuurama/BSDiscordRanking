using System;
using System.Reflection;
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using BSDiscordRanking.Formats.Controller;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace BSDiscordRanking.Discord
{
    public class BotHandler
    {
#pragma warning disable CA2211
        public static string m_Prefix;
#pragma warning restore CA2211
#pragma warning disable CA2211
        public static CommandService m_Commands;
#pragma warning restore CA2211
        // ReSharper disable once MemberCanBePrivate.Global
        public DiscordSocketClient m_Client;
        public static void StartBot(ConfigFormat p_Config) => new BotHandler().RunBotAsync(p_Config).GetAwaiter().GetResult();

        private async Task RunBotAsync(ConfigFormat p_Config)
        {
            m_Prefix = p_Config.CommandPrefix[0];
            m_Client = new DiscordSocketClient(new DiscordSocketConfig {GatewayIntents = GatewayIntents.All});
            m_Commands = new CommandService();

            await m_Client.SetGameAsync(p_Config.DiscordStatus);
            m_Client.Log += _client_Log;
            await RegisterCommandsAsync();
            await m_Client.LoginAsync(TokenType.Bot, p_Config.DiscordToken);
            await m_Client.StartAsync();
            await Task.Delay(-1);
        }

        private static Task _client_Log(LogMessage p_Arg)
        {
            Console.WriteLine(p_Arg);
            return Task.CompletedTask;
        }

        public async Task RegisterCommandsAsync()
        {
            m_Client.MessageReceived += HandleCommandAsync;
            await m_Commands.AddModulesAsync(Assembly.GetEntryAssembly(), null);
        }

        private async Task HandleCommandAsync(SocketMessage p_Arg)
        {
            var l_Message = p_Arg as SocketUserMessage;
            var l_Context = new SocketCommandContext(m_Client, l_Message);
            if (l_Message != null && l_Message.Author.IsBot) return;

            int l_ArgPos = 0;

            foreach (var l_Prefix in ConfigController.GetConfig().CommandPrefix)
            {
                if (l_Message.HasStringPrefix(l_Prefix, ref l_ArgPos))
                {
                    var l_Result = m_Commands.ExecuteAsync(l_Context, l_ArgPos, null);
                    if (!l_Result.Result.IsSuccess) Console.WriteLine(l_Result.Result.ErrorReason);
                    if (l_Result.Result.Error.Equals(CommandError.UnmetPrecondition))
                        if (l_Message != null)
                            await l_Message.Channel.SendMessageAsync(l_Result.Result.ErrorReason);
                }
            }
        }
    }
}
/// Most of BotHandler() is from @Inspectiver