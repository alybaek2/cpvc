using System;

namespace CPvC
{
    /// <summary>
    /// Represents an action taken by the core thread in response to a request.
    /// </summary>
    public class CoreAction : CoreRequest
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
            return new CoreAction(Types.Reset, ticks);
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

        static public CoreAction LoadTape(UInt64 ticks, IBlob tape)
        {
            CoreAction action = new CoreAction(Types.LoadTape, ticks)
            {
                MediaBuffer = tape
            };

            return action;
        }

        static public CoreAction LoadCore(UInt64 ticks, IBlob state)
        {
            CoreAction action = new CoreAction(Types.LoadCore, ticks)
            {
                CoreState = state
            };

            return action;
        }

        static public CoreAction CoreVersion(UInt64 ticks, int version)
        {
            CoreAction action = new CoreAction(Types.CoreVersion, ticks)
            {
                Version = version
            };

            return action;
        }

        public CoreAction Clone()
        {
            switch (Type)
            {
                case Types.CoreVersion:
                    return CoreAction.CoreVersion(Ticks, Version);
                case Types.KeyPress:
                    return CoreAction.KeyPress(Ticks, KeyCode, KeyDown);
                case Types.LoadDisc:
                    return CoreAction.LoadDisc(Ticks, Drive, (MediaBuffer != null) ? (new MemoryBlob(MediaBuffer.GetBytes())) : null);
                case Types.LoadTape:
                    return CoreAction.LoadTape(Ticks, (MediaBuffer != null) ? (new MemoryBlob(MediaBuffer.GetBytes())) : null);
                case Types.Quit:
                    return new CoreAction(Types.Quit, Ticks);
                case Types.Reset:
                    return CoreAction.Reset(Ticks);
                case Types.RunUntilForce:
                    return CoreAction.RunUntilForce(Ticks, StopTicks);
                case Types.LoadCore:
                    return CoreAction.LoadCore(Ticks, CoreState);
                default:
                    return null;
            }
        }
    }
}
