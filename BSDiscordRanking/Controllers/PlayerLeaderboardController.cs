using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using BSDiscordRanking.Formats;
using BSDiscordRanking.Formats.Controller;
using BSDiscordRanking.Formats.Player;

namespace BSDiscordRanking.Controllers
{
    public abstract class PlayerLeaderboardController
    {
        private const string PATH = @"./Leaderboard/";
        protected string m_Filename = @"Leaderboard";
        protected string m_LeaderboardType = "sample";
        protected string m_PointName = "Points";
        private const int ERROR_LIMIT = 3;
        private int m_ErrorNumber;
        public LeaderboardControllerFormat m_Leaderboard;

        protected PlayerLeaderboardController()
        {
            LoadLeaderboard();
        }

        public SnipeFormat ManagePlayer(string p_Name, string p_ScoreSaberID, float p_Points, int p_Level, Trophy p_Trophy, bool p_PingToggle)
        {
            if (p_ScoreSaberID != null)
            {
                int l_BannedCount = 0;
                bool l_NewPlayer = true;
                SnipeFormat l_Snipe = new SnipeFormat
                {
                    Player = new Sniped(),
                    SnipedByPlayers = new List<Sniped>()
                };


                for (int l_I = 0; l_I <= m_Leaderboard.Leaderboard.Count - 1; l_I++)
                {
                    if (p_ScoreSaberID == m_Leaderboard.Leaderboard[l_I].ScoreSaberID)
                    {
                        l_NewPlayer = false;
                        if (p_Name != null) m_Leaderboard.Leaderboard[l_I].Name = p_Name;

                        if (p_Points >= 0) m_Leaderboard.Leaderboard[l_I].Points = p_Points;

                        if (p_Level >= 0) m_Leaderboard.Leaderboard[l_I].Level = p_Level;

                        if (p_Trophy != null) m_Leaderboard.Leaderboard[l_I].Trophy = p_Trophy;

                        if (p_PingToggle) m_Leaderboard.Leaderboard[l_I].IsPingAllowed = !m_Leaderboard.Leaderboard[l_I].IsPingAllowed;

                        m_Leaderboard.Leaderboard[l_I].DiscordID = UserController.GetDiscordID(m_Leaderboard.Leaderboard[l_I].ScoreSaberID);

                        /// Update Player's info ///
                        l_Snipe.Player.Name = m_Leaderboard.Leaderboard[l_I].Name;
                        l_Snipe.Player.ScoreSaberID = m_Leaderboard.Leaderboard[l_I].ScoreSaberID;
                        l_Snipe.Player.DiscordID = m_Leaderboard.Leaderboard[l_I].DiscordID;
                        l_Snipe.Player.OldRank = l_I + 1;
                        l_Snipe.Player.NewRank = l_I + 1;
                        l_Snipe.Player.IsPingAllowed = m_Leaderboard.Leaderboard[l_I].IsPingAllowed;
                        ///////////////////
                        m_Leaderboard.Leaderboard = m_Leaderboard.Leaderboard.OrderByDescending(p_X => p_X.Points).ToList();
                        break;
                    }

                    if (m_Leaderboard.Leaderboard[l_I].IsBanned) l_BannedCount++; /// Banned player before the old player rank.
                    else
                        l_Snipe.SnipedByPlayers.Add(new Sniped
                        {
                            Name = m_Leaderboard.Leaderboard[l_I].Name,
                            DiscordID = m_Leaderboard.Leaderboard[l_I].DiscordID,
                            ScoreSaberID = m_Leaderboard.Leaderboard[l_I].ScoreSaberID,
                            OldRank = l_I + 1 - l_BannedCount,
                            NewRank = l_I + 1,
                            IsPingAllowed = m_Leaderboard.Leaderboard[l_I].IsPingAllowed
                        });
                }

                if (l_NewPlayer)
                {
                    RankedPlayer l_RankedPlayer = new RankedPlayer
                    {
                        Name = p_Name,
                        ScoreSaberID = p_ScoreSaberID,
                        DiscordID = UserController.GetDiscordID(p_ScoreSaberID),
                        IsPingAllowed = false
                    };

                    if (p_Points >= 0)
                        l_RankedPlayer.Points = p_Points;
                    else
                        l_RankedPlayer.Points = 0;

                    l_RankedPlayer.Level = p_Level >= 0 ? p_Level : 0;

                    if (p_Trophy != null)
                        l_RankedPlayer.Trophy = p_Trophy;
                    else
                        l_RankedPlayer.Trophy = new Trophy
                        {
                            Plastic = 0,
                            Silver = 0,
                            Gold = 0,
                            Diamond = 0,
                            Ruby = 0
                        };

                    m_Leaderboard.Leaderboard.Add(l_RankedPlayer);
                    ReWriteLeaderboard();
                    return new SnipeFormat
                    {
                        Player = new Sniped
                        {
                            NewRank = 0,
                            OldRank = 0,
                            DiscordID = UserController.GetDiscordID(p_ScoreSaberID),
                            Name = p_Name,
                            IsPingAllowed = false,
                            ScoreSaberID = p_ScoreSaberID
                        },
                        SnipedByPlayers = new List<Sniped>()
                    };
                }

                ReWriteLeaderboard();

                l_Snipe.Player.OldRank -= l_BannedCount;

                if (m_Leaderboard.Leaderboard.Any(p_X => p_X.ScoreSaberID == p_ScoreSaberID && p_X.IsBanned))
                {
                    l_Snipe.Player.NewRank = l_Snipe.Player.OldRank; /// Make banned user from leaderboard not trigger snipe message etc.
                }
                else
                {
                    m_Leaderboard.Leaderboard.RemoveAll(p_X => p_X.IsBanned); /// Remove banned user after the rewrite just to get the player index.
                    l_Snipe.Player.NewRank = m_Leaderboard.Leaderboard.FindIndex(p_X => p_X.ScoreSaberID == l_Snipe.Player.ScoreSaberID) + 1;


                }


                for (int l_I = l_Snipe.SnipedByPlayers.Count - 1; l_I >= 0; l_I--)
                {
                    l_Snipe.SnipedByPlayers[l_I].NewRank = m_Leaderboard.Leaderboard.FindIndex(p_X => p_X.ScoreSaberID == l_Snipe.SnipedByPlayers[l_I].ScoreSaberID) + 1;
                    if (l_Snipe.SnipedByPlayers[l_I].OldRank >= l_Snipe.SnipedByPlayers[l_I].NewRank) l_Snipe.SnipedByPlayers.RemoveAt(l_I);
                }

                return l_Snipe; /// Returns the sniped players only if it not the first time the player is being registered.
            }

            Console.WriteLine("This player is missing his score saber id");

            return null;
        }

