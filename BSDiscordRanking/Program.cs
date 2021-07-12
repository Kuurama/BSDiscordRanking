using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BSDiscordRanking.Controllers;

namespace BSDiscordRanking
{
    static class Program
    {
        private static void Main(string[] p_Args)
        {
            Discord.BotHandler.StartBot(ConfigController.ReadConfig());
            
            /// New stuff :
            /// Fetch all levels in the Level's folder and put them into a cache file named LevelController.json (LevelID of the levels : {"LevelID":[12,1,2,4]}) 
            new LevelController().FetchLevel();
            
            Player l_Player = new Player("76561198410694791");
            l_Player.FetchScores();
            l_Player.FetchPass();

            /// Old stuff (code commented, List of level was useless):
            /// Fetch all levels in the Levels's folder and put them in a list
            ///List<Level> l_levels = Controllers.LevelController.FetchLevels();

            
            
            /// Level l_Level = new Level(1);
            /// l_Level.AddMap("E6E02417E730AD6408FBE6363E99EFD462083070", "Standard", "ExpertPlus");
            /// l_Level.AddMap("41D7C7B621D397DB0723B55F75AB2EF6BE1891E8", "Standard", "Expert");

            /// Command name: !addmap(myHash, Standard, MyDiff)
            /// Create a Level instance and add a map and it's infos to the level's playlist
            ///l_levels[0].AddMap("41D7C7B621D397DB0723B55F75AB2EF6BE1891E8", "Standard", "ExpertPlus");
           
            
            /// Command name: !checkscore
            /// Create a Player instance and fetch his scores.
            
            
            /// Debug Samples:
            /// new Player("76561198064857768").FetchScores(); ///< (wont rate limit, really quick)
            /// new Player("76561198410694791").FetchScores(); ///< (wont rate limit, >50 pages)
            /// new Player("76561198025265829").FetchScores(); ///< (will rate limit once, have score saber badges)
            /// new Player("76561198126131670").FetchScores(); ///< (will rate limit two/three time)
            
            
        }
    }
}
