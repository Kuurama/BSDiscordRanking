using BSDiscordRanking.Controllers;

namespace BSDiscordRanking
{
    internal static class Program
    {
        private static void Main(string[] p_Args)
        {
            new LevelController().FetchLevel();
            UserController.ReadDB();
            Discord.BotHandler.StartBot(ConfigController.ReadConfig());
            
            ///Level l_Level = new Level(5);
            ///l_Level.AddMap("2","Standard","ExpertPlus", null);
            /// New stuff :
            /// Fetch all levels in the Level's folder and put them into a cache file named LevelController.json (LevelID of the levels : {"LevelID":[12,1,2,4]}) 


            //Player l_Player = new Player("76561198126131670"); /// Use Kuurama "76561198126131670" for complete level 1 wdg pass with multiple diff
            //l_Player.FetchScores();
            //l_Player.FetchPass();

            /// Old stuff (code commented, List of level was useless):
            /// Fetch all levels in the Levels's folder and put them in a list
            ///List<Level> l_levels = Controllers.LevelController.FetchLevels();


            /*
            Level l_Level2 = new Level(2);
            l_Level2.AddMap("864404d0950a796938d7f13c3e970619ee228519", "Standard", "ExpertPlus"); /// Clarence ExpertPlus not in level1
            */

            /* /// Use that to make the first wdg playlist
            /// WDG Level 1 :
            Level l_Level = new Level(1);
            l_Level.AddMap("67dde01baec8713f6524d67a8e8558f05afe2e91", "Standard", "Easy");
            l_Level.AddMap("ee9060ca33165c46d0b2ef6d4de6b12ab6c77516", "Standard", "ExpertPlus");
            l_Level.AddMap("c4ccc41a43bb15f252b025f03bce6f9c1dbbdbeb", "Standard", "ExpertPlus");
            l_Level.AddMap("1383fc623a8ba6c8e15542698c7a68185e5ed9a2", "Standard", "ExpertPlus");
            l_Level.AddMap("29283c0f425a5faebc6eb5567c3476667df71813", "Standard", "ExpertPlus");
            l_Level.AddMap("ea26ad523ba11a136db9f9a4627efe594bb71cb1", "Standard", "ExpertPlus");
            l_Level.AddMap("864404d0950a796938d7f13c3e970619ee228519", "Standard", "Expert");
            /// l_Level.AddMap("864404d0950a796938d7f13c3e970619ee228519", "Standard", "ExpertPlus"); /// not level 1, Clarence ex+ for debug
            l_Level.AddMap("d82cf0604ee1dbc3b5c62df05f7c73422140ebf9", "Standard", "ExpertPlus");
            l_Level.AddMap("a705b90abf8cbaa65fc00a0d8b98fe0da6320878", "Standard", "Expert");
            l_Level.AddMap("196c6a407903f29535a1827209f0118a345f8886", "Standard", "Normal");
            */

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