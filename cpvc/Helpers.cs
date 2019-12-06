using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
        /// <param name="n">Decimal string representation of the 64-bit integer to be converted.</param>
        /// <returns>A DateTime object.</returns>
        static public DateTime NumberToDateTime(string n)
        {
            return new DateTime(Convert.ToInt64(n));
        }

        /// <summary>
        /// Creates a hexadecimal string representation of a byte array.
        /// </summary>
        /// <param name="b">Byte array to be represented as a hexadecimal string.</param>
        /// <returns>Hexadecimal string representation of the byte array.</returns>
        static public string HexString(byte[] b)
        {
            if (b == null)
            {
                return String.Empty;
            }

            char[] buffer = new char[b.Length * 2];

            for (int c = 0; c < b.Length; c++)
            {
                byte loNibble = (byte)(b[c] & 0x0f);
                byte hiNibble = (byte)(b[c] >> 4);

                loNibble += ((loNibble < 10) ? ((byte)'0') : ((byte)'7'));
                hiNibble += ((hiNibble < 10) ? ((byte)'0') : ((byte)'7'));

                buffer[c * 2] = (char)hiNibble;
                buffer[(c * 2) + 1] = (char)loNibble;
            }

            return new string(buffer);
        }

        /// <summary>
        /// Converts a hexadecimal string to a byte array.
        /// </summary>
        /// <param name="hexString">Hexadecimal string to be converted.</param>
        /// <returns>A byte array based on the hexadecimal string.</returns>
        static public byte[] Bytes(string hexString)
        {
            if ((hexString.Length % 2) == 1)
            {
                throw new ArgumentException(String.Format("Hex string length should be an even number."));
            }

            if (hexString.Length == 0)
            {
                return null;
            }

            int count = hexString.Length / 2;
            byte[] bytes = new byte[count];

            for (int i = 0; i < count; i++)
            {
                bytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            }

            return bytes;
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
    }
}
