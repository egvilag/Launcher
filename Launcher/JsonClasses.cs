using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using System.Runtime.InteropServices;

namespace Launcher
{
    static class JsonClasses
    {
        //Records
        public const string header = "Elements JSON data";
        public const string clientName = "Launcher";
        public static string clientVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
        public static string clientPlatform = GetPlatform();

        static string GetPlatform()
        {
            int p = (int)Environment.OSVersion.Platform;
            switch (p)
            {
                case 0:
                    return "Windows Win32s";
                case 1:
                    return "Windows 9x";
                case 2:
                    return "Windows NT";
                case 3:
                    return "Windows CE";
                case 4:
                    return "Unix";
                case 6:
                    return "OSX";
                default:
                    return "Other"; 
            }
        }
    }

    class UpdateJSON
    {
        //Records
        const string command = "update";
        bool betaTesting;
        string hash;

        //Constructor
        public UpdateJSON(bool betaTesting, string hash)
        {
            this.betaTesting = betaTesting;
            this.hash = hash;
        }

        //Return JSON text
        public string GetResult()
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);
            JsonWriter writer = new JsonTextWriter(sw);
            writer.Formatting = Formatting.Indented;

            writer.WriteStartObject();
            writer.WritePropertyName("header");
            writer.WriteValue(JsonClasses.header);
            writer.WritePropertyName("client");
            writer.WriteStartObject();
            writer.WritePropertyName("name");
            writer.WriteValue(JsonClasses.clientName);
            writer.WritePropertyName("version");
            writer.WriteValue(JsonClasses.clientVersion);
            writer.WritePropertyName("platform");
            writer.WriteValue(JsonClasses.clientPlatform);
            writer.WriteEndObject();
            writer.WritePropertyName("command");
            writer.WriteValue(command);
            writer.WritePropertyName("betatesting");
            writer.WriteValue(this.betaTesting);
            writer.WritePropertyName("hash");
            writer.WriteValue(this.hash);
            writer.WriteEndObject();

            return sb.ToString();
        }
    }
}
