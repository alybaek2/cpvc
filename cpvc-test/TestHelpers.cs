using Moq;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace CPvC.Test
{
    static public class TestHelpers
    {
        /// <summary>
        /// Effectively a Times value representing 0 or more times.
        /// </summary>
        /// <returns>A Times value representing 0 or more times.</returns>
        static public Times TimesAny()
        {
            return Times.AtMost(int.MaxValue);
        }

        static public string AnyString()
        {
            return It.IsAny<string>();
        }

        /// <summary>
        /// Returns a lambda representing the invocation of the IFileSystem.ReadBytes method with a specific filename.
        /// </summary>
        static public Expression<Func<IFileSystem, byte[]>> ReadBytes(string filename)
        {
            return fileSystem => fileSystem.ReadBytes(filename);
        }

        /// <summary>
        /// Returns a lambda representing the invocation of the IFileSystem.ReadBytes method with any filename.
        /// </summary>
        static public Expression<Func<IFileSystem, byte[]>> ReadBytes()
        {
            return fileSystem => fileSystem.ReadBytes(AnyString());
        }

        /// <summary>
        /// Returns a lambda representing the invocation of the IFileSystem.DeleteFile method with a specific filename.
        /// </summary>
        static public Expression<Action<IFileSystem>> DeleteFile(string filename)
        {
            return fileSystem => fileSystem.DeleteFile(filename);
        }

        /// <summary>
        /// Returns a mock IFile which writes to a list of strings.
        /// </summary>
        /// <param name="lines">A list of strings the mock will write too.</param>
        /// <returns>A mock IFile which writes to the given list of strings.</returns>
        static public IFile MockFileWriter(List<string> lines)
        {
            Mock<IFile> mockWriter = new Mock<IFile>(MockBehavior.Strict);
            mockWriter.Setup(s => s.WriteLine(AnyString())).Callback<string>(line => lines.Add(line));
            mockWriter.Setup(s => s.Close());

            return mockWriter.Object;
        }
    }
}
