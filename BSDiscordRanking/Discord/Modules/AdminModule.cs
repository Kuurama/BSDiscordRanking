using System;
using System.Linq;
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;

namespace BSDiscordRanking.Discord.Modules
{
    [RequireManagerRole]
    public class AdminModule : ModuleBase<SocketCommandContext>
    {
        [Command("createroles")]
        public async Task CreateRoles()
        {
            new RoleController().CreateAllRoles(Context, false);
        }
        
        [Command("addmap")]
        public async Task AddMap(int p_Level = 0, string p_Code = "", string p_Characteristic = "",
            string p_DifficultyName = "")
        {
            if (p_Level <= 0 || string.IsNullOrEmpty(p_Code) || string.IsNullOrEmpty(p_Characteristic) ||
                string.IsNullOrEmpty(p_DifficultyName))
            {
                await ReplyAsync(
                    $"> :x: Seems like you didn't used the command correctly, use: `{BotHandler.m_Prefix}addmap [level] [key] [Standard/Lawless..] [ExpertPlus/Hard..] `");
            }
            else
            {
                if (p_Characteristic is "Lawless" or "Standard" or "90Degree" or "360Degree")
                {
                    if (p_DifficultyName is "Easy" or "Normal" or "Hard" or "Expert" or "ExpertPlus")
                    {
                        new Level(p_Level).AddMap(p_Code, p_Characteristic, p_DifficultyName, Context);
                    }
                    else
                        await ReplyAsync(
                            "> :x: Seems like you didn't entered the difficulty name correctly. Use: \"`Easy,Normal,Hard,Expert or ExpertPlus`\"");
                }
                else
                    await ReplyAsync(
                        "> :x: Seems like you didn't entered the characteristic name correctly. Use: \"`Standard,Lawless,90Degree or 360Degree`\"");
            }
        }

        [Command("reset-config")]
        public async Task Reset_config()
        {
            await ReplyAsync("> :white_check_mark: After the bot finished to reset the config, it will stops.");
            ConfigController.CreateConfig();
        }
        
        private class RequireManagerRoleAttribute : PreconditionAttribute
        {
            public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext p_Context, CommandInfo p_Command, IServiceProvider p_Services)
            {
                if (p_Context.User is SocketGuildUser l_User)
                {
                    if (l_User.Roles.Any(p_Role => p_Role.Id == Controllers.ConfigController.ReadConfig().BotManagementRoleID))
                    {
                        return Task.FromResult(PreconditionResult.FromSuccess());
                    }
                }
                return Task.FromResult(PreconditionResult.FromError("> :x: Sorry, you don't have the permission to access admin commands."));
            }
        }
    }
}