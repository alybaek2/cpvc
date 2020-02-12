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

        static public CoreAction CoreVersion(UInt64 ticks, int version)
        {
            CoreAction action = new CoreAction(Types.CoreVersion, ticks)
            {
                Version = version
            };

            return action;
        }
    }
}
