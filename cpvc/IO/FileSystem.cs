using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;

namespace CPvC
{
    public class FileSystem : IFileSystem
    {
        public FileSystem()
        {
        }

        public ITextFile OpenTextFile(string filepath)
        {
            System.IO.FileStream fileStream = System.IO.File.Open(filepath, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.ReadWrite, System.IO.FileShare.None);

            return new TextFile(fileStream);
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
    }
}
