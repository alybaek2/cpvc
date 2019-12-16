using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public class MachineFile2
    {
        // Using bytes here limits us to 256 possible block types.... could reserve most significant bit and do some kind of variable length encoding if we really need to.
        public const byte _idName = 0;
        public const byte _idKey = 1;
        public const byte _idReset = 2;
        public const byte _idLoadDisc = 3;
        public const byte _idLoadTape = 4;
        public const byte _idCheckpoint = 5;
        public const byte _idDeleteEvent = 6;
        public const byte _idCurrent = 7;
        public const byte _idBookmark = 8;


        private System.IO.FileStream _fileStream;


        public MachineFile2(string filepath)
        {
            _fileStream = System.IO.File.Open(filepath, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.ReadWrite, System.IO.FileShare.None);
        }

        public void WriteHistoryEvent(HistoryEvent historyEvent)
        {

        }


        private void WriteKey(int id, UInt64 ticks, byte keyCode, bool keyDown)
        {
            Write(_idKey, id, ticks, keyCode, keyDown);
        }

        private void WriteReset(int id, UInt64 ticks, byte keyCode, bool keyDown)
        {
            Write(_idReset, id, ticks);
        }

        private void WriteLoadDisc(int id, UInt64 ticks, byte drive, byte[] media)
        {
            Write(_idLoadDisc, id, ticks, drive, media);
        }

        private void WriteLoadTape(int id, UInt64 ticks, byte[] media)
        {
            Write(_idLoadTape, id, ticks, media);
        }

        private void WriteCheckpoint(int id, UInt64 ticks, Bookmark bookmark)
        {
            if (bookmark == null)
            {
                Write(_idCheckpoint, id, ticks, false);
            }
            else
            {
                Write(_idCheckpoint, id, ticks, true, bookmark.System, bookmark.State);
            }
        }

        private void WriteDelete(int id)
        {
            Write(_idDeleteEvent, id);
        }

        private void WriteCurrent(int id)
        {
            Write(_idCurrent, id);
        }

        private void WriteBookmark(int id, Bookmark bookmark)
        {
            if (bookmark == null)
            {
                Write(_idCheckpoint, id, false);
            }
            else
            {
                Write(_idCheckpoint, id, true, bookmark.System, bookmark.State);
            }
        }

        private void Write(params object[] args)
        {
            foreach (object arg in args)
            {
                switch (arg)
                {
                    case bool b:
                        Write(b);
                        break;
                    case byte b:
                        Write(b);
                        break;
                    case Int32 i:
                        Write(i);
                        break;
                    case UInt64 u:
                        Write(u);
                        break;
                    case Byte[] b:
                        Write(b);
                        break;
                }
            }
        }

        private void Write(byte b)
        {
            _fileStream.WriteByte(b);
        }

        private void Write(bool b)
        {
            _fileStream.WriteByte(b ? ((byte)0x01) : ((byte)0x00));
        }

        private void Write(Int32 i)
        {
            byte[] bytes = BitConverter.GetBytes(i);
            _fileStream.Write(bytes, 0, 4);
        }

        private void Write(UInt64 u)
        {
            byte[] bytes = BitConverter.GetBytes(u);
            _fileStream.Write(bytes, 0, 8);
        }

        private void Write(byte[] b)
        {
            Write(b.Length);
            _fileStream.Write(b, 0, b.Length);
        }
    }
}
