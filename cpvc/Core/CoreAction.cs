using System;
using System.Collections.Generic;

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

        /// <summary>
        /// The set of audio samples that were generated during a RunUntil request.
        /// </summary>
        public List<UInt16> AudioSamples { get; private set; }

        /// <summary>
        /// For a create snapshot action, this indicates the id of the parent snapshot.
        /// </summary>
        //public int CreatedSnapshotId { get; private set; }

        public CoreAction(Types type, UInt64 ticks) : base(type)
        {
            Ticks = ticks;
            AudioSamples = new List<UInt16>();
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

        static public CoreAction RunUntil(UInt64 ticks, UInt64 stopTicks, List<UInt16> audioSamples)
        {
            CoreAction action = new CoreAction(Types.RunUntil, ticks)
            {
                StopTicks = stopTicks,
                AudioSamples = audioSamples
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

        static public CoreAction CreateSnapshot(UInt64 ticks, int id)
        {
            CoreAction action = new CoreAction(Types.CreateSnapshot, ticks)
            {
                SnapshotId = id
            };

            return action;
        }

        static public CoreAction DeleteSnapshot(UInt64 ticks, int id)
        {
            CoreAction action = new CoreAction(Types.DeleteSnapshot, ticks)
            {
                SnapshotId = id
            };

            return action;
        }

        static public CoreAction RevertToSnapshot(UInt64 ticks, int id)
        {
            CoreAction action = new CoreAction(Types.RevertToSnapshot, ticks)
            {
                SnapshotId = id
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
                case Types.Reset:
                    return CoreAction.Reset(Ticks);
                case Types.RunUntil:
                    {
                        List<UInt16> samples = null;
                        if (AudioSamples != null)
                        {
                            samples = new List<UInt16>(AudioSamples);
                        }
                        return CoreAction.RunUntil(Ticks, StopTicks, samples);
                    }
                case Types.LoadCore:
                    return CoreAction.LoadCore(Ticks, CoreState);
                case Types.CreateSnapshot:
                    return CoreAction.CreateSnapshot(Ticks, SnapshotId);
                case Types.RevertToSnapshot:
                    return CoreAction.RevertToSnapshot(Ticks, SnapshotId);
                default:
                    return null;
            }
        }

        public override string ToString()
        {
            return String.Format("{0} @ {1}", Type, Ticks);
        }
    }
}
