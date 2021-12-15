using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;

namespace CPvC
{
    static public class Helpers
    {
        /// <summary>
        /// Encodes a DateTime as a 64-bit integer.
        /// </summary>
        /// <param name="dt">DateTime to be converted.</param>
        /// <returns>A 64-bit integer representing the DateTime object.</returns>
        static public long DateTimeToNumber(DateTime dt)
        {
            return dt.Ticks;
        }

        /// <summary>
        /// Decodes a 64-bit integer encoded by <c>DateTimeToNumber</c> back to a DateTime object.
        /// </summary>
        /// <param name="n">64-bit integer to be converted.</param>
        /// <returns>A DateTime object.</returns>
        static public DateTime NumberToDateTime(Int64 n)
        {
            return new DateTime(n);
        }

        /// <summary>
        /// Decodes a 64-bit integer encoded by <c>DateTimeToNumber</c> back to a DateTime object.
        /// </summary>
        /// <param name="n">Decimal string representation of the 64-bit integer to be converted.</param>
        /// <returns>A DateTime object.</returns>
        static public DateTime NumberToDateTime(string n)
        {
            return NumberToDateTime(Convert.ToInt64(n));
        }

        /// <summary>
        /// Compresses a byte array.
        /// </summary>
        /// <param name="bytes">Byte array to be compressed.</param>
        /// <returns>A compressed byte array.</returns>
        static public byte[] Compress(byte[] bytes)
        {
            MemoryStream outStream = new MemoryStream();
            using (System.IO.Compression.DeflateStream deflate = new System.IO.Compression.DeflateStream(outStream, System.IO.Compression.CompressionMode.Compress))
            {
                deflate.Write(bytes, 0, bytes.Length);
            }

            return outStream.ToArray();
        }

        /// <summary>
        /// Uncompresses a byte array compressed with <c>Compress</c>.
        /// </summary>
        /// <param name="compressedBytes">Compressed byte array.</param>
        /// <returns>An uncompressed byte array.</returns>
        static public byte[] Uncompress(byte[] compressedBytes)
        {
            MemoryStream inStream = new MemoryStream(compressedBytes);
            using (MemoryStream outStream = new MemoryStream())
            using (System.IO.Compression.DeflateStream deflate = new System.IO.Compression.DeflateStream(inStream, System.IO.Compression.CompressionMode.Decompress))
            {
                deflate.CopyTo(outStream);

                return outStream.ToArray();
            }
        }

        /// <summary>
        /// Splits a string based on a delimiter, while ignoring delimiters escaped with a backspace.
        /// </summary>
        /// <param name="delim">Delimiter character.</param>
        /// <param name="str">String to split.</param>
        /// <returns>An array of strings.</returns>
        static public List<string> SplitWithEscape(char delim, string str)
        {
            List<string> strs = new List<string>();

            string currentStr = "";
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] != '\\' && str[i] != delim)
                {
                    currentStr += str[i];
                }
                else if (str[i] == delim)
                {
                    strs.Add(currentStr);
                    currentStr = "";
                }
                else
                {
                    i++;
                    currentStr += str[i];
                }
            }

            strs.Add(currentStr);

            return strs;
        }

        /// <summary>
        /// Joins a list of strings, ensuring to escape any delimiter characters before joining.
        /// </summary>
        /// <param name="delim">Delimiter character.</param>
        /// <param name="strs">Strings to join.</param>
        /// <returns>A joined string.</returns>
        static public string JoinWithEscape(char delim, IEnumerable<string> strs)
        {
            string delimStr = delim.ToString();
            return String.Join(delimStr, strs.Select(x => x.Replace("\\", "\\\\").Replace(delimStr, String.Format("\\{0}", delim))));
        }

        /// <summary>
        /// Converts a given number of ticks (of the CPC's internal 4MHz clock) to a TimeSpan object.
        /// </summary>
        /// <param name="ticks">The number of ticks to convert.</param>
        /// <returns>A TimeSpan object corresponding to the amount of time <c>ticks</c> represents.</returns>
        static public TimeSpan GetTimeSpanFromTicks(UInt64 ticks)
        {
            return new TimeSpan((long)(5 * ticks / 2));
        }

        /// <summary>
        /// Resolves a hostname and port to an IPv4 IPEndPoint.
        /// </summary>
        /// <param name="hostname">Name of the host to resolve.</param>
        /// <param name="port">Port number</param>
        /// <returns>An IPEndPoint corresponding to the specified hostname and port; null if resolving failed.</returns>
        static public System.Net.IPEndPoint GetEndpoint(string hostname, UInt16 port)
        {
            System.Net.IPAddress[] addrs;

            try
            {
                addrs = System.Net.Dns.GetHostAddresses(hostname);
            }
            catch (SocketException)
            {
                return null;
            }

            System.Net.IPAddress ipAddr = addrs.Where(addr => addr.AddressFamily == AddressFamily.InterNetwork).FirstOrDefault();
            if (ipAddr != null)
            {
                return new System.Net.IPEndPoint(ipAddr, port);
            }

            return null;
        }

        static public string StrFromBytes(byte[] bytes)
        {
            if (bytes == null)
            {
                return String.Empty;
            }

            StringWriter strw = new StringWriter();
            for (int i = 0; i < bytes.Length; i++)
            {
                strw.Write(String.Format("{0:X2}", bytes[i]));
            }

            return strw.ToString();
        }

        static public byte[] BytesFromStr(string str)
        {
            if (str == String.Empty)
            {
                return null;
            }

            byte[] bytes = new byte[str.Length / 2];

            for (int i = 0; i < bytes.Length; i++)
            {
                string h = str.Substring(i * 2, 2);
                byte b = System.Convert.ToByte(h, 16);
                bytes[i] = b;
            }

            return bytes;
        }
    }
}
