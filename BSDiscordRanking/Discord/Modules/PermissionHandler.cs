using System;
using System.Linq;
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using BSDiscordRanking.Formats.Controller;
using Discord.Commands;
using Discord.WebSocket;

namespace BSDiscordRanking.Discord.Modules
{
    public static class PermissionHandler
    {
        /// <summary>
        ///  Check if user has the correct permission level. 1 = Editor, 2 = Admin
        /// </summary>
        public class RequirePermissionAttribute : PreconditionAttribute
        {
            private int m_PermissionLevel;
            private ConfigFormat m_Config = ConfigController.GetConfig();
            public RequirePermissionAttribute(int p_PermissionLevel)
            {
                this.m_PermissionLevel = p_PermissionLevel;
            }
            
            
            public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext p_Context, CommandInfo p_Command, IServiceProvider p_Services)
            {
                if (p_Context.User is SocketGuildUser l_User)
                {
                    ulong l_RoleID = 0;
                    switch (m_PermissionLevel)
                    {
                        case >= 2:
                            l_RoleID = m_Config.BotManagementRoleID;
                            break;
                        case >= 1:
                            l_RoleID = m_Config.BotEditorRoleID;
                            break;
                        case 0:
                            l_RoleID = 1;
                            break;
                    }
                    
                    
                    if (l_User.Roles.Any(p_Role => p_Role.Id == l_RoleID || p_Role.Id == m_Config.BotManagementRoleID) && l_RoleID != 0 || l_RoleID == 1) /// Gives moderator the ability to use permission < 2.
                    {
                        return Task.FromResult(PreconditionResult.FromSuccess());
                    }
                }

                return Task.FromResult(PreconditionResult.FromError(ExecuteResult.FromError(new Exception(ErrorMessage = "Incorrect user's permissions"))));
            }
            
        }
        public static int GetUserPermLevel(ICommandContext p_Context)
        {
            if (p_Context.User is SocketGuildUser l_User)
            {
                if (l_User.Roles.ToList().Find(p_X => p_X.Id == ConfigController.GetConfig().BotEditorRoleID) != null)
                    return 1;
                else if (l_User.Roles.ToList()
                    .Find(p_X => p_X.Id == ConfigController.GetConfig().BotManagementRoleID) != null)
                    return 2;
            }
            return 0;
        }
    }
}