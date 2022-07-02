using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FutagoHost
{
    // https://stackoverflow.com/questions/30880709/c-sharp-native-host-with-chrome-native-messaging
    class Messaging
    {
        public static JObject Read()
        {
            Stream stdin = Console.OpenStandardInput();
            uint length = 0;

            var lengthBytes = new byte[4];
            stdin.Read(lengthBytes, 0, 4);
            length = BitConverter.ToUInt32(lengthBytes, 0);

            var buffer = new char[length];
            using (var reader = new StreamReader(stdin))
            {
                var count = reader.Read(buffer, 0, buffer.Length);
                Console.Error.WriteLine("read {0} bytes", count);
                //Console.Error.WriteLine(buffer);
                if(count == 0) return null;
            }

            return (JObject)JsonConvert.DeserializeObject<JObject>(new string(buffer));
        }

        public static void Write(JObject json)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(json.ToString(Formatting.None));

            var stdout = Console.OpenStandardOutput();
            stdout.WriteByte((byte)((bytes.Length >> 0) & 0xFF));
            stdout.WriteByte((byte)((bytes.Length >> 8) & 0xFF));
            stdout.WriteByte((byte)((bytes.Length >> 16) & 0xFF));
            stdout.WriteByte((byte)((bytes.Length >> 24) & 0xFF));
            stdout.Write(bytes, 0, bytes.Length);
            stdout.Flush();
        }

        public static void Write(string key, JToken data)
        {
            var json = new JObject();
            json[key] = data;
            Write(json);
        }
    }
}