namespace BSDiscordRanking
{
    static class Program
    {
        private static void Main(string[] p_Args)
        {
            /// The bot system will create a level instance if a Level command is used (public method) :
            /// like : !addmap(myHash, Standard, MyDiff)
            Level l_Level = new Level(1);
            ////////////////////////////////////////////////////////////////////////////
            /// Then Matching this instance l_Level,
            /// the bot will execute the command the player asked (make sure the method is set to "public") matching the instance it just created,
            /// like : l_Level.AddMap(myHash, Standard, MyDiff);
            l_Level.AddMap("41D7C7B621D397DB0723B55F75AB2EF6BE1891E8", "Standard", "ExpertPlus");
            /// Then the instance will close itself after a while (you said).
            /// And the bot will, for each command an admin use (need admin role for level), quickly create a new level and execute a method from it.


            /// Quick example of player
            /// Player l_Julien = new Player("76561198410694791"); /// (wont rate limit)
            /// Player l_HardCPP = new Player("76561198025265829"); /// (will rate limit once, have score saber badges)
            /// Player l_Kuurama = new Player("76561198126131670"); /// (will rate limit two/three time)
            /// Player l_M4rie = new Player("76561198064857768"); /// will end really fast because less than 5 pages. (wont rate limit)


            /// The bot system will create a player instance is a player command is used (public method) :
            Player l_Player = new Player("76561198064857768");
            /// like : !checkscore
            /// then the bot will execute that command matching the player instance it just created.
            /// like l_Player.CheckScore();
            l_Player.CheckScore();
            /// Then the instance will close itself after a while (you said).
            /// And the bot will for each command a player use, quickly create a new player and execute a method from it (need to be registered on the bot for that, to get their playerID), .
        }
    }
}