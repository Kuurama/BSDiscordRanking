using System.Collections.Generic;

namespace BSDiscordRanking.Formats
{
    public class BeatSaverFormat
    {
        public string id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public Uploader uploader { get; set; }
        public Metadata metadata { get; set; }
        public Stats stats { get; set; }
        public string uploaded { get; set; }
        public bool automapper { get; set; }
        public bool ranked { get; set; }
        public bool qualified { get; set; }
        public List<Versions> versions { get; set; }
    }

    public class Metadata
    {
        public float bpm { get; set; }
        public int duration { get; set; }
        public string songName { get; set; }
        public string songSubName { get; set; }
        public string levelAuthorName { get; set; }
        public string songAuthorName { get; set; }
    }

    public class Stats
    {
        public int plays { get; set; }
        public int downloads { get; set; }
        public int upvotes { get; set; }
        public int downvotes { get; set; }
        public float score { get; set; }
    }

    public class Uploader
    {
        public int id { get; set; }
        public string name { get; set; }
        public string hash { get; set; }
        public string avatar { get; set; }
    }

    public class Versions
    {
        public string hash { get; set; }
        public string key { get; set; }
        public string state { get; set; }
        public string createdAt { get; set; }
        public int sageScore { get; set; }
        public List<Diffs> diffs { get; set; }
        public string downloadURL { get; set; }
        public string coverURL { get; set; }
        public string previewURL { get; set; }
    }

    public class Diffs
    {
        public float njs { get; set; }
        public float offset { get; set; }
        public int notes { get; set; }
        public int bombs { get; set; }
        public int obstacles { get; set; }
        public float nps { get; set; }
        public float length { get; set; }
        public string characteristic { get; set; }
        public string difficulty { get; set; }
        public int events { get; set; }
        public bool chroma { get; set; }
        public bool me { get; set; }
        public bool ne { get; set; }
        public bool cinema { get; set; }
        public float seconds { get; set; }
        public ParitySummary paritySummary { get; set; }
    }

    public class ParitySummary
    {
        public int errors { get; set; }
        public int warns { get; set; }
        public int resets { get; set; }
    }
}