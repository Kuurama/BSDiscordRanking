namespace BSDiscordRanking.Formats
{
    public class UserFormat
    {
        public string DiscordID { get; set; }
        public string ScoreSaberID { get; set; }
        public string PendingScoreSaberID { get; set; }
        public ELinkState LinkState { get; set; } = ELinkState.None;
    }

    public enum ELinkState
    {
        None,
        Unverified,
        Verified,
        Banned
    }
}
