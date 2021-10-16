using System.Collections.Generic;

namespace BSDiscordRanking.Formats
{
    public class SnipeFormat
    {
        public Sniped Player { get; set; }
        public List<Sniped> SnipedByPlayers { get; set; }
    }
    public class Sniped
    {
        public string Name { get; set; }
        public string ScoreSaberID { get; set; }
        public string DiscordID { get; set; }
        public int OldRank { get; set; }
        public int NewRank { get; set; }
        public bool IsPingAllowed { get; set; }
    }
}