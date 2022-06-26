using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace FutagoHost
{
    class Gemini
    {
        public Uri Url { get; set; }
        SslStream sslStream;
        public Gemini(string url)
        {
            Url = new Uri(url);
        }
        public Gemini(Uri url)
        {
            Url = url;
        }
        bool ValidateServerCertificate(
              object sender,
              X509Certificate certificate,
              X509Chain chain,
              SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;
            // TODO: validate user certificates
            //Console.WriteLine("Certificate error: {0}", sslPolicyErrors);
            return true;
        }
        public void Connect()
        {
            var port = Url.Port;
            if(port < 0) port = 1965;
            TcpClient client = new TcpClient(Url.DnsSafeHost, port);
            sslStream = new SslStream(client.GetStream(), false,
                new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
            sslStream.AuthenticateAsClient(Url.Host);
            byte[] message = Encoding.UTF8.GetBytes(Url.AbsoluteUri+"\r\n");

            sslStream.Write(message);
            sslStream.Flush();
        }
        public string ReadHeader()
        {
            StringBuilder message = new StringBuilder();
            int b;
            do
            {
                b = sslStream.ReadByte();
                message.Append((char)b);
            } while(b != 10 && b != -1);
            return message.ToString();
        }
        public int Read(byte[] buffer, int offset, int count)
        {
            return sslStream.Read(buffer, offset, count);
        }
        public string ReadAll()
        {
            byte[] buffer = new byte[2048];
            StringBuilder messageData = new StringBuilder();
            int bytes = -1;
            do
            {
                bytes = Read(buffer, 0, buffer.Length);

                Decoder decoder = Encoding.UTF8.GetDecoder();
                char[] chars = new char[decoder.GetCharCount(buffer, 0, bytes)];
                decoder.GetChars(buffer, 0, bytes, chars, 0);
                messageData.Append(chars);
            } while (bytes != 0);

            return messageData.ToString();
        }
        public string ReadBase64()
        {
            IEnumerable<byte> array = new byte[0];
            int bytes = -1;
            byte[] buffer = new byte[2048];
            do
            {
                bytes = Read(buffer, 0, buffer.Length);
                var buffer2 = new byte[bytes];
                Array.Copy(buffer, buffer2, bytes);
                array = array.Concat(buffer2);
            } while (bytes != 0);

            return Convert.ToBase64String(array.ToArray());
        }
    }
}