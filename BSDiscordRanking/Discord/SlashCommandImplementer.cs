using System;
using System.Reflection;
using System.Threading.Tasks;
using BSDiscordRanking.Discord.Modules.UserModule;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace BSDiscordRanking.Discord
{
    public class SlashCommandImplementer
    {
        private DiscordSocketClient m_SocketClient;
        private Type m_Type;
        private ulong m_GuildID;

        public SlashCommandImplementer(DiscordSocketClient p_DiscordSocketClient, Type p_Type, ulong p_GuildID)
        {
            m_SocketClient = p_DiscordSocketClient;
            m_Type = p_Type;
            m_GuildID = p_GuildID;
        }

        public static async Task RunAllSlashCommand(SocketInteraction p_SocketInteraction)
        {
            MethodInfo[] l_MethodInfos = typeof(UserModule).GetMethods(BindingFlags.Static | BindingFlags.Public);
            foreach (var l_Method in l_MethodInfos)
            {
                l_Method.Invoke(null, new object[] { p_SocketInteraction });
            }
        }
        
        public async Task SlashCommandFetchAndCreation()
        {
            MemberInfo[] l_MyMembers = m_Type.GetMembers();
            foreach (var l_Member in l_MyMembers)
            {
                Object[] l_MyAttributes = l_Member.GetCustomAttributes(true);
                foreach (var l_Attribute in l_MyAttributes)
                {
                    if (l_Attribute is SetSlashCommandAttribute l_SlashCommand)
                    {
                        Console.WriteLine($"Slash command created: {l_SlashCommand.Name} - {l_SlashCommand.Description}");
                        var l_GuildCommand = new SlashCommandBuilder();
                        l_GuildCommand.WithName(l_SlashCommand.Name);
                        l_GuildCommand.WithDescription(l_SlashCommand.Description);
                        try
                        {
                            // Now that we have our builder, we can call the rest API to make our slash command.
                            await m_SocketClient.Rest.CreateGuildCommand(l_GuildCommand.Build(), m_GuildID);
                            // With global commands we dont need the guild id.
                            //await m_Client.Rest.CreateGlobalCommand(l_GlobalCommand.Build());
                        }
                        catch (ApplicationCommandException l_Exception)
                        {
                            // If our command was invalid, we should catch an ApplicationCommandException. This exception contains the path of the error as well as the error message. You can serialize the Error field in the exception to get a visual of where your error is.
                            var l_Json = JsonConvert.SerializeObject(l_Exception.Error, Formatting.Indented);

                            // You can send this error somewhere or just print it to the console, for this example we're just going to print it.
                            Console.WriteLine(l_Json);
                        }
                    }
                }
            }
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class SetSlashCommandAttribute : Attribute
    {
        public string Name { get; }
        public string Description { get; }

        public SetSlashCommandAttribute(string Name, string Description)
        {
            this.Name = Name;
            this.Description = Description;
        }
    }
}