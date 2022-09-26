using System;
using System.Collections.Generic;

namespace CPvC
{
    public interface IMachineAction
    {
        UInt64 Ticks { get; }
    }

    public class ResetAction : ResetRequest, IMachineAction
    {
        public ResetAction(UInt64 ticks)
        {
            Ticks = ticks;
        }

        public UInt64 Ticks { get; }
    }

    public class KeyPressAction : KeyPressRequest, IMachineAction
    {
        public KeyPressAction(UInt64 ticks, byte keyCode, bool keyDown) : base(keyCode, keyDown)
        {
            Ticks = ticks;
        }

        public UInt64 Ticks { get; }
    }

    public class LoadDiscAction : LoadDiscRequest, IMachineAction
    {
        public LoadDiscAction(UInt64 ticks, byte drive, IBlob mediaBuffer) : base(drive, mediaBuffer)
        {
            Ticks = ticks;
        }

        public UInt64 Ticks { get; }
    }

    public class LoadTapeAction : LoadTapeRequest, IMachineAction
    {
        public LoadTapeAction(UInt64 ticks, IBlob mediaBuffer) : base(mediaBuffer)
        {
            Ticks = ticks;
        }

        public UInt64 Ticks { get; }
    }

    public class CoreVersionAction : CoreVersionRequest, IMachineAction
    {
        public CoreVersionAction(UInt64 ticks, int version) : base(version)
        {
            Ticks = ticks;
        }

        public UInt64 Ticks { get; }
    }

    public class RunUntilAction : RunUntilRequest, IMachineAction
    {
        public RunUntilAction(UInt64 ticks, UInt64 stopTicks, List<UInt16> audioSamples) : base(stopTicks)
        {
            Ticks = ticks;
            AudioSamples = audioSamples;
        }

        public UInt64 Ticks { get; }
    }

    public class LoadCoreAction : LoadCoreRequest, IMachineAction
    {
        public LoadCoreAction(UInt64 ticks, IBlob state, IBlob screen) : base(state, screen)
        {
            Ticks = ticks;
        }

        public UInt64 Ticks { get; }
    }

    public class CreateSnapshotAction : CreateSnapshotRequest, IMachineAction
    {
        public CreateSnapshotAction(UInt64 ticks, int snapshotId) : base(snapshotId)
        {
            Ticks = ticks;
        }

        public UInt64 Ticks { get; }
    }

    public class DeleteSnapshotAction : DeleteSnapshotRequest, IMachineAction
    {
        public DeleteSnapshotAction(UInt64 ticks, int snapshotId) : base(snapshotId)
        {
            Ticks = ticks;
        }

        public UInt64 Ticks { get; }
    }

    public class RevertToSnapshotAction : RevertToSnapshotRequest, IMachineAction
    {
        public RevertToSnapshotAction(UInt64 ticks, int snapshotId) : base(snapshotId)
        {
            Ticks = ticks;
        }

        public UInt64 Ticks { get; }
    }

    /// <summary>
    /// Represents an action taken by the core thread in response to a request.
    /// </summary>
    static public class MachineAction
    {
        static public ResetAction Reset(UInt64 ticks)
        {
            return new ResetAction(ticks);
        }

        static public KeyPressAction KeyPress(UInt64 ticks, byte keycode, bool down)
        {
            return new KeyPressAction(ticks, keycode, down);
        }

        static public RunUntilAction RunUntil(UInt64 ticks, UInt64 stopTicks, List<UInt16> audioSamples)
        {
            return new RunUntilAction(ticks, stopTicks, audioSamples);
        }

        static public LoadDiscAction LoadDisc(UInt64 ticks, byte drive, IBlob disc)
        {
            return new LoadDiscAction(ticks, drive, disc);
        }

        static public LoadTapeAction LoadTape(UInt64 ticks, IBlob tape)
        {
            return new LoadTapeAction(ticks, tape);
        }

        static public LoadCoreAction LoadCore(UInt64 ticks, IBlob state)
        {
            return new LoadCoreAction(ticks, state, null);
        }

        static public CreateSnapshotAction CreateSnapshot(UInt64 ticks, int id)
        {
            return new CreateSnapshotAction(ticks, id);
        }

        static public DeleteSnapshotAction DeleteSnapshot(UInt64 ticks, int id)
        {
            return new DeleteSnapshotAction(ticks, id);
        }

        static public RevertToSnapshotAction RevertToSnapshot(UInt64 ticks, int id)
        {
            return new RevertToSnapshotAction(ticks, id);
        }

        static public CoreVersionAction CoreVersion(UInt64 ticks, int version)
        {
            return new CoreVersionAction(ticks, version);
        }

        static public IMachineAction Clone(IMachineAction action)
        {
            switch (action)
            {
                case CoreVersionAction coreVersionAction:
                    return new CoreVersionAction(coreVersionAction.Ticks, coreVersionAction.Version);
                case KeyPressAction keyPressAction:
                    return new KeyPressAction(keyPressAction.Ticks, keyPressAction.KeyCode, keyPressAction.KeyDown);
                case LoadDiscAction loadDiscAction:
                    return new LoadDiscAction(loadDiscAction.Ticks, loadDiscAction.Drive, MemoryBlob.Create(loadDiscAction.MediaBuffer));
                case LoadTapeAction loadTapeAction:
                    return new LoadTapeAction(loadTapeAction.Ticks, MemoryBlob.Create(loadTapeAction.MediaBuffer));
                case ResetAction resetAction:
                    return new ResetAction(resetAction.Ticks);
                case RunUntilAction runUntilAction:
                    {
                        List<UInt16> samples = null;
                        if (runUntilAction.AudioSamples != null)
                        {
                            samples = new List<UInt16>(runUntilAction.AudioSamples);
                        }
                        return new RunUntilAction(runUntilAction.Ticks, runUntilAction.StopTicks, samples);
                    }
                case LoadCoreAction loadCoreAction:
                    return new LoadCoreAction(loadCoreAction.Ticks, loadCoreAction.State, loadCoreAction.Screen);
                case CreateSnapshotAction createSnapshotAction:
                    return new CreateSnapshotAction(createSnapshotAction.Ticks, createSnapshotAction.SnapshotId);
                case RevertToSnapshotAction revertToSnapshotAction:
                    return new RevertToSnapshotAction(revertToSnapshotAction.Ticks, revertToSnapshotAction.SnapshotId);
                default:
                    return null;
            }
        }
    }
}
