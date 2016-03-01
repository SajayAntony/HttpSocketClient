using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace HttpSocketClient
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                // string requestUri = "http://www.bing.com/";      // DNSHost
                // string requestUri = "http://localhost:5000/";    // Kestrel
                 string requestUri= "http://localhost:12777/";    // IIS
                RequestAsync(requestUri).Wait();
            }
        }

        static async Task RequestAsync(string requestUri)
        {
            var uri = new Uri(requestUri);
            string hostname = uri.Host;
            int port = uri.Port;
            var endpoint = GetEndpoint(uri.Host, uri.Port);

            Socket socket = null;
            try
            {
                socket = await endpoint.ConnectAsync();
                socket.SendRequest(() => GetRequest(uri.ToString(), hostname, port));
                await socket.DrainResponse();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
            finally
            {
                if (socket != null)
                {
                    socket.Dispose();
                }
            }
        }

        private static byte[] GetRequest(string requestUri, string hostname, int port)
        {
            var request = RequestBuilder.Build("GET", requestUri,
                // Headers
                "Host: " + hostname + (port != 80 ? ":" + port : string.Empty),
                "Content-Length: 0",
                "Connection: close"
            );

            DebugUtility.DumpASCII(request);

            return request;
        }

        private static EndPoint GetEndpoint(string hostname, int port)
        {
            EndPoint endpoint = null;
            IPAddress address;
            if (IPAddress.TryParse(hostname, out address))
            {
                endpoint = new IPEndPoint(address, port);
            }
            else
            {
                endpoint = new DnsEndPoint(hostname, port);
            }

            return endpoint;
        }
    }
}
