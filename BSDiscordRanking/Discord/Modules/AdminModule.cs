using System;
using Discord.Commands;

// ReSharper disable once CheckNamespace
namespace BSDiscordRanking.Discord.Modules.AdminModule
{
    [PermissionHandler.RequirePermissionAttribute(PERMISSION)]
    public partial class AdminModule : ModuleBase<SocketCommandContext>
    {
        private const int PERMISSION = 3;

        public static int ScoreFromAcc(float p_Acc = 0f, int p_NoteCount = 0)
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
                    l_MaxScore = 115 + (p_NoteCount - 1) * 2 * 115;
                    break;

                case < 13:
                    l_MaxScore = 1035 + (p_NoteCount - 5) * 4 * 115;
                    break;
                case 13:
                    l_MaxScore = 4715;
                    break;
                case > 13:
                    l_MaxScore = p_NoteCount * 8 * 115 - 7245;
                    break;
            }

            if (p_Acc == 0f)
                return 0;

            return (int)Math.Round(l_MaxScore * (p_Acc / 100));
        }
    }
}