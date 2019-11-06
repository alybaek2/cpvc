using System;
using System.IO;

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
                buffer[c * 2 + 1] = (char)loNibble;
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
    }
}
