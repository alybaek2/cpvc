using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;

namespace CPvC
{
    public class MachineRequest
    {
        public MachineRequest()
        {
            _processed = new ManualResetEvent(false);
        }

        public void SetProcessed()
        {
            _processed.Set();
        }

        public bool Wait(int timeout)
        {
            return _processed.WaitOne(timeout);
        }

        public bool Wait()
        {
            return _processed.WaitOne();
        }

        private ManualResetEvent _processed;
    }

    public class CoreRequest : MachineRequest { }

    public class ResetRequest : CoreRequest { }

    public class KeyPressRequest : CoreRequest
    {
        public KeyPressRequest(byte keyCode, bool keyDown)
        {
            KeyCode = keyCode;
            KeyDown = keyDown;
        }

        /// <summary>
        /// The key that has been pressed. Key codes are encoded as a two digit decimal number; the first digit is the key bit and the second is the key line.
        /// </summary>
        public byte KeyCode { get; protected set; }

        /// <summary>
        /// Indicates whether the key is in the down state. If false, the key is "up" (ie. not pressed).
        /// </summary>
        public bool KeyDown { get; protected set; }
    }

    public class LoadDiscRequest : CoreRequest
    {
        public LoadDiscRequest(byte drive, IBlob mediaBuffer)
        {
            Drive = drive;
            MediaBuffer = mediaBuffer;
        }

        /// <summary>
        /// Indicates the drive for LoadDisc; 0 is Drive A and 1 is Drive B.
        /// </summary>
        public byte Drive { get; protected set; }

        /// <summary>
        /// A buffer representing an uncompressed tape or disc image.
        /// </summary>
        public IBlob MediaBuffer { get; protected set; }
    }

    public class LoadTapeRequest : CoreRequest
    {
        public LoadTapeRequest(IBlob mediaBuffer)
        {
            MediaBuffer = mediaBuffer;
        }

        /// <summary>
        /// A buffer representing an uncompressed tape or disc image.
        /// </summary>
        public IBlob MediaBuffer { get; protected set; }
    }

    public class CoreVersionRequest : CoreRequest
    {
        public CoreVersionRequest(int version)
        {
            Version = version;
        }

        /// <summary>
        /// For an action, this indicates the version of the core to switch to.
        /// </summary>
        public int Version { get; protected set; }
    }

    public class RunUntilRequest : CoreRequest, INotifyPropertyChanged
    {
        public RunUntilRequest(UInt64 stopTicks)
        {
            StopTicks = stopTicks;
        }

        /// <summary>
        /// For a request, indicates the desired ticks to stop at. For an action represents the actual ticks value that the core stopped at.
        /// </summary>
        public UInt64 StopTicks
        {
            get
            {
                return _stopTicks;
            }

            set
            {
                if (_stopTicks == value)
                {
                    return;
                }

                _stopTicks = value;
                OnPropertyChanged();
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public List<UInt16> AudioSamples { get; protected set; }

        private UInt64 _stopTicks;

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class LoadCoreRequest : CoreRequest
    {
        public LoadCoreRequest(IBlob state, IBlob screen)
        {
            State = state;
            Screen = screen;
        }

        public IBlob State { get; protected set; }
        public IBlob Screen { get; protected set; }
    }

    public abstract class SnapshotRequest : CoreRequest
    {
        public SnapshotRequest(int snapshotId)
        {
            SnapshotId = snapshotId;
        }

        public int SnapshotId { get; protected set; }
    }

    public class CreateSnapshotRequest : SnapshotRequest
    {
        public CreateSnapshotRequest(int snapshotId) : base(snapshotId)
        {
        }
    }

    public class DeleteSnapshotRequest : SnapshotRequest
    {
        public DeleteSnapshotRequest(int snapshotId) : base(snapshotId)
        {
        }
    }

    public class RevertToSnapshotRequest : SnapshotRequest
    {
        public RevertToSnapshotRequest(int snapshotId) : base(snapshotId)
        {
        }
    }

    public class PauseRequest : MachineRequest { }

    public class ResumeRequest : MachineRequest { }

    public class ReverseRequest : MachineRequest { }

    public class LockRequest : MachineRequest { }

    public class UnlockRequest : MachineRequest { }
}
