namespace BSDiscordRanking
{
    static class Program
    {
        private static void Main(string[] p_Args)
        {

            /// Command name: !addmap(myHash, Standard, MyDiff)
            /// Create a Level instance and add a map and it's infos to the level's playlist
            new Level(1).AddMap("41D7C7B621D397DB0723B55F75AB2EF6BE1891E8", "Standard", "ExpertPlus");

            
            /// Command name: !checkscore
            /// Create a Player instance and fetch his scores.
            new Player("76561198064857768").FetchScores();
            
            /// Debug Samples:
            /// new Player("76561198064857768").FetchScores(); ///< (wont rate limit, really quick)
            /// new Player("76561198410694791").FetchScores(); ///< (wont rate limit, >50 pages)
            /// new Player("76561198025265829").FetchScores(); ///< (will rate limit once, have score saber badges)
            /// new Player("76561198126131670").FetchScores(); ///< (will rate limit two/three time)
        }
    }
}
