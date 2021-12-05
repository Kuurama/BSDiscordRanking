﻿using System;
using System.Collections.Generic;
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
        public static List<int> GetUserPermLevel(ICommandContext p_Context)
        {
            List<int> l_Ints = new List<int>(){0};
            if (p_Context.User is SocketGuildUser l_User1 && l_User1.Roles.ToList().Find(p_X => p_X.Id == ConfigController.GetConfig().RankingTeamRoleID) != null)
                l_Ints.Add(1);
            if (p_Context.User is SocketGuildUser l_User2 && l_User2.Roles.ToList().Find(p_X => p_X.Id == ConfigController.GetConfig().ScoringTeamRoleID) != null)
                l_Ints.Add(2);
            if (p_Context.User is SocketGuildUser l_User3 && l_User3.Roles.ToList().Find(p_X => p_X.Id == ConfigController.GetConfig().BotAdminRoleID) != null)
                l_Ints.Add(3);
            return l_Ints;
        }

        /// <summary>
        ///     Check if user has the correct permission level. 1 = RankingTeam, 2 = ScoringTeam, 3 = Admin
        /// </summary>
        public class RequirePermissionAttribute : PreconditionAttribute
        {
            private readonly ConfigFormat m_Config = ConfigController.GetConfig();
            private readonly int m_PermissionLevel;

            public RequirePermissionAttribute(int p_PermissionLevel)
            {
                m_PermissionLevel = p_PermissionLevel;
            }


            public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext p_Context, CommandInfo p_Command, IServiceProvider p_Services)
            {
                if (p_Context.User is SocketGuildUser l_User)
                    switch (m_PermissionLevel)
                    {
                        case >= 3:
                            if (l_User.Roles.Any(p_Role => p_Role.Id == m_Config.BotAdminRoleID))
                                return Task.FromResult(PreconditionResult.FromSuccess());
                            break;
                        
                        case 2:
                            if (l_User.Roles.Any(p_Role => p_Role.Id == m_Config.ScoringTeamRoleID || p_Role.Id == m_Config.BotAdminRoleID)) /// Gives moderator the ability to use permission == 2.
                                return Task.FromResult(PreconditionResult.FromSuccess());
                            break;

                        case 1:
                            if (l_User.Roles.Any(p_Role => p_Role.Id == m_Config.RankingTeamRoleID || p_Role.Id == m_Config.BotAdminRoleID)) /// Gives moderator the ability to use permission == 1.
                                return Task.FromResult(PreconditionResult.FromSuccess());
                            break;

                        case 0:
                            /// No need.
                            break;
                    }

                return Task.FromResult(PreconditionResult.FromError(ExecuteResult.FromError(new Exception(ErrorMessage = "Incorrect user's permissions"))));
            }
        }
    }
}