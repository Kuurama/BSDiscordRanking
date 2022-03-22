using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using BSDiscordRanking.Controllers;
using BSDiscordRanking.Formats.Controller;
using BSDiscordRanking.Utils;
using Discord.Interactions;


namespace BSDiscordRanking.API
{
    internal static partial class WebApp
    {
        /// <summary>
        /// Listener
        /// </summary>
        private static HttpListener s_Listener;
        /// <summary>
        /// Cancellation token
        /// </summary>
        private static CancellationTokenSource s_CancellationToken;

        /// <summary>
        /// Server Config
        /// </summary>
        private static readonly ConfigFormat s_Config = ConfigController.GetConfig();

        /// <summary>
        /// Port used
        /// </summary>
        private const string PORT = "5000";

        private static List<Tuple<Regex, string>> s_RegexTuples = new List<Tuple<Regex, string>>();

        internal static void Start()
        {
            if (s_Listener != null) return;

            s_CancellationToken = new CancellationTokenSource();
            s_Listener = new HttpListener();
            s_Listener.Prefixes.Add($"http://127.0.0.1:{PORT}/");

            try
            {
                s_Listener.Start();
                Console.WriteLine("Listener Started");
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
                bool l_IsAuthorised = false;
                string l_PageData = null;
                HttpListenerRequest l_Request = p_Context.Request;
                if (l_Request.Url == null) return;

                HttpListenerResponse l_Response = p_Context.Response;

                if (l_Request.HttpMethod == "POST" && l_Request.Url.AbsolutePath == "/submit") /// On POST request (Don't have any usage yet)
                {
                    Console.WriteLine("Post submitted");
                }
                List<Tuple<int, ApiAccessHandler>> l_TupleFoundHandlers = new List<Tuple<int, ApiAccessHandler>>();
                foreach (KeyValuePair<string, ApiAccessHandler> l_Handler in ApiAccessHandler.s_Handlers)
                {
                    if (l_Handler.Value.AccessRegex.IsMatch(l_Request.Url.AbsolutePath))
                    {
                        l_TupleFoundHandlers.Add(new Tuple<int, ApiAccessHandler>(l_Handler.Value.Priority, l_Handler.Value));
                    }
                }

                ApiAccessHandler l_ApiAccessInstance = l_TupleFoundHandlers.Max()?.Item2;

                if (l_ApiAccessInstance != null)
                {
                    string l_Result = "";
                    string l_ErrorMessage = "";
                    string l_UrlAccessMatch = l_Request.Url.LocalPath;
                    l_UrlAccessMatch = l_ApiAccessInstance?.ParameterRegex.Match(l_Request.Url.LocalPath).Value;
                    List<string> l_Parameters = null;
                    if (!string.IsNullOrEmpty(l_UrlAccessMatch))
                    {
                        string l_TruncatedRequestURL = l_Request.Url.LocalPath.Replace(l_UrlAccessMatch, "");

                        l_Parameters = Regex.Split(l_TruncatedRequestURL, "(?<=^[^\"]*(?:\"[^\"]*\"[^\"]*)*)/(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)").ToList();
                    }
                    l_Parameters?.RemoveAll(p_X => p_X == "");

                    if (l_ApiAccessInstance.Call(l_Response, l_Parameters?.ToArray(), out l_Result, out l_ErrorMessage))
                    {
                        l_PageData = l_Result;
                        l_IsAuthorised = true;
                    }
                    else
                    {
                        Console.WriteLine(l_ErrorMessage);
                    }
                }

                l_Response.ContentType = "application/json";
                l_Response.ContentEncoding = Encoding.UTF8;

                byte[] l_Data;

                if (l_IsAuthorised && !string.IsNullOrEmpty(l_PageData))
                {
                    StringBuilder l_PageBuilder = new StringBuilder(l_PageData);
                    l_Data = Encoding.UTF8.GetBytes(l_PageBuilder.ToString());
                }
                else if (l_IsAuthorised)
                {
                    l_Response.StatusCode = 404;
                    l_Data = Encoding.UTF8.GetBytes("{ \"error\" : \"Not Found\"}");
                }
                else
                {
                    l_Response.StatusCode = 400;
                    l_Data = Encoding.UTF8.GetBytes("{ \"error\" : \"Bad Request\"}");
                }

                l_Response.AppendHeader("Access-Control-Allow-Origin", "*");
                l_Response.ContentLength64 = l_Data.LongLength;
                l_Response.OutputStream.Write(l_Data, 0, l_Data.Length);
                l_Response.OutputStream.Close();
            }
            catch (Exception l_Exception)
            {
                Console.WriteLine(l_Exception);
            }
        }
    }
}