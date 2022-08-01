using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;

namespace BSDiscordRanking.API
{
    /// <summary>
    /// ApiAccessHandler attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ApiAccessHandler : Attribute
    {
        /// <summary>
        /// List of all handlers
        /// </summary>
        static public Dictionary<string, ApiAccessHandler> s_Handlers = new Dictionary<string, ApiAccessHandler>();

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Method info
        /// </summary>
        public MethodInfo Method;
        /// <summary>
        /// Api Endpoint Name
        /// </summary>
        public string Name;

        /// <summary>
        /// AccessRegex
        /// </summary>
        public Regex AccessRegex;
        /// <summary>
        /// ParameterRegex
        /// </summary>
        public Regex ParameterRegex;
        /// <summary>
        /// Priority number
        /// </summary>
        public int Priority;

        /// <summary>
        /// Argument types
        /// </summary>
        public ParameterInfo[] Parameters = null;

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p_Name"></param>
        /// <param name="p_Regex"></param>
        public ApiAccessHandler(string p_Name, string p_AccessRegexPattern, string p_ParameterRegexPattern, int p_Priority = 0)
        {
            Name = p_Name;
            AccessRegex = new Regex(p_AccessRegexPattern);
            ParameterRegex = new Regex(p_ParameterRegexPattern);
            Priority = p_Priority;
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Init the attribute
        /// </summary>
        /// <param name="p_Method">MethodInfo</param>
        public void Init(MethodInfo p_Method)
        {
            Parameters = p_Method.GetParameters();
            Method = p_Method;

            if (Parameters.Length == 0 || Parameters[0].ParameterType != typeof(HttpListenerResponse))
                throw new Exception("EndPoint " + Name + " has wrong parameter set");
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Call the effect handler
        /// </summary>
        /// <param name="p_Session">Session instance</param>
        public bool Call(HttpListenerResponse p_Session, string[] p_StringParameters, out string p_Result, out string p_ErrorMsg)
        {
            var l_Parameters = new object[Parameters.Length];
            l_Parameters[0] = p_Session;

            p_Result = "";
            p_ErrorMsg = "";

            if (p_StringParameters != null && p_StringParameters.Any() && p_StringParameters[0] != "")

                for (int l_I = 1; l_I < Parameters.Length; ++l_I)
                {
                    var l_Parameter = Parameters[l_I];

                    if ((l_I - 1) >= p_StringParameters.Length)
                    {
                        if (!l_Parameter.HasDefaultValue)
                        {
                            p_ErrorMsg = "Parameter " + (l_I - 1).ToString() + " is missing";
                            return false;
                        }

                        l_Parameters[l_I] = l_Parameter.DefaultValue;
                    }
                    else
                    {
                        string l_RawParameter = p_StringParameters[l_I - 1];


                        if (l_Parameter.ParameterType == typeof(ulong))
                            if (ulong.TryParse(l_RawParameter, out var l_Value)) l_Parameters[l_I] = l_Value;
                            else
                            {
                                p_ErrorMsg = "Parameter " + l_I.ToString() + " is expected to be ulong";
                                return false;
                            }
                        else if (l_Parameter.ParameterType == typeof(long))
                            if (long.TryParse(l_RawParameter, out var l_Value)) l_Parameters[l_I] = l_Value;
                            else
                            {
                                p_ErrorMsg = "Parameter " + l_I.ToString() + " is expected to be long";
                                return false;
                            }
                        else if (l_Parameter.ParameterType == typeof(ulong?))
                            if (ulong.TryParse(l_RawParameter, out var l_Value)) l_Parameters[l_I] = l_Value;
                            else
                            {
                                if (l_RawParameter is null or "null" or "")
                                    l_RawParameter = null;
                                else
                                {
                                    p_ErrorMsg = "Parameter " + l_I.ToString() + " is expected to be ulong?";
                                    return false;
                                }
                            }
                        else if (l_Parameter.ParameterType == typeof(uint))
                            if (uint.TryParse(l_RawParameter, out var l_Value)) l_Parameters[l_I] = l_Value;
                            else
                            {
                                p_ErrorMsg = "Parameter " + l_I.ToString() + " is expected to be uint";
                                return false;
                            }
                        else if (l_Parameter.ParameterType == typeof(uint?))
                            if (uint.TryParse(l_RawParameter, out var l_Value)) l_Parameters[l_I] = l_Value;
                            else
                            {
                                if (l_RawParameter is null or "null" or "")
                                    l_RawParameter = null;
                                else
                                {
                                    p_ErrorMsg = "Parameter " + l_I.ToString() + " is expected to be uint?";
                                    return false;
                                }
                            }
                        else if (l_Parameter.ParameterType == typeof(int))
                            if (int.TryParse(l_RawParameter, out var l_Value)) l_Parameters[l_I] = l_Value;
                            else
                            {
                                p_ErrorMsg = "Parameter " + l_I.ToString() + " is expected to be int";
                                return false;
                            }
                        else if (l_Parameter.ParameterType == typeof(int?))
                            if (int.TryParse(l_RawParameter, out var l_Value)) l_Parameters[l_I] = l_Value;
                            else
                            {
                                if (l_RawParameter is null or "null" or "")
                                    l_RawParameter = null;
                                else
                                {
                                    p_ErrorMsg = "Parameter " + l_I.ToString() + " is expected to be int?";
                                    return false;
                                }
                            }
                        else if (l_Parameter.ParameterType == typeof(ushort))
                            if (ushort.TryParse(l_RawParameter, out var l_Value)) l_Parameters[l_I] = l_Value;
                            else
                            {
                                p_ErrorMsg = "Parameter " + l_I.ToString() + " is expected to be ushort";
                                return false;
                            }
                        else if (l_Parameter.ParameterType == typeof(ushort?))
                            if (ushort.TryParse(l_RawParameter, out var l_Value)) l_Parameters[l_I] = l_Value;
                            else
                            {
                                if (l_RawParameter is null or "null" or "")
                                    l_RawParameter = null;
                                else
                                {
                                    p_ErrorMsg = "Parameter " + l_I.ToString() + " is expected to be ushort?";
                                    return false;
                                }
                            }
                        else if (l_Parameter.ParameterType == typeof(short))
                            if (short.TryParse(l_RawParameter, out var l_Value)) l_Parameters[l_I] = l_Value;
                            else
                            {
                                p_ErrorMsg = "Parameter " + l_I.ToString() + " is expected to be short";
                                return false;
                            }
                        else if (l_Parameter.ParameterType == typeof(byte))
                            if (byte.TryParse(l_RawParameter, out var l_Value)) l_Parameters[l_I] = l_Value;
                            else
                            {
                                p_ErrorMsg = "Parameter " + l_I.ToString() + " is expected to be byte";
                                return false;
                            }
                        else if (l_Parameter.ParameterType == typeof(byte?))
                            if (byte.TryParse(l_RawParameter, out var l_Value)) l_Parameters[l_I] = l_Value;
                            else
                            {
                                if (l_RawParameter is null or "null" or "")
                                    l_RawParameter = null;
                                else
                                {
                                    p_ErrorMsg = "Parameter " + l_I.ToString() + " is expected to be byte?";
                                    return false;
                                }
                            }
                        else if (l_Parameter.ParameterType == typeof(sbyte))
                            if (sbyte.TryParse(l_RawParameter, out var l_Value)) l_Parameters[l_I] = l_Value;
                            else
                            {
                                p_ErrorMsg = "Parameter " + l_I.ToString() + " is expected to be sbyte";
                                return false;
                            }
                        else if (l_Parameter.ParameterType == typeof(sbyte?))
                            if (sbyte.TryParse(l_RawParameter, out var l_Value)) l_Parameters[l_I] = l_Value;
                            else
                            {
                                if (l_RawParameter is null or "null" or "")
                                    l_RawParameter = null;
                                else
                                {
                                    p_ErrorMsg = "Parameter " + l_I.ToString() + " is expected to be sbyte?";
                                    return false;
                                }
                            }
                        else if (l_Parameter.ParameterType == typeof(bool))
                            if (bool.TryParse(l_RawParameter, out var l_Value)) l_Parameters[l_I] = l_Value;
                            else
                            {
                                p_ErrorMsg = "Parameter " + l_I.ToString() + " is expected to be bool";
                                return false;
                            }
                        else if (l_Parameter.ParameterType == typeof(string))
                        {
                            if (l_RawParameter.Length == 0)
                            {
                                p_ErrorMsg = "Parameter " + l_I.ToString() + " is expected to be string";
                                return false;
                            }

                            l_Parameters[l_I] = l_RawParameter;
                        }
                        else if (l_Parameter.ParameterType == typeof(float))
                        {
                            if (float.TryParse(l_RawParameter, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var l_Value))
                                l_Parameters[l_I] = l_Value;
                            else
                            {
                                p_ErrorMsg = "Parameter " + l_I.ToString() + " is expected to be float";
                                return false;
                            }
                        }
                        else if (l_Parameter.ParameterType == typeof(double))
                        {
                            if (double.TryParse(l_RawParameter, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var l_Value))
                                l_Parameters[l_I] = l_Value;
                            else
                            {
                                p_ErrorMsg = "Parameter " + l_I.ToString() + " is expected to be double";
                                return false;
                            }
                        }
                    }
                }
            var l_Return = Method.Invoke(null, l_Parameters);

            if (l_Return != null)
                p_Result = l_Return.ToString();

            return true;
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Init all api access handler
        /// </summary>
        public static void InitHandlers()
        {
            s_Handlers.Clear();

            foreach (Assembly l_Assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type l_Type in l_Assembly.GetTypes())
                {
                    foreach (MethodInfo l_Method in l_Type.GetMethods())
                    {
                        var l_Attributes = l_Method.GetCustomAttributes(typeof(ApiAccessHandler));

                        // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
                        foreach (ApiAccessHandler l_Attribute in l_Attributes)
                        {
                            l_Attribute.Init(l_Method);

                            if (!s_Handlers.ContainsKey(l_Attribute.Name))
                                s_Handlers.Add(l_Attribute.Name, l_Attribute);
                            else
                                s_Handlers[l_Attribute.Name] = l_Attribute;
                        }
                    }
                }
            }
        }
    }
}
