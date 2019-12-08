using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CPvC
{
    public class FileSystem : IFileSystem
    {
        public FileSystem()
        {
        }

        public IFile OpenFile(string filepath)
        {
            return new File(filepath);
        }

        public void RenameFile(string oldFilename, string newFilename)
        {
            System.IO.File.Move(oldFilename, newFilename);
        }

        /// <summary>
        /// Renames "newFilepath" to "filepath" and deletes "filepath", but in a way that can be recovered from if a crash or power failure occurs
        /// at some point during the operation. Code would be needed to be written elsewhere to perform this recovery.
        /// </summary>
        /// <param name="filepath">File path of the original file.</param>
        /// <param name="newFilepath">Filepath of the new file.</param>
        public void ReplaceFile(string filepath, string newFilepath)
        {
            if (System.IO.File.Exists(filepath))
            {
                string oldFilepath = String.Format("{0}.old", filepath);

                System.IO.File.Move(filepath, oldFilepath);
                System.IO.File.Move(newFilepath, filepath);
                System.IO.File.Delete(oldFilepath);
            }
            else
            {
                System.IO.File.Move(newFilepath, filepath);
            }
        }

        public void DeleteFile(string filepath)
        {
            System.IO.File.Delete(filepath);
        }

        public byte[] ReadBytes(string filepath)
        {
            return System.IO.File.ReadAllBytes(filepath);
        }

        public bool Exists(string filepath)
        {
            return System.IO.File.Exists(filepath);
        }

        public Int64 FileLength(string filepath)
        {
            return new System.IO.FileInfo(filepath).Length;
        }

        public List<string> GetZipFileEntryNames(string filepath)
        {
            List<string> entries = new List<string>();

            using (ZipInputStream zipStream = new ZipInputStream(System.IO.File.OpenRead(filepath)))
            {
                ZipEntry entry;
                while ((entry = zipStream.GetNextEntry()) != null)
                {
                    entries.Add(entry.Name);
                }
            }

            return entries;
        }

        public byte[] GetZipFileEntry(string filepath, string entryName)
        {
            using (ZipInputStream zipStream = new ZipInputStream(System.IO.File.OpenRead(filepath)))
            {
                ZipEntry entry;
                while ((entry = zipStream.GetNextEntry()) != null)
                {
                    if (entryName.ToLower() != entry.Name.ToLower())
                    {
                        continue;
                    }

                    List<byte> bytes = new List<byte>();
                    int count = 1024;
                    byte[] temp = new byte[count];
                    while ((count = zipStream.Read(temp, 0, temp.Length)) > 0)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            bytes.Add(temp[i]);
                        }
                    }

                    return bytes.ToArray();
                }
            }

            return null;
        }

        /// <summary>
        /// Reads a text file.
        /// </summary>
        /// <param name="filepath">File path of text file to read.</param>
        /// <returns>An enumerator returning the lines of the text file.</returns>
        public IEnumerable<string> ReadLines(string filepath)
        {
            return System.IO.File.ReadLines(filepath);
        }

        /// <summary>
        /// Reads a text file in reverse.
        /// </summary>
        /// <param name="filepath">File path of text file to read.</param>
        /// <returns>An enumerator returning the lines of the text file in reverse.</returns>
        public IEnumerable<string> ReadLinesReverse(string filepath)
        {
            using (FileStream fs = System.IO.File.Open(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fs.Seek(0, SeekOrigin.End);

                byte[] buffer = new byte[1024];
                bool firstChar = true;

                StringBuilder sb = new StringBuilder();
                while (fs.Position > 0)
                {
                    int bytesToRead = (int)Math.Min(fs.Position, buffer.Length);

                    fs.Position -= bytesToRead;
                    fs.Read(buffer, 0, bytesToRead);
                    fs.Position -= bytesToRead;

                    for (int i = bytesToRead - 1; i >= 0; i--)
                    {
                        char c = (char)buffer[i];
                        bool isNewlineChar = (c == '\n');

                        // Don't include newline characters in returned strings, and don't return an empty string
                        // as the first line if the final character in the file is a newline.
                        if (!isNewlineChar)
                        {
                            firstChar = false;

                            if ((sb.Length > 0) || (c != '\r'))
                            {
                                sb.Append(c);
                            }
                        }
                        else if (!firstChar)
                        {
                            yield return Helpers.ReverseString(sb.ToString());

                            sb.Clear();
                        }
                    }
                }

                // Ensure that an empty file doesn't return any strings.
                if (!firstChar)
                {
                    yield return Helpers.ReverseString(sb.ToString());
                }
            }
        }

    }
}
