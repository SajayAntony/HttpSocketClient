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

        public static void Main(string[] args)
        {
            var requestUri = string.Empty;
            if (args.Length == 0)
            {
                // requestUri = "http://www.bing.com/";      // DNSHost
                requestUri = "http://localhost:5000/";    // Kestrel
                // requestUri = "http://localhost:12777/";    // IIS

            }
            else
            {
                requestUri = args[0];
            }

            // You only have one request. 
            s_tcsComplete = new TaskCompletionSource<object>();
            //s_tcsComplete.SetResult(null);
            //RequestAsync(requestUri).Wait();
            //s_tcsComplete = new TaskCompletionSource<object>();
            List<Task> requests = new List<Task>();
            for (int i = 0; i < 10000; i++)
            {
                // Do not flood connections.
                Thread.Sleep(10);
                RequestAsync(requestUri);
                //requests.Add(RequestAsync(requestUri));
            }
            Console.ReadLine();
            s_tcsComplete.SetResult(null);

            //Task.Delay(1000).ContinueWith((_) => s_tcsComplete.SetResult(null));
            //Task.WaitAll(requests.ToArray());
        }

        static async Task RequestAsync(string requestUri)
        {
            var uri = new Uri(requestUri);
            var endpoint = GetEndpoint(uri.Host, uri.Port);

            Socket socket = null;
            try
            {
                socket = await endpoint.ConnectAsync();
                do
                {
                    await socket.ProcessRequest(
                        () => RequestBuilder.BuildGetRequest(uri, false),
                        async () =>
                        {
                            await socket.DrainResponse();
                        });

                    await s_tcsComplete.Task;
                } while ((!s_tcsComplete.Task.IsCompleted));
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
