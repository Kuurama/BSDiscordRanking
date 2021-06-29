using System;
using System.IO;
using System.Net;
using System.Text.Json;

namespace BSLevelUpDiscordSystem1._2
{
    public class Player
    {
        private apiPlayerFull m_PlayerFull;
        private apiPlayerScore m_PlayerScore;

        public Player(string p_PlayerID)
        {
            var l_Path = @$".\Player\{p_PlayerID}\";
            Console.WriteLine(l_Path);
            GetInfos(this, p_PlayerID); /// Get Full Player Info.
            CreateDirectoryAndFile(this, l_Path); /// Make the score file if it don't exist.
            OpenSavedScore(this, l_Path); /// Null if no score.
            CheckForScore(this, 12);
            ReWriteScore(this, l_Path);
        }

        /// get the Player Info
        private void GetInfos(Player p_Player, string p_PlayerID)
        {
            using (WebClient l_WebClient = new WebClient())
            {
                p_Player.m_PlayerFull = JsonSerializer.Deserialize<apiPlayerFull>(
                    l_WebClient.DownloadString(@$"https://new.scoresaber.com/api/player/{p_PlayerID}/full"));
            }
        }

        /// Create The directory And File if missing
        private void CreateDirectoryAndFile(Player p_Player, string p_Path)
        {
            if (!Directory.Exists(p_Path))
                Directory.CreateDirectory(p_Path ?? throw new InvalidOperationException());
            if (!File.Exists(p_Path + @"\score.json"))
            {
                File.WriteAllText(p_Path + @"\score.json",
                    @"{
    ""scores"": [
        {
            ""rank"": 0,
            ""scoreId"": 0,
            ""score"": 0,
            ""unmodifiedScore"": 0,
            ""mods"": """",
            ""pp"": 0.0,
            ""weight"": 0.0,
            ""timeSet"": """",
            ""leaderboardId"": 0,
            ""songHash"": """",
            ""songName"": """",
            ""songSubName"": """",
            ""songAuthorName"": """",
            ""levelAuthorName"": """",
            ""difficulty"": 0.0,
            ""difficultyRaw"": """",
            ""maxScore"": 0
        }
    ]
}
    ");
            }
        }

        /// Open Saved Score
        private void OpenSavedScore(Player p_Player, string p_Path)
        {
            using (StreamReader l_SR = new StreamReader(p_Path + @"\score.json"))
            {
                p_Player.m_PlayerScore = JsonSerializer.Deserialize<apiPlayerScore>(l_SR.ReadToEnd());
            }
        }

        /// Give a selected page from the player's recent score.
        private apiPlayerScore GetLatestScorePage(Player p_Player, int p_Page)
        {
            using (WebClient l_WebClient = new WebClient())
            {
                return JsonSerializer.Deserialize<apiPlayerScore>(l_WebClient.DownloadString(
                    @$"https://new.scoresaber.com/api/player/{p_Player.m_PlayerFull.playerInfo.playerId}/scores/recent/{p_Page.ToString()}"));
            }
        }

        /// Check a number of the player's recent score and add them to the data if they don't exist.
        private void CheckForScore(Player p_Player, int p_NumberOfRequest)
        {
            bool l_Skip = false;
            int l_Index = 0;
            int l_Counter = 1;
            apiPlayerScore l_TempScore;
            while (!l_Skip && l_Counter <= p_NumberOfRequest &&
                   (float) p_Player.m_PlayerFull.scoreStats.totalPlayCount / 8.0 >
                   (float) l_Counter) //// Don't scan last page of the account to avoid any error.
            {
                l_TempScore = GetLatestScorePage(p_Player, l_Counter);

                for (int i = 0; i < p_Player.m_PlayerScore.scores.Count; i++)
                {
                    if (l_Skip)
                        break;
                    for (l_Index = 0; l_Index < l_TempScore.scores.Count; l_Index++)
                    {
                        if (l_TempScore.scores[l_Index].timeSet == p_Player.m_PlayerScore.scores[i].timeSet)
                        {
                            l_Skip = true;
                            break;
                        }
                    }
                }

                for (int k = 0; k < l_Index; k++)
                {
                    p_Player.m_PlayerScore.scores.Add(l_TempScore.scores[k]);
                    /// Add all the new score which don't exist in the currentScore currently saved,
                    /// l_Index is the index until where the recent score exist into the saved scores
                }

                l_Counter++;
            }
        }

        /// Write the Score Data from the current Player instance.
        private void ReWriteScore(Player p_Player, string p_Path)
        {
            File.WriteAllText(p_Path + @"\score.json",
                JsonSerializer.Serialize<apiPlayerScore>(p_Player.m_PlayerScore));
        }
    }
}