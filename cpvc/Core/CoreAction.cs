using System;

namespace CPvC
{
    /// <summary>
    /// Represents an action taken by the core thread in response to a request.
    /// </summary>
    public class CoreAction : CoreActionBase
    {
        /// <summary>
        /// The ticks at which the action took place.
        /// </summary>
        public UInt64 Ticks { get; }

        public CoreAction(Types type, UInt64 ticks) : base(type)
        {
            Ticks = ticks;
        }

        static public CoreAction Reset(UInt64 ticks)
        {
            CoreAction request = new CoreAction(Types.Reset, ticks);

            return request;
        }

        static public CoreAction KeyPress(UInt64 ticks, byte keycode, bool down)
        {
            CoreAction action = new CoreAction(Types.KeyPress, ticks)
            {
                KeyCode = keycode,
                KeyDown = down
            };

            return action;
        }

        static public CoreAction RunUntil(UInt64 ticks, UInt64 stopTicks, byte stopReason)
        {
            CoreAction action = new CoreAction(Types.RunUntil, ticks)
            {
                StopTicks = stopTicks,
                StopReason = stopReason
            };

            return action;
        }
        
        static public CoreAction RunUntilForce(UInt64 ticks, UInt64 stopTicks)
        {
            CoreAction action = new CoreAction(Types.RunUntilForce, ticks)
            {
                StopTicks = stopTicks
            };

            return action;
        }

        static public CoreAction LoadDisc(UInt64 ticks, byte drive, IBlob disc)
        {
            CoreAction action = new CoreAction(Types.LoadDisc, ticks)
            {
                Drive = drive,
                MediaBuffer = disc
            };

            return action;
        }

        static public CoreAction LoadDisc(UInt64 ticks, byte drive, byte[] disc)
        {
            return LoadDisc(ticks, drive, new MemoryBlob(disc));
        }

        static public CoreAction LoadTape(UInt64 ticks, IBlob tape)
        {
            CoreAction action = new CoreAction(Types.LoadTape, ticks)
            {
                MediaBuffer = tape
            };

            return action;
        }

        static public CoreAction LoadTape(UInt64 ticks, byte[] tape)
        {
            return LoadTape(ticks, new MemoryBlob(tape));
        }

        static public CoreAction CoreVersion(UInt64 ticks, int version)
        {
            CoreAction action = new CoreAction(Types.CoreVersion, ticks)
            {
                Version = version
            };

            return action;
        }

        public CoreRequest AsRequest()
        {
            CoreRequest request = new CoreRequest(Type);

            switch (Type)
            {
                case Types.KeyPress:
                    return CoreRequest.KeyPress(KeyCode, KeyDown);
                case Types.LoadDisc:
                    return CoreRequest.LoadDisc(Drive, MediaBuffer.GetBytes());
                case Types.LoadTape:
                    return CoreRequest.LoadTape(MediaBuffer.GetBytes());
                case Types.Reset:
                    return CoreRequest.Reset();
                case Types.RunUntil:
                    return CoreRequest.RunUntil(StopTicks, StopReasons.None);
                case Types.CoreVersion:
                    return CoreRequest.SwitchVersion(Version);
            }

            return null;
        }
    }
}
