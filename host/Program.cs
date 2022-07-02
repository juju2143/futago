using Newtonsoft.Json.Linq;
using System.CommandLine;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace FutagoHost
{
    class Program
    {
        public static int Main(string[] argv)
        {
            // Arguments useful to the browser, we don't use them yet but we still define them
            var originArgument = new Argument<string>("origin", "Origin of the caller") { IsHidden = true };
            var parentWindowOption = new Option<string>("--parent-window", "Parent window") { IsHidden = true };

            // Test client subcommand arguments
            var urlArgument = new Argument<string>("url", "An URL to navigate to");
            var certOption = new Option<FileInfo>("--cert", "Attach a client certificate");
            var keyOption = new Option<FileInfo>("--key", "Private key for the certificate");

            var rootCommand = new RootCommand("Futago Native Messaging host app");
            rootCommand.Add(originArgument);
            rootCommand.Add(parentWindowOption);
            rootCommand.SetHandler(Host);

            var test = new Command("client", "Tests the client");
            test.Add(urlArgument);
            test.Add(certOption);
            test.Add(keyOption);
            test.SetHandler(Test, urlArgument, certOption, keyOption);
            rootCommand.Add(test);

            //var install = new Command("install", "Registers native messaging host with your browser");
            //var uninstall = new Command("uninstall", "Unregisters native messaging host in your browser");

            return rootCommand.InvokeAsync(argv).Result;
        }
        public static void Test(string url, FileInfo cert, FileInfo key)
        {
            if(url != null)
            {
                X509CertificateCollection store = null;
                if(cert != null && key != null && cert.Exists && key.Exists)
                {
                    try
                    {
                        store = new X509Certificate2Collection(X509Certificate2.CreateFromPemFile(cert.FullName, key.FullName));
                    }
                    catch(Exception e)
                    {
                        store = null;
                        Console.Error.WriteLine(e.Message);
                    }
                }
                Gemini gemini = new Gemini(url, store);
                try
                {
                    gemini.Connect();
                    Console.Write(gemini.ReadHeader());
                    Console.Write(gemini.ReadAll());
                }
                catch(Exception e)
                {
                    Console.Error.WriteLine(e.Message);
                }
                gemini.Close();
            }
            else
            {
                Console.Error.WriteLine("Error: URL is required");
            }
        }
        public static void Host()
        {
            JObject data;
            while ((data = Messaging.Read()) != null)
            {
                var url = data["url"]?.Value<string>();
                var queryFavicon = data["favicon"]?.Value<bool>();
                var cert = data["cert"]?.Value<string>();
                var key = data["key"]?.Value<string>();

                X509CertificateCollection store = null;
                if(cert != null && cert.Length > 0 && key != null && key.Length > 0)
                {
                    try
                    {
                        store = new X509Certificate2Collection(X509Certificate2.CreateFromPem(cert, key));
                    }
                    catch(CryptographicException e)
                    {
                        store = null;
                        Messaging.Write("error", e.Message);
                        Console.Error.WriteLine(e.Message);
                    }
                }
                // TODO: support other protocols?
                if(url != null)
                {
                    Gemini gemini = new Gemini(url, store);
                    try
                    {
                        gemini.Connect();
                        Messaging.Write("cert", Convert.ToBase64String(gemini.ServerCertificate.GetRawCertData()));
                        string header = gemini.ReadHeader();
                        Messaging.Write("header", header);
                        string result = gemini.ReadBase64();
                        if(result.Length > 0)
                        {
                            var chunked = result.Chunk(1048544);
                            foreach(var chunk in chunked)
                                Messaging.Write("data", new string(chunk));
                        }
                        gemini.Close();
                    }
                    catch(Exception e)
                    {
                        Messaging.Write("error", e.Message);
                        Console.Error.WriteLine(e.Message);
                        queryFavicon = false;
                    }

                    if(queryFavicon == true)
                    {
                        UriBuilder favurl = new UriBuilder(gemini.Url);
                        favurl.Path = "/favicon.txt";
                        Gemini favicon = new Gemini(favurl.Uri, store);
                        try
                        {
                            favicon.Connect();
                            string header = favicon.ReadHeader();
                            if(header[0] == '2')
                            {
                                string icon = favicon.ReadAll();
                                Messaging.Write("favicon", icon);
                            }
                            else
                                Messaging.Write("favicon", "");
                            favicon.Close();
                        }
                        catch(Exception e)
                        {
                            Console.Error.WriteLine(e.Message);
                            Messaging.Write("favicon", "");
                        }
                    }
                    Messaging.Write("end", true);
                }
            }
        }
    }
}