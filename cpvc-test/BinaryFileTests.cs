using NUnit.Framework;
using System;
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

        [TestCaseSource("ByteCases")]
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

        [TestCaseSource("BoolCases")]
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

        [TestCaseSource("BoolCases")]
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

        [TestCaseSource("ByteCases")]
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

        [TestCaseSource("Int32Cases")]
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

        [TestCaseSource("Int32Cases")]
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

        [TestCaseSource("UInt64Cases")]
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

        [TestCaseSource("UInt64Cases")]
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

        [TestCaseSource("VariableLengthByteArrayCases")]
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

        [TestCaseSource("VariableLengthByteArrayCases")]
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

        [TestCaseSource("StringCases")]
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

        [TestCaseSource("StringCases")]
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

        [TestCaseSource("BytesBlobCases")]
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

        [TestCaseSource("BytesBlobCases")]
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
    }
}
