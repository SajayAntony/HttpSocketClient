using System;
using System.Diagnostics;
using System.Text;

namespace HttpSocketClient
{
    public static class DebugUtility
    {
        [Conditional("DEBUG")]
        public static void DumpASCII(SocketAwaitableEventArgs args)
        {
            if (args.BytesTransferred > 0)
            {
                DumpASCII(args.Buffer, 0, args.Count);
            }
        }

        public static void DumpASCII(byte[] content, int index = 0, int count = -1)
        {
            if (count == -1)
            {
                count = content.Length;
            }

            Console.WriteLine(Encoding.ASCII.GetChars(content, index, count));
        }
    }
}
