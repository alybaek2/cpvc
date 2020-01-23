using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public interface IBinaryFile
    {
        void WriteByte(byte b);
        void WriteBool(bool b);
        void WriteInt32(Int32 i);
        void WriteUInt64(UInt64 u);
        void WriteVariableLengthByteArray(byte[] b);
        void WriteString(string s);
        byte ReadByte();
        bool ReadBool();
        int ReadInt32();
        UInt64 ReadUInt64();
        byte[] ReadVariableLengthByteArray();
        string ReadString();
        byte[] ReadFixedLengthByteArray(int count);

        IStreamBlob WriteBytesBlob(byte[] bytes);
        IStreamBlob WriteDiffBlob(IStreamBlob oldBlob, byte[] newBytes);
        IStreamBlob ReadBlob();
    }
}