        public void LoadLeaderboard()
        {
            if (m_ErrorNumber < ERROR_LIMIT)
            {
                if (!Directory.Exists(PATH))
                {
                    Console.WriteLine("Seems like you forgot to Create the Level Directory, attempting creation..");
                    JsonDataBaseController.CreateDirectory(PATH);
                    Console.WriteLine("Directory Created, continuing Loading Levels");
                }

                try
                {
                    using (StreamReader l_SR = new StreamReader($"{PATH}{m_Filename}.json"))
                    {
                        m_Leaderboard = JsonSerializer.Deserialize<LeaderboardControllerFormat>(l_SR.ReadToEnd());
                        if (m_Leaderboard == null) /// json contain "null"
                        {
                            m_Leaderboard = new LeaderboardControllerFormat
                            {
                                Leaderboard = new List<RankedPlayer>
                                {
                                    new RankedPlayer
                                    {
                                        Name = "PlayerSample",
                                        IsPingAllowed = false
                                    }
                                }
                            };
                            Console.WriteLine("Leaderboard Created (Empty Format), contained null");
                        }
                        else
                        {
                            if (m_Leaderboard.Leaderboard != null)
                            {
                                //m_Leaderboard.Leaderboard = m_Leaderboard.Leaderboard.OrderByDescending(p_X => p_X.PassPoints).ToList();
                            }

                            if (m_Leaderboard.Leaderboard is { Count: >= 2 }) m_Leaderboard.Leaderboard.RemoveAll(p_X => p_X.ScoreSaberID == null);

                            Console.WriteLine($"{m_LeaderboardType}Leaderboard Loaded and Sorted");
                        }
                    }
                }
                catch (Exception) /// file format is wrong / there isn't any file.
                {
                    m_Leaderboard = new LeaderboardControllerFormat
                    {
                        Leaderboard = new List<RankedPlayer>
                        {
                            new RankedPlayer
                            {
                                Name = "PlayerSample",
                                IsPingAllowed = false
                            }
                        }
                    };
                    Console.WriteLine($"{m_LeaderboardType}Leaderboard Created (Empty Format)");
                }
            }
            else
            {
                Console.WriteLine("Too Many Errors => Method Locked, try finding the errors then use ResetRetryNumber()");
                Console.WriteLine("Please Contact an Administrator.");
            }
        }

        public void ReWriteLeaderboard()
        {
            /// This Method Serialise the data from m_Leaderboard and create the Cache.
            /// m_ErrorNumber will be increased at every error and lock the method if it exceed m_ErrorLimit

            if (m_ErrorNumber < ERROR_LIMIT)
            {
                try
                {
                    if (m_Leaderboard != null)
                    {
                        if (m_Leaderboard.Leaderboard.Count > 0)
                        {
                            m_Leaderboard.Leaderboard = m_Leaderboard.Leaderboard.OrderByDescending(p_X => p_X.Points).ToList();
                            File.WriteAllText($"{PATH}{m_Filename}.json", JsonSerializer.Serialize(m_Leaderboard));
                            Console.WriteLine($"{m_Filename}.json Updated and sorted ({m_Leaderboard.Leaderboard.Count} player in the leaderboard)");
                        }
                        else
                        {
                            Console.WriteLine("No Player in Leaderboard.");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Seems like you forgot to load the {m_LeaderboardType}Leaderboard, Attempting to load..");
                        LoadLeaderboard();
                        ReWriteLeaderboard();
                    }
                }
                catch
                {
                    Console.WriteLine(
                        $"An error occured While attempting to Write the {m_LeaderboardType}Leaderboard's cache file. (missing directory?)");
                    Console.WriteLine("Attempting to create the directory..");
                    m_ErrorNumber++;
                    JsonDataBaseController.CreateDirectory(PATH); /// m_ErrorNumber will increase again if the directory creation fail.
                    Thread.Sleep(200);
                    ReWriteLeaderboard();
                }
            }
            else
            {
                Console.WriteLine("Too Many Errors => Method Locked, try finding the errors then use ResetRetryNumber()");
                Console.WriteLine("Please Contact an Administrator.");
            }
        }
    }
}