﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace FutagoHost
{
    class Program
    {
        public static void Main(string[] argv)
        {
            // TODO: some sort of --install option
            if(argv.Length > 1 && argv[0] == "--test")
            {
                var url = argv[1];
                Gemini gemini = new Gemini(url);
                try
                {
                    gemini.Connect();
                    Console.WriteLine(gemini.ReadHeader());
                    Console.WriteLine(gemini.ReadAll());
                }
                catch(Exception e)
                {
                    Console.Error.WriteLine(e.Message);
                }
            }
            else
            {
                JObject data;
                while ((data = Messaging.Read()) != null)
                {
                    var url = data["url"]?.Value<string>();
                    // TODO: get certificates
                    // TODO: support other protocols?
                    if(url != null)
                    {
                        Gemini gemini = new Gemini(url);
                        try
                        {
                            gemini.Connect();
                            string header = gemini.ReadHeader();
                            Messaging.Write("header", header);
                            string result = gemini.ReadBase64();
                            var chunked = result.Chunk(1048544);
                            foreach(var chunk in chunked)
                                Messaging.Write("data", new string(chunk));
                            Messaging.Write("end", true);
                        }
                        catch(Exception e)
                        {
                            Messaging.Write("error", e.Message);
                            Console.Error.WriteLine(e.Message);
                        }
                    }
                }
            }
        }
    }
}