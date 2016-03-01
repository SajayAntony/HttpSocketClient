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
            var requestUri = string.Empty;
            if (args.Length == 0)
            {
                 requestUri = "http://www.bing.com/";      // DNSHost
                // requestUri = "http://localhost:5000/";    // Kestrel
                // requestUri = "http://localhost:12777/";    // IIS

            }
            else
            {
                requestUri = args[0];
            }

            RequestAsync(requestUri).Wait();
        }

        static async Task RequestAsync(string requestUri)
        {
            var uri = new Uri(requestUri);
            var endpoint = GetEndpoint(uri.Host, uri.Port);

            Socket socket = null;
            try
            {
                socket = await endpoint.ConnectAsync();

                await socket.ProcessRequest(
                    () => RequestBuilder.BuildGetRequest(uri),
                    () => socket.DrainResponse());                
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
