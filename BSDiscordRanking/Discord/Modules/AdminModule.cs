using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BSDiscordRanking.Controllers;
using BSDiscordRanking.Formats;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace BSDiscordRanking.Discord.Modules.AdminModule
{
    [RequireManagerRole]
    public partial class AdminModule : ModuleBase<SocketCommandContext>
    {
        private int ScoreFromAcc(float p_Acc = 0f, int p_NoteCount = 0)
        {
            /// Made by MoreOwO :3

            /// Calculate maxScore

            int l_MaxScore;

            switch (p_NoteCount)
            {
                case <= 0:
                    return 0;
                case 1:
                    l_MaxScore = 115;
                    break;
                case <= 5:
                    l_MaxScore = 115 + ((p_NoteCount - 1) * 2 * 115);
                    break;

                case <13:
                    l_MaxScore = 1035 + ((p_NoteCount - 5) * 4 * 115);
                    break;
                case 13:
                    l_MaxScore = 4715;
                    break;
                case > 13:
                    l_MaxScore = (p_NoteCount * 8 * 115) - 7245;
                    break;
            }

            if ((int)p_Acc == 0)
                return 0;

            return (int)Math.Round(l_MaxScore * (p_Acc / 100));
        }

        private class RequireManagerRoleAttribute : PreconditionAttribute
        {
            public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext p_Context, CommandInfo p_Command, IServiceProvider p_Services)
            {
                if (p_Context.User is SocketGuildUser l_User)
                {
                    ulong l_RoleID = ConfigController.GetConfig().BotManagementRoleID;
                    if (l_User.Roles.Any(p_Role => p_Role.Id == l_RoleID))
                    {
                        return Task.FromResult(PreconditionResult.FromSuccess());
                    }
                }

                return Task.FromResult(PreconditionResult.FromError(ExecuteResult.FromError(new Exception(ErrorMessage = "Incorrect user's permissions"))));
            }
        }
    }
}