#include "gtest/gtest.h"
#include "../cpvc-core/FDC.h"
#include "helpers.h"

bytevector CreateTestDiskImage()
{
    StreamWriter writer;
    writer.WriteArray("EXTENDED CPC DSK File\r\nDisk-Info\r\n", 0x22);
    writer.WriteArray("\0\0\0\0\0\0\0\0\0\0\0\0\0", 0x0e);

    // Track count
    writer << (byte)0x01;

    // Side count
    writer << (byte)0x01;

    writer << '\0' << '\0';

    // Track size table - a single track of sie 0x100
    writer << (byte)0x01;

    // Pad out to 0x100
    while (writer.Size() < 0x100)
    {
        writer << '\0';
    }

    // First track
    size_t trackInfoOffset = writer.Size();
    writer.WriteArray("Track-Info\r\n", 0x0c);
    writer << '\0' << '\0' << '\0' << '\0';

    // Track information
    writer
        << (byte)0x00  // Track id
        << (byte)0x00  // Side
        << (byte)0x01  // Data rate
        << (byte)0x00  // Recording mode
        << (byte)0x10  // Sector size
        << (byte)0x01  // Sector count
        << (byte)0x80  // GAP3 length
        << (byte)0xe5; // Filler byte

    // Sector Information
    writer
        << (byte)0x00    // Track id
        << (byte)0x00    // Side
        << (byte)0xc1    // Sector id
        << (byte)0x10    // Size
        << (byte)0x00    // FDC register 1
        << (byte)0x00    // FDC register 2
        << (word)0x0010; // Data length

    // Padding
    while ((writer.Size() - trackInfoOffset) < 0x100)
    {
        writer << '\0';
    }

    // Sector data
    for (byte data = 0; data < 0x10; data++)
    {
        writer << data;
    }

    bytevector data;
    data.resize(writer.Size());
    writer.CopyTo(data.data(), data.size());

    return data;
}

TEST(FDCTests, ReadInitialMainStatusRegister) {
    // Setup
    FDC fdc;
    fdc.Init();

    // Act
    byte b = fdc.Read(0x0100);

    // Verify
    ASSERT_EQ(statusRequestMaster, b);
};

TEST(FDCTests, ReadSector) {
    // Setup
    byte readCommand[] = {
        cmdReadData,
        0,     // Drive
        0,     // Cylinder
        0,     // Head
        0xc1,  // Sector
        0x10,  // Number of bytes
        0,     // End of track
        0,     // Gap length
        0x10   // Data length
    };

    bytevector image = CreateTestDiskImage();
    Disk disk;
    disk.LoadDisk(image.data(), image.size());

    FDC fdc;
    fdc.Init();
    fdc._drives[0].Load(disk);

    // Execute read data command.
    for (byte b : readCommand)
    {
        fdc.Write(0x0101, b);
    }

    // Act
    for (byte b = 0; b < 0x10; b++)
    {
        for (byte t = 0; t < 27; t++)
        {
            fdc.Tick();
        }

        byte readData = fdc.Read(0x0101);

        // Verify
        ASSERT_EQ(b, readData);
    }
};
