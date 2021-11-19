using System;
using System.Linq;
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
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
            private int PermissionLevel;

            public RequirePermissionAttribute(int PermissionLevel)
            {
                this.PermissionLevel = PermissionLevel;
            }
            
            
            public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext p_Context, CommandInfo p_Command, IServiceProvider p_Services)
            {
                if (p_Context.User is SocketGuildUser l_User)
                {
                    ulong l_RoleID = 0;
                    if (PermissionLevel >= 1)
                        l_RoleID = ConfigController.GetConfig().BotEditorRoleID;
                    else if (PermissionLevel >= 2)
                        l_RoleID = ConfigController.GetConfig().BotManagementRoleID;
                    else if (PermissionLevel == 0)
                        l_RoleID = 1;
                    
                    
                    if (l_User.Roles.Any(p_Role => p_Role.Id == l_RoleID) && l_RoleID != 0 || l_RoleID == 1)
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
                if (l_User.Roles.ToList().Find(x => x.Id == ConfigController.GetConfig().BotEditorRoleID) != null)
                    return 1;
                else if (l_User.Roles.ToList()
                    .Find(x => x.Id == ConfigController.GetConfig().BotManagementRoleID) != null)
                    return 2;
            }
            return 0;
        }
    }
}