using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using BSDiscordRanking.Controllers;
using BSDiscordRanking.Formats.API;
using BSDiscordRanking.Formats.Player;
using Newtonsoft.Json;


namespace BSDiscordRanking.API
{
    internal static class WebApp
    {
        /// <summary>
        /// Listener
        /// </summary>
        private static HttpListener s_Listener;
        /// <summary>
        /// Page data
        /// </summary>
        private static string s_PageData;
        /// <summary>
        /// Cancellation token
        /// </summary>
        private static CancellationTokenSource s_CancellationToken;

        /// <summary>
        /// Port used
        /// </summary>
        private const string PORT = "8080";

        internal static void Start()
        {
            if (s_PageData == null)
            {
                using StreamReader l_Reader = new StreamReader("./API/DebugPageData.html");
                s_PageData = l_Reader.ReadToEnd();
            }

            if (s_Listener != null) return;

            s_CancellationToken = new CancellationTokenSource();
            s_Listener          = new HttpListener();
            s_Listener.Prefixes.Add($"http://localhost:{PORT}/");

            try
            {       
                s_Listener.Start();
            }
            catch (HttpListenerException l_Exception)
            {
                Console.WriteLine($"Can't start the listener: + {l_Exception}");
                return;
            }

            while (!s_CancellationToken.IsCancellationRequested)
            {
                OnContext(s_Listener.GetContext());
            }
        }

        /// <summary>
        /// Stop the webapp
        /// </summary>
        internal static void Stop()
        {
            if (s_CancellationToken == null) return;
            
            s_CancellationToken.Cancel();
            Console.WriteLine("Listener Stopped");
        }

        /// <summary>
        /// On HTTP request
        /// </summary>
        /// <param name="p_Context"></param>
        private static void OnContext(HttpListenerContext p_Context)
        {
            try
            {
                HttpListenerRequest l_Request = p_Context.Request;
                if (l_Request.Url == null) return;
           
                
                HttpListenerResponse l_Response = p_Context.Response;

                if (l_Request.HttpMethod == "POST" && l_Request.Url.AbsolutePath == "/submit") /// On POST request (Don't have any usage yet)
                {
                    Console.WriteLine("Post submitted");
                }

                Regex l_PlayerRegex = new Regex(@"\/player\/0*[1-9][0-9]*");

                if (l_PlayerRegex.IsMatch(l_Request.Url.AbsolutePath))
                {
                    string l_PlayerID = Regex.Replace(l_Request.Url.AbsolutePath, @"\/player\/", "");
                    GetPlayerInfo(l_Response, l_PlayerID);
                    return;
                }
                
                StringBuilder l_PageBuilder = new StringBuilder(s_PageData);

                byte[] l_Data = Encoding.UTF8.GetBytes(l_PageBuilder.ToString());
                l_Response.ContentType = "text/html";
                l_Response.ContentEncoding = Encoding.UTF8;
                l_Response.ContentLength64 = l_Data.LongLength;

                l_Response.OutputStream.Write(l_Data, 0, l_Data.Length);
                l_Response.OutputStream.Close();
            }
            catch (Exception l_Exception)
            {
                Console.WriteLine(l_Exception);
            }
        }

        private static void GetPlayerInfo(HttpListenerResponse p_Response, string p_PlayerID)
        {
            if (UserController.UserExist(p_PlayerID))
            {
                p_PlayerID = UserController.GetPlayer(p_PlayerID);
            }
            else if (!UserController.AccountExist(p_PlayerID) && !UserController.UserExist(p_PlayerID))
            {
                return;
            }

            Player l_Player = new Player(p_PlayerID);
            PlayerStatsFormat l_PlayerStats = l_Player.GetStats();
            string l_PlayerFullJsonString = JsonConvert.SerializeObject(l_Player.m_PlayerFull);

            PlayerApiOutput l_PlayerApiOutput = new PlayerApiOutput()
            {
                PlayerFull = l_Player.m_PlayerFull,
                PlayerStats = l_Player.m_PlayerStats
            };
       
            StringBuilder l_PageBuilder = new StringBuilder(JsonConvert.SerializeObject(l_PlayerApiOutput));
            
            byte[] l_Data = Encoding.UTF8.GetBytes(l_PageBuilder.ToString());
            p_Response.ContentType = "text/html";
            p_Response.ContentEncoding = Encoding.UTF8;
            p_Response.ContentLength64 = l_Data.LongLength;
            
            p_Response.OutputStream.Write(l_Data, 0, l_Data.Length);
            p_Response.OutputStream.Close();
        }
    }
}