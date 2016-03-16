using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Buffers;

namespace HttpSocketClient
{
    public static class HttpExtensions
    {
        static ArrayPool<byte> bytePool = System.Buffers.ArrayPool<byte>.Shared;

        internal static async Task<Socket> ConnectAsync(
                           this EndPoint endpoint,
                           SocketType socketType = SocketType.Stream,
                           ProtocolType protocolType = ProtocolType.Tcp)
        {
            var socket = new Socket(socketType, protocolType);
            Bind(socket, endpoint);
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

        private static void Bind(Socket socket, EndPoint endpoint)
        {
            var localendpoint = endpoint.CloneIPWithoutPort();
            socket.Bind(localendpoint);
        }

        public static IPEndPoint CloneIPWithoutPort(this EndPoint endpoint)
        {
            IPEndPoint dest = endpoint as IPEndPoint;
            IPEndPoint ip = new IPEndPoint(dest.Address, 0);
            return ip;
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

        public static async Task DrainResponse(this Socket socket, bool close = false)
        {

            byte[] buffer = bytePool.Rent(500);

            try
            {
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
            finally
            {
                if (close)
                {
                    socket.Dispose();
                }

                bytePool.Return(buffer);
            }
        }
    }
}
