using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace HttpSocketClient
{
    public static class HttpExtensions
    {
        internal static async Task<Socket> ConnectAsync(
                           this EndPoint endpoint,
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

        public static async Task<Socket> ProcessRequest(this Socket socket,
                                                    Func<byte[]> requestBuilder, 
                                                    Func<Task> handleResponse)
        {
            var request = requestBuilder();
            socket.Send(request);
            await handleResponse();
            return socket;
        }

        public static async Task DrainResponse(this Socket socket)
        {
            var buffer = new byte[1024];
            using (SocketAwaitableEventArgs args = new SocketAwaitableEventArgs())
            {                
                while (true)
                {
                    try
                    {
                        args.SetBuffer(buffer, 0, buffer.Length);
                        await socket.ReceiveSocketAsync(args);
                        if (args.BytesTransferred == 0)
                        {
                            socket.Dispose();
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
        }
    }
}
