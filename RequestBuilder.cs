using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
namespace HttpSocketClient
{
    public static class RequestBuilder
    {
        static byte[] CRLF = ASCIIEncoding.ASCII.GetBytes("\r\n");
        static byte[] HTTP_11 = ASCIIEncoding.ASCII.GetBytes("HTTP/1.1");

        public static byte[] BuildGetRequest(Uri requestUri, bool close)
        {
            var hostname = requestUri.Host;
            var port = requestUri.Port;

            // For kestrel use requestUri.PathAndQuery.ToString()            
            var request = Build("GET", requestUri.PathAndQuery.ToString(),
               // Headers
               "Host: " + hostname + (port != 80 ? ":" + port : string.Empty),
               "Content-Length: 0",
               close ? "Connection: close" : string.Empty);           

            DebugUtility.DumpASCII(request);

            return request;
        }


        static byte[] Build(string verb, string requestUri, params string[] headers)
        {
            var r = Request.Create();
            r.Append(verb);
            r.Append(" ");
            r.Append(requestUri);
            r.Append(" ");
            r.Append(HTTP_11);
            r.Append(CRLF);

            for (int i = 0; i < headers.Length; i++)
            {
                if (!String.IsNullOrWhiteSpace(headers[i]))
                {
                    r.Append(headers[i]);
                    r.Append(CRLF);
                }
            }

            r.Append(CRLF);
            return r.GetBytes();
        }

        struct Request
        {
            List<byte[]> _items;
            int _count;

            public static Request Create()
            {
                return new Request
                {
                    _items = new List<byte[]>(),
                    _count = 0
                };
            }

            public void Append(string content)
            {
                Append(Encoding.ASCII.GetBytes(content));
            }


            public void Append(byte[] payload)
            {
                _count += payload.Length;
                _items.Add(payload);
            }

            public byte[] GetBytes()
            {
                var encodedBytes = new byte[_count];
                for (int i = 0, index = 0; i < _items.Count; i++)
                {
                    Debug.Assert(encodedBytes.Length >= (index + _items[i].Length));

                    Buffer.BlockCopy(
                                        _items[i],
                                        0,
                                        encodedBytes,
                                        index,
                                        _items[i].Length);
                    index += _items[i].Length;
                }

                return encodedBytes;
            }
        }
    }
}
