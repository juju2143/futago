using Microsoft.Win32;
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

            // Installer/uninstaller subcommand arguments
            var dirOption = new Option<DirectoryInfo>("--dir", ()=>{
                if(OperatingSystem.IsLinux())
                    if(Environment.UserName == "root")
                        return new DirectoryInfo("/etc/opt/chrome/native-messaging-hosts");
                    else
                        return new DirectoryInfo(Environment.GetEnvironmentVariable("HOME")+"/.config/google-chrome/NativeMessagingHosts");
                else if(OperatingSystem.IsMacOS())
                    if(Environment.UserName == "root")
                        return new DirectoryInfo("/Library/Google/Chrome/NativeMessagingHosts");
                    else
                        return new DirectoryInfo(Environment.GetEnvironmentVariable("HOME")+"/Library/Application Support/Google/Chrome/NativeMessagingHosts");
                return new DirectoryInfo(".");
            }, "Target directory");
            var nameOption = new Option<string>("--name", ()=>"ca.a39.futago", "Application name");
            var descriptionOption = new Option<string>("--description", ()=>"Futago Native Messaging Host", "Application description");
            var originOption = new Option<string>("--origins", ()=>"chrome-extension://miboeaafijfjhncaapbmmipeaeobomnh/", "Comma-separated list of allowed origins");
            var dumpOption = new Option<bool>("--dump", "Prints the JSON manifest to stdout");
            var dryRunOption = new Option<bool>("--dry-run", "Don't do anything");
            var allOption = new Option<bool>("--all", "Install for all users"){ IsHidden = !OperatingSystem.IsWindows() };

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

            var install = new Command("install", "Registers native messaging host with your browser");
            install.Add(dirOption);
            install.Add(nameOption);
            install.Add(descriptionOption);
            install.Add(originOption);
            install.Add(allOption);
            install.Add(dumpOption);
            install.Add(dryRunOption);
            install.SetHandler(Install, dirOption, nameOption, descriptionOption, originOption, dumpOption, dryRunOption, allOption);
            rootCommand.Add(install);

            var uninstall = new Command("uninstall", "Unregisters native messaging host from your browser");
            uninstall.Add(dirOption);
            uninstall.Add(nameOption);
            uninstall.Add(allOption);
            uninstall.Add(dryRunOption);
            uninstall.SetHandler(Uninstall, dirOption, nameOption, dryRunOption, allOption);
            rootCommand.Add(uninstall);

            return rootCommand.InvokeAsync(argv).Result;
        }
        public static void Install(DirectoryInfo dir, string name, string description, string origin, bool dump, bool dryRun, bool all)
        {
            JObject json = JObject.FromObject(new{
                name = name,
                description = description,
                path = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName,
                type = "stdio",
                allowed_origins = origin.Split(',')
            });
            var fileName = dir.FullName + Path.DirectorySeparatorChar + name + ".json";
            if(dump) Console.WriteLine(json.ToString());
            if(!dryRun)
                try
                {
                    if(!dir.Exists) dir.Create();
                    File.WriteAllText(fileName, json.ToString());

                    if(OperatingSystem.IsWindows())
                    {
                        RegistryKey reg = all ? Registry.LocalMachine : Registry.CurrentUser;
                        using(RegistryKey key = reg.CreateSubKey(@"SOFTWARE\Google\Chrome\NativeMessagingHosts\"+name))
                        {
                            key.SetValue(null, fileName, RegistryValueKind.String);
                        }
                    }
                }
                catch(Exception e)
                {
                    Console.Error.WriteLine(e.Message);
                }
        }
        public static void Uninstall(DirectoryInfo dir, string name, bool dryRun, bool all)
        {
            var fileName = dir.FullName + Path.DirectorySeparatorChar + name + ".json";
            if(!dryRun)
                try
                {
                    File.Delete(fileName);
                    
                    if(OperatingSystem.IsWindows())
                    {
                        RegistryKey reg = all ? Registry.LocalMachine : Registry.CurrentUser;
                        reg.DeleteSubKey(@"SOFTWARE\Google\Chrome\NativeMessagingHosts\"+name, false);
                    }
                }
                catch(Exception e)
                {
                    Console.Error.WriteLine(e.Message);
                }
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