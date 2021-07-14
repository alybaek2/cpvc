using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CPvC.Test
{
    public class BinaryFileTests
    {
        static object[] BoolCases =
        {
            new object[] { false, new byte[] { 0x00 } },
            new object[] { true, new byte[] { 0x01 } }
        };

        static object[] ByteCases =
        {
            new object[] { (byte)0, new byte[] { 0x00 } },
            new object[] { (byte)1, new byte[] { 0x01 } },
            new object[] { (byte)255, new byte[] { 0xff } }
        };

        static object[] Int32Cases =
        {
            new object[] { 0, new byte[] { 0x00, 0x00, 0x00, 0x00 } },
            new object[] { 1, new byte[] { 0x01, 0x00, 0x00, 0x00 } },
            new object[] { 0x12345678, new byte[] { 0x78, 0x56, 0x34, 0x12 } },
            new object[] { -1, new byte[] { 0xff, 0xff, 0xff, 0xff } }
        };

        static object[] UInt64Cases =
        {
            new object[] { (UInt64) 0, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 } },
            new object[] { (UInt64) 1, new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 } },
            new object[] { (UInt64) 0x0123456789abcdef, new byte[] { 0xef, 0xcd, 0xab, 0x89, 0x67, 0x45, 0x23, 0x01 } },
            new object[] { (UInt64) 0xffffffffffffffff, new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff } }
        };

        static object[] VariableLengthByteArrayCases =
        {
            new object[] { new byte[] { }, new byte[] { 0x00, 0x00, 0x00, 0x00 } },
            new object[] { new byte[] { 0x34 }, new byte[] { 0x01, 0x00, 0x00, 0x00, 0x34 } },
            new object[] { new byte[] { 0x34, 0x56 }, new byte[] { 0x02, 0x00, 0x00, 0x00, 0x34, 0x56 } }
        };

        static object[] StringCases =
        {
            new object[] { "", new byte[] { 0x00, 0x00, 0x00, 0x00 } },
            new object[] { "abc", new byte[] { 0x03, 0x00, 0x00, 0x00, 0x61, 0x62, 0x63 } }
        };

        static object[] BytesBlobCases =
        {
            new object[] { null, new byte[] { 0x00 } },
            new object[] { new byte[] { }, new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00 } },
            new object[] { new byte[] { 0x34 }, new byte[] { 0x01, 0x01, 0x00, 0x00, 0x00, 0x34 } },
            new object[] { new byte[] { 0x34, 0x56 }, new byte[] { 0x01, 0x02, 0x00, 0x00, 0x00, 0x34, 0x56 } }
        };

        static object[] DiffBlobCases =
        {
            new object[] { null, new byte[] { 0x00 } },
            new object[] { new byte[] { }, new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00 } },
            new object[] { new byte[] { 0x34 }, new byte[] { 0x01, 0x01, 0x00, 0x00, 0x00, 0x34 } },
            new object[] { new byte[] { 0x34, 0x56 }, new byte[] { 0x01, 0x02, 0x00, 0x00, 0x00, 0x34, 0x56 } }
        };

        [TestCaseSource(nameof(ByteCases))]
        public void ReadByte(int expected, byte[] content)
        {
            // Setup
            MockFileByteStream mock = new MockFileByteStream();
            BinaryFile file = new BinaryFile(mock.Object);
            mock.Content = content.ToList();

            // Act
            int b = file.ReadByte();

            // Verify
            Assert.AreEqual(expected, b);
        }

        [TestCaseSource(nameof(BoolCases))]
        public void WriteBool(bool b, byte[] expected)
        {
            // Setup
            MockFileByteStream mock = new MockFileByteStream();
            BinaryFile file = new BinaryFile(mock.Object);

            // Act
            file.WriteBool(b);

            // Verify
            Assert.AreEqual(expected, mock.Content);
        }

        [TestCaseSource(nameof(BoolCases))]
        public void ReadBool(bool expected, byte[] content)
        {
            // Setup
            MockFileByteStream mock = new MockFileByteStream();
            BinaryFile file = new BinaryFile(mock.Object);
            mock.Content = content.ToList();

            // Act
            bool b = file.ReadBool();

            // Verify
            Assert.AreEqual(expected, b);
        }

        [TestCaseSource(nameof(ByteCases))]
        public void WriteByte(byte b, byte[] expected)
        {
            // Setup
            MockFileByteStream mock = new MockFileByteStream();
            BinaryFile file = new BinaryFile(mock.Object);

            // Act
            file.WriteByte(b);

            // Verify
            Assert.AreEqual(expected, mock.Content);
        }

        [TestCaseSource(nameof(Int32Cases))]
        public void ReadInt32(int expected, byte[] content)
        {
            // Setup
            MockFileByteStream mock = new MockFileByteStream();
            BinaryFile file = new BinaryFile(mock.Object);
            mock.Content = content.ToList();

            // Act
            int i = file.ReadInt32();

            // Verify
            Assert.AreEqual(expected, i);
        }

        [TestCaseSource(nameof(Int32Cases))]
        public void WriteInt32(int i, byte[] expected)
        {
            // Setup
            MockFileByteStream mock = new MockFileByteStream();
            BinaryFile file = new BinaryFile(mock.Object);

            // Act
            file.WriteInt32(i);

            // Verify
            Assert.AreEqual(expected, mock.Content);
        }

        [TestCaseSource(nameof(UInt64Cases))]
        public void ReadInt64(UInt64 expected, byte[] content)
        {
            // Setup
            MockFileByteStream mock = new MockFileByteStream();
            BinaryFile file = new BinaryFile(mock.Object);
            mock.Content = content.ToList();

            // Act
            UInt64 u = file.ReadUInt64();

            // Verify
            Assert.AreEqual(expected, u);
        }

        [TestCaseSource(nameof(UInt64Cases))]
        public void WriteInt64(UInt64 u, byte[] expected)
        {
            // Setup
            MockFileByteStream mock = new MockFileByteStream();
            BinaryFile file = new BinaryFile(mock.Object);

            // Act
            file.WriteUInt64(u);

            // Verify
            Assert.AreEqual(expected, mock.Content);
        }

        [TestCaseSource(nameof(VariableLengthByteArrayCases))]
        public void ReadVariableLengthByteArray(byte[] expected, byte[] content)
        {
            // Setup
            MockFileByteStream mock = new MockFileByteStream();
            BinaryFile file = new BinaryFile(mock.Object);
            mock.Content = content.ToList();

            // Act
            byte[] array = file.ReadVariableLengthByteArray();

            // Verify
            Assert.AreEqual(expected, array);
        }

        [TestCaseSource(nameof(VariableLengthByteArrayCases))]
        public void WriteVariableLengthByteArray(byte[] array, byte[] expected)
        {
            // Setup
            MockFileByteStream mock = new MockFileByteStream();
            BinaryFile file = new BinaryFile(mock.Object);

            // Act
            file.WriteVariableLengthByteArray(array);

            // Verify
            Assert.AreEqual(expected, mock.Content);
        }

        [TestCaseSource(nameof(StringCases))]
        public void ReadString(string expected, byte[] content)
        {
            // Setup
            MockFileByteStream mock = new MockFileByteStream();
            BinaryFile file = new BinaryFile(mock.Object);
            mock.Content = content.ToList();

            // Act
            string str = file.ReadString();

            // Verify
            Assert.AreEqual(expected, str);
        }

        [TestCaseSource(nameof(StringCases))]
        public void WriteString(string str, byte[] expected)
        {
            // Setup
            MockFileByteStream mock = new MockFileByteStream();
            BinaryFile file = new BinaryFile(mock.Object);

            // Act
            file.WriteString(str);

            // Verify
            Assert.AreEqual(expected, mock.Content);
        }

        [TestCaseSource(nameof(BytesBlobCases))]
        public void ReadBytesBlob(byte[] expected, byte[] content)
        {
            // Setup
            MockFileByteStream mock = new MockFileByteStream();
            BinaryFile file = new BinaryFile(mock.Object);
            mock.Content = content.ToList();

            // Act
            IBlob blob = file.ReadBlob();
            byte[] bytes = blob.GetBytes();

            // Verify
            Assert.AreEqual(expected, bytes);
        }

        [TestCaseSource(nameof(BytesBlobCases))]
        public void WriteBytesBlob(byte[] array, byte[] expected)
        {
            // Setup
            MockFileByteStream mock = new MockFileByteStream();
            BinaryFile file = new BinaryFile(mock.Object);

            // Act
            file.WriteBytesBlob(array);

            // Verify
            Assert.AreEqual(expected, mock.Content);
        }

        [Test]
        public void WriteAndReadDiffBlob()
        {
            // Setup
            byte[] content =
            {
                0x01, 0x02, 0x00, 0x00, 0x00, 0x34, 0x56
            };

            byte[] newBytes =
            {
                0x67, 0x89
            };

            MockFileByteStream mock = new MockFileByteStream();
            BinaryFile file = new BinaryFile(mock.Object);
            mock.Content = content.ToList();
            mock.Position = 0;

            // Act
            IStreamBlob oldBlob = file.ReadBlob();

            file.WriteDiffBlob(oldBlob, newBytes);
            mock.Position = content.Length;

            IBlob blob = file.ReadBlob();
            byte[] bytes = blob.GetBytes();

            // Verify
            Assert.AreEqual(newBytes, bytes);
        }

        [Test]
        public void ReadPastEndArray()
        {
            // Setup
            byte[] content =
            {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07
            };

            MockFileByteStream mock = new MockFileByteStream();
            BinaryFile file = new BinaryFile(mock.Object);
            mock.Content = content.ToList();
            mock.Position = 0;

            // Act and Verify
            byte[] array = new byte[8];
            Assert.Throws<Exception>(() => file.ReadFixedLengthByteArray(8));
        }

        [Test]
        public void ReadPastEndByte()
        {
            // Setup
            MockFileByteStream mock = new MockFileByteStream();
            BinaryFile file = new BinaryFile(mock.Object);
            mock.Content = new List<byte>();
            mock.Position = 0;

            // Act and Verify
            Assert.Throws<Exception>(() => file.ReadByte());
        }

        [Test]
        public void WriteSmallestBlob()
        {
            // Setup
            MockFileByteStream mock = new MockFileByteStream();
            BinaryFile file = new BinaryFile(mock.Object);
            file.DiffsEnabled = true;
            mock.Content = new List<byte>();
            mock.Position = 0;
            byte[] array1 = new byte[1000];
            byte[] array2 = new byte[1000];
            for (int i = 0; i < array2.Length; i++)
            {
                array2[i] = (byte)(i % 256);
            }

            IStreamBlob bytesBlob = file.WriteBytesBlob(array2);
            IStreamBlob diffBlob1 = file.WriteDiffBlob(bytesBlob, array1);
            IStreamBlob diffBlob2 = file.WriteDiffBlob(bytesBlob, array2);

            // Act
            IStreamDiffBlob smallestBlob = file.WriteSmallestBlob(array2, diffBlob2) as IStreamDiffBlob;

            // Verify
            Assert.NotNull(smallestBlob);
            Assert.AreEqual(diffBlob2, smallestBlob.BaseBlob);
            mock.Position = 0;
            Assert.AreEqual(array2, file.ReadBlob().GetBytes());
        }

        [Test]
        public void ReadCorruptBlob()
        {
            // Setup
            MockFileByteStream mock = new MockFileByteStream();
            BinaryFile file = new BinaryFile(mock.Object);
            file.DiffsEnabled = true;
            mock.Content = new List<byte> { 99 }; // Blob type 99 is not valid!
            mock.Position = 0;

            // Act and Verify
            Assert.Throws<Exception>(() => file.ReadBlob());
        }
    }
}
