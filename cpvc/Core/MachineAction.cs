using System;
using System.Collections.Generic;

namespace CPvC
{
    /// <summary>
    /// Represents an action taken by the core thread in response to a request.
    /// </summary>
    public class MachineAction : MachineRequest
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

        public MachineAction(Types type, UInt64 ticks) : base(type)
        {
            Ticks = ticks;
            AudioSamples = new List<UInt16>();
        }

        static public MachineAction Reset(UInt64 ticks)
        {
            return new MachineAction(Types.Reset, ticks);
        }

        static public MachineAction KeyPress(UInt64 ticks, byte keycode, bool down)
        {
            MachineAction action = new MachineAction(Types.KeyPress, ticks)
            {
                KeyCode = keycode,
                KeyDown = down
            };

            return action;
        }

        static public MachineAction RunUntil(UInt64 ticks, UInt64 stopTicks, List<UInt16> audioSamples)
        {
            MachineAction action = new MachineAction(Types.RunUntil, ticks)
            {
                StopTicks = stopTicks,
                AudioSamples = audioSamples
            };

            return action;
        }

        static public MachineAction LoadDisc(UInt64 ticks, byte drive, IBlob disc)
        {
            MachineAction action = new MachineAction(Types.LoadDisc, ticks)
            {
                Drive = drive,
                MediaBuffer = disc
            };

            return action;
        }

        static public MachineAction LoadTape(UInt64 ticks, IBlob tape)
        {
            MachineAction action = new MachineAction(Types.LoadTape, ticks)
            {
                MediaBuffer = tape
            };

            return action;
        }

        static public MachineAction LoadCore(UInt64 ticks, IBlob state)
        {
            MachineAction action = new MachineAction(Types.LoadCore, ticks)
            {
                CoreState = state
            };

            return action;
        }

        static public MachineAction CreateSnapshot(UInt64 ticks, int id)
        {
            MachineAction action = new MachineAction(Types.CreateSnapshot, ticks)
            {
                SnapshotId = id
            };

            return action;
        }

        static public MachineAction DeleteSnapshot(UInt64 ticks, int id)
        {
            MachineAction action = new MachineAction(Types.DeleteSnapshot, ticks)
            {
                SnapshotId = id
            };

            return action;
        }

        static public MachineAction RevertToSnapshot(UInt64 ticks, int id)
        {
            MachineAction action = new MachineAction(Types.RevertToSnapshot, ticks)
            {
                SnapshotId = id
            };

            return action;
        }

        static public MachineAction CoreVersion(UInt64 ticks, int version)
        {
            MachineAction action = new MachineAction(Types.CoreVersion, ticks)
            {
                Version = version
            };

            return action;
        }

        public MachineAction Clone()
        {
            switch (Type)
            {
                case Types.CoreVersion:
                    return MachineAction.CoreVersion(Ticks, Version);
                case Types.KeyPress:
                    return MachineAction.KeyPress(Ticks, KeyCode, KeyDown);
                case Types.LoadDisc:
                    return MachineAction.LoadDisc(Ticks, Drive, (MediaBuffer != null) ? (new MemoryBlob(MediaBuffer.GetBytes())) : null);
                case Types.LoadTape:
                    return MachineAction.LoadTape(Ticks, (MediaBuffer != null) ? (new MemoryBlob(MediaBuffer.GetBytes())) : null);
                case Types.Reset:
                    return MachineAction.Reset(Ticks);
                case Types.RunUntil:
                    {
                        List<UInt16> samples = null;
                        if (AudioSamples != null)
                        {
                            samples = new List<UInt16>(AudioSamples);
                        }
                        return MachineAction.RunUntil(Ticks, StopTicks, samples);
                    }
                case Types.LoadCore:
                    return MachineAction.LoadCore(Ticks, CoreState);
                case Types.CreateSnapshot:
                    return MachineAction.CreateSnapshot(Ticks, SnapshotId);
                case Types.RevertToSnapshot:
                    return MachineAction.RevertToSnapshot(Ticks, SnapshotId);
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
