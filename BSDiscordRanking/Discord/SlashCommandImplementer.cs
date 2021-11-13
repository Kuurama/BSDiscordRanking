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

                        foreach (var l_MyOthersAttribute in l_MyAttributes) /// Will implement every Requirement Attribute found.
                        {
                            switch (l_MyOthersAttribute)
                            {
                                case SetBooleanRequirementAttribute l_SetBooleanRequirement:
                                    l_GuildCommand.AddOption(l_SetBooleanRequirement.Name, ApplicationCommandOptionType.Boolean, l_SetBooleanRequirement.Description,l_SetBooleanRequirement.Required);
                                    break;
                                
                                case SetChannelRequirementAttribute l_SetChannelRequirement:
                                    l_GuildCommand.AddOption(l_SetChannelRequirement.Name, ApplicationCommandOptionType.Channel, l_SetChannelRequirement.Description, l_SetChannelRequirement.Required);
                                    break;
                                    
                                case SetIntegerRequirementAttribute l_SetIntegerRequirement:
                                    l_GuildCommand.AddOption(l_SetIntegerRequirement.Name, ApplicationCommandOptionType.Boolean, l_SetIntegerRequirement.Description, l_SetIntegerRequirement.Required);
                                    break;
                                
                                case SetMentionableRequirementAttribute l_SetMentionableRequirement:
                                    l_GuildCommand.AddOption(l_SetMentionableRequirement.Name, ApplicationCommandOptionType.Mentionable, l_SetMentionableRequirement.Description, l_SetMentionableRequirement.Required);
                                    break;
                                
                                case SetNumberRequirementAttribute l_SetNumberRequirement:
                                    l_GuildCommand.AddOption(l_SetNumberRequirement.Name, ApplicationCommandOptionType.Number, l_SetNumberRequirement.Description, l_SetNumberRequirement.Required);
                                    break;
                                
                                case SetRoleRequirementAttribute l_SetRoleRequirement:
                                    l_GuildCommand.AddOption(l_SetRoleRequirement.Name, ApplicationCommandOptionType.Role, l_SetRoleRequirement.Description, l_SetRoleRequirement.Required);
                                    break;
                                
                                case SetStringRequirementAttribute l_SetStringRequirement:
                                    l_GuildCommand.AddOption(l_SetStringRequirement.Name, ApplicationCommandOptionType.String, l_SetStringRequirement.Description, l_SetStringRequirement.Required);
                                    break;
                                
                                case SetSubCommandRequirementAttribute l_SetSubCommandRequirement:
                                    l_GuildCommand.AddOption(l_SetSubCommandRequirement.Name, ApplicationCommandOptionType.SubCommand, l_SetSubCommandRequirement.Description, l_SetSubCommandRequirement.Required);
                                    break;
                                
                                case SetSubCommandGroupRequirementAttribute l_SetSubCommandGroupRequirement:
                                    l_GuildCommand.AddOption(l_SetSubCommandGroupRequirement.Name, ApplicationCommandOptionType.Mentionable, l_SetSubCommandGroupRequirement.Description, l_SetSubCommandGroupRequirement.Required);
                                    break;
                                
                                case SetUserRequirementAttribute l_SetUserRequirement:
                                    l_GuildCommand.AddOption(l_SetUserRequirement.Name, ApplicationCommandOptionType.User, l_SetUserRequirement.Description, l_SetUserRequirement.Required);
                                    break;
                            }
                        }
                        
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

                        break;
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

    [AttributeUsage(AttributeTargets.Method)]
    public class SetBooleanRequirementAttribute : Attribute
    {
        public string Name { get; }
        public string Description { get; }
        public bool Required { get; }

        public SetBooleanRequirementAttribute(string Name, string Description, bool Required)
        {
            this.Name = Name;
            this.Description = Description;
            this.Required = Required;
        }
    }
    
    [AttributeUsage(AttributeTargets.Method)]
    public class SetChannelRequirementAttribute : Attribute
    {
        public string Name { get; }
        public string Description { get; }
        public bool Required { get; }

        public SetChannelRequirementAttribute(string Name, string Description, bool Required)
        {
            this.Name = Name;
            this.Description = Description;
            this.Required = Required;
        }
    }
    
    [AttributeUsage(AttributeTargets.Method)]
    public class SetIntegerRequirementAttribute : Attribute
    {
        public string Name { get; }
        public string Description { get; }
        public bool Required { get; }

        public SetIntegerRequirementAttribute(string Name, string Description, bool Required)
        {
            this.Name = Name;
            this.Description = Description;
            this.Required = Required;
        }
    }
    
    [AttributeUsage(AttributeTargets.Method)]
    public class SetMentionableRequirementAttribute : Attribute
    {
        public string Name { get; }
        public string Description { get; }
        public bool Required { get; }

        public SetMentionableRequirementAttribute(string Name, string Description, bool Required)
        {
            this.Name = Name;
            this.Description = Description;
            this.Required = Required;
        }
    }
    
    [AttributeUsage(AttributeTargets.Method)]
    public class SetNumberRequirementAttribute : Attribute
    {
        public string Name { get; }
        public string Description { get; }
        public bool Required { get; }

        public SetNumberRequirementAttribute(string Name, string Description, bool Required)
        {
            this.Name = Name;
            this.Description = Description;
            this.Required = Required;
        }
    }
    
    [AttributeUsage(AttributeTargets.Method)]
    public class SetRoleRequirementAttribute : Attribute
    {
        public string Name { get; }
        public string Description { get; }
        public bool Required { get; }

        public SetRoleRequirementAttribute(string Name, string Description, bool Required)
        {
            this.Name = Name;
            this.Description = Description;
            this.Required = Required;
        }
    }
    
    [AttributeUsage(AttributeTargets.Method)]
    public class SetStringRequirementAttribute : Attribute
    {
        public string Name { get; }
        public string Description { get; }
        public bool Required { get; }

        public SetStringRequirementAttribute(string Name, string Description, bool Required)
        {
            this.Name = Name;
            this.Description = Description;
            this.Required = Required;
        }
    }
    
    [AttributeUsage(AttributeTargets.Method)]
    public class SetSubCommandRequirementAttribute : Attribute
    {
        public string Name { get; }
        public string Description { get; }
        public bool Required { get; }

        public SetSubCommandRequirementAttribute(string Name, string Description, bool Required)
        {
            this.Name = Name;
            this.Description = Description;
            this.Required = Required;
        }
    }
    
    [AttributeUsage(AttributeTargets.Method)]
    public class SetSubCommandGroupRequirementAttribute : Attribute
    {
        public string Name { get; }
        public string Description { get; }
        public bool Required { get; }

        public SetSubCommandGroupRequirementAttribute(string Name, string Description, bool Required)
        {
            this.Name = Name;
            this.Description = Description;
            this.Required = Required;
        }
    }
    
    [AttributeUsage(AttributeTargets.Method)]
    public class SetUserRequirementAttribute : Attribute
    {
        public string Name { get; }
        public string Description { get; }
        public bool Required { get; }

        public SetUserRequirementAttribute(string Name, string Description, bool Required)
        {
            this.Name = Name;
            this.Description = Description;
            this.Required = Required;
        }
    }
}