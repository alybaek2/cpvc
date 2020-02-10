using System;

namespace CPvC
{
    /// <summary>
    /// Represents a request to the core thread.
    /// </summary>
    public class CoreRequest : CoreActionBase
    {
        public CoreRequest(Types type) : base(type)
        {
        }

        static public CoreRequest Reset()
        {
            CoreRequest request = new CoreRequest(Types.Reset);

            return request;
        }

        static public CoreRequest KeyPress(byte keycode, bool down)
        {
            CoreRequest request = new CoreRequest(Types.KeyPress)
            {
                KeyCode = keycode,
                KeyDown = down
            };

            return request;
        }

        static public CoreRequest RunUntil(UInt64 stopTicks, byte stopReason)
        {
            CoreRequest request = new CoreRequest(Types.RunUntil)
            {
                StopTicks = stopTicks,
                StopReason = stopReason
            };

            return request;
        }

        static public CoreRequest RunUntilForce(UInt64 stopTicks)
        {
            CoreRequest request = new CoreRequest(Types.RunUntilForce)
            {
                StopTicks = stopTicks
            };

            return request;
        }

        static public CoreRequest LoadDisc(byte drive, byte[] buffer)
        {
            CoreRequest request = new CoreRequest(Types.LoadDisc)
            {
                Drive = drive,
                MediaBuffer = new MemoryBlob((byte[])buffer?.Clone())
            };

            return request;
        }

        static public CoreRequest LoadTape(byte[] buffer)
        {
            CoreRequest request = new CoreRequest(Types.LoadTape)
            {
                MediaBuffer = new MemoryBlob((byte[])buffer?.Clone())
            };

            return request;
        }

        static public CoreRequest SwitchVersion(int version)
        {
            CoreRequest request = new CoreRequest(Types.CoreVersion)
            {
                Version = version
            };

            return request;
        }

        static public CoreRequest Quit()
        {
            return new CoreRequest(Types.Quit);
        }
    }
}
