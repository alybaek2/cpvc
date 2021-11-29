using Moq;
using System;
using System.Collections.Generic;

namespace CPvC.Test
{
    public class MockTextFile : ITextFile
    {
        private List<string> _lines;
        private int _readIndex;

        public MockTextFile()
        {
            _lines = new List<string>();
        }

        public void WriteLine(string line)
        {
            _lines.Add(line);
        }

        public string ReadLine()
        {
            if (_readIndex >= _lines.Count)
            {
                return null;
            }

            return _lines[_readIndex++];
        }

        public int LineCount()
        {
            return _lines.Count;
        }

        public void Dispose()
        {
        }
    }
}
