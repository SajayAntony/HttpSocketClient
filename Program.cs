using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HttpSocketClient
{
    public class Program
    {
        static TaskCompletionSource<object> s_tcsComplete;
        static int s_connectionCount = 0;

        public static void Main(string[] args)
        {
            var requestUri = string.Empty;
            if (args.Length == 0)
            {
                // requestUri = "http://www.bing.com/";      // DNSHost
                requestUri = "http://127.0.0.1:5000/";    // Kestrel
                // requestUri = "http://localhost:12777/";    // IIS

            }
            else
            {
                requestUri = args[0];
            }

            s_tcsComplete = new TaskCompletionSource<object>();

            new Timer((_) =>
            {
                Console.WriteLine("Number of Connections: " + s_connectionCount);
            }, null, 0, 1000);


            Task.Run(async () => {
                // var eth0 = "http://172.30.169.104:5000/";
                var eth0 = requestUri;
                Console.WriteLine("URI:{0} , LocalIP: {1}", eth0, GetEndpoint(eth0).CloneIPWithoutPort());
                for (int j = 0; j < 30 * 1000; j++)
                {
                    // Do not flood connections.
                    await Task.Delay(5);
                    RequestAsync(eth0);
                }
            });


            Console.ReadLine();
            s_tcsComplete.SetResult(null);
        }

        static async Task RequestAsync(string requestUri)
        {
            var uri = new Uri(requestUri);
            var endpoint = GetEndpoint(requestUri);

            Socket socket = null;
            try
            {
                socket = await endpoint.ConnectAsync();
                Interlocked.Increment(ref s_connectionCount);
                var request = RequestBuilder.BuildGetRequest(uri, false);
                do
                {
                    await socket.ProcessRequest(
                        () => request,
                        async () =>
                        {
                            await socket.DrainResponse();
                        });
                    await Task.Delay(10 * 1000);
                } while ((!s_tcsComplete.Task.IsCompleted));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Environment.FailFast(ex.Message);
                throw;
            }
            finally
            {
                if (socket != null)
                {
                    socket.Dispose();
                }

                Interlocked.Decrement(ref s_connectionCount);
            }
        }

        private static EndPoint GetEndpoint(string requestUri)
        {
            Uri uri = new Uri(requestUri);
            var hostname = uri.Host;
            var port = uri.Port;
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
