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
            RequestAsync().Wait();
        }

        static async Task RequestAsync()
        {
            string hostname = "127.0.0.1";
            int port = 5000;

            IPAddress address = IPAddress.Parse(hostname);
            IPEndPoint endpoint = new IPEndPoint(address, port);

            using (var socket = await ConnectAsync(endpoint))
            {
                const string request =
                                "GET / HTTP/1.1\r\n" +
                                "Host: 127.0.0.1\r\n" +
                                "Content-Length: 0\r\n" +
                                "Connection: close\r\n" +
                                "\r\n";
                var buffer = new byte[1024];
                socket.Send(Encoding.ASCII.GetBytes(request));
                using (SocketAwaitableEventArgs args = new SocketAwaitableEventArgs())
                {
                    args.SetBuffer(buffer, 0, buffer.Length);
                    while (true)
                    {
                        try
                        {
                            await socket.ReceiveSocketAsync(args);
                            if (args.BytesTransferred == 0)
                            {
                                break;
                            }

                            DebugUtility.DumpASCII(args);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            throw;
                        }
                    }
                }
                socket.Dispose();
            }
        }

        internal static async Task<Socket> ConnectAsync(
                            EndPoint endpoint,
                            SocketType socketType = SocketType.Stream,
                            ProtocolType protocolType = ProtocolType.Tcp)
        {
            var socket = new Socket(socketType, protocolType);
            bool disposeSocket = false;
            try
            {
                using (SocketAwaitableEventArgs args = new SocketAwaitableEventArgs())
                {
                    args.RemoteEndPoint = endpoint;
                    await socket.ConnectSocketAsync(args);
                }
            }
            catch (Exception)
            {
                disposeSocket = true;
                throw;
            }
            finally
            {
                if (disposeSocket)
                {
                    socket.Dispose();
                    socket = null;
                }
            }

            return socket;
        }
    }

}
