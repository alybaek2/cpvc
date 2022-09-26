using System;
using System.Collections.Generic;
using System.Linq;

namespace CPvC
{
    public class Serializer
    {
        public const byte _coreActionKeyPress = 1;
        public const byte _coreActionReset = 2;
        public const byte _coreActionLoadDisc = 3;
        public const byte _coreActionLoadTape = 4;
        public const byte _coreActionRunUntil = 5;
        public const byte _coreActionLoadCore = 6;
        public const byte _coreActionCoreVersion = 7;
        public const byte _coreActionCreateSnapshot = 8;
        public const byte _coreActionDeleteSnapshot = 9;
        public const byte _coreActionRevertToSnapshot = 10;

        static public void SelectMachineToBytes(MemoryByteStream stream, string machineName)
        {
            stream.Write(machineName);
        }

        static public string SelectMachineFromBytes(MemoryByteStream stream)
        {
            string machineName = stream.ReadString();

            return machineName;
        }

        static public void AvailableMachinesToBytes(MemoryByteStream stream, IEnumerable<string> machines)
        {
            int count = machines.Count();
            stream.Write(count);

            for (int i = 0; i < count; i++)
            {
                stream.Write(machines.ElementAt(i));
            }
        }

        static public List<string> AvailableMachinesFromBytes(MemoryByteStream stream)
        {
            List<string> availableMachines = new List<string>();

            int count = stream.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                availableMachines.Add(stream.ReadString());
            }

            return availableMachines;
        }

        static public void CoreRequestToBytes(MemoryByteStream stream, MachineRequest request)
        {
            switch (request)
            {
                case KeyPressRequest keyPressRequest:
                    stream.Write(_coreActionKeyPress);
                    stream.Write(keyPressRequest.KeyCode);
                    stream.Write(keyPressRequest.KeyDown);
                    break;
                case ResetRequest resetPressRequest:
                    stream.Write(_coreActionReset);
                    break;
                case LoadDiscRequest loadDiscPressRequest:
                    stream.Write(_coreActionLoadDisc);
                    stream.Write(loadDiscPressRequest.Drive);
                    stream.WriteArray(loadDiscPressRequest.MediaBuffer.GetBytes());
                    break;
                case LoadTapeRequest loadTapeRequest:
                    stream.Write(_coreActionLoadTape);
                    stream.WriteArray(loadTapeRequest.MediaBuffer.GetBytes());
                    break;
                case RunUntilRequest runUntilRequest:
                    stream.Write(_coreActionRunUntil);
                    stream.Write(runUntilRequest.StopTicks);
                    break;
                case LoadCoreRequest loadCoreRequest:
                    stream.Write(_coreActionLoadCore);
                    stream.WriteArray(loadCoreRequest.State.GetBytes());
                    break;
                case CoreVersionRequest coreVersionRequest:
                    stream.Write(_coreActionCoreVersion);
                    stream.Write(coreVersionRequest.Version);
                    break;
                case CreateSnapshotRequest snapshotRequest:
                    stream.Write(_coreActionCreateSnapshot);
                    stream.Write(snapshotRequest.SnapshotId);
                    break;
                case DeleteSnapshotRequest snapshotRequest:
                    stream.Write(_coreActionDeleteSnapshot);
                    stream.Write(snapshotRequest.SnapshotId);
                    break;
                case RevertToSnapshotRequest snapshotRequest:
                    stream.Write(_coreActionRevertToSnapshot);
                    stream.Write(snapshotRequest.SnapshotId);
                    break;
                default:
                    throw new Exception(String.Format("Unknown CoreRequest type {0}!", request.GetType()));
            }
        }

        static public void CoreActionToBytes(MemoryByteStream stream, IMachineAction action)
        {
            switch (action)
            {
                case KeyPressAction keyPressAction:
                    stream.Write(_coreActionKeyPress);
                    stream.Write(keyPressAction.Ticks);
                    stream.Write(keyPressAction.KeyCode);
                    stream.Write(keyPressAction.KeyDown);
                    break;
                case ResetAction resetAction:
                    stream.Write(_coreActionReset);
                    stream.Write(resetAction.Ticks);
                    break;
                case LoadDiscAction loadDiscAction:
                    stream.Write(_coreActionLoadDisc);
                    stream.Write(loadDiscAction.Ticks);
                    stream.Write(loadDiscAction.Drive);
                    stream.WriteArray(loadDiscAction.MediaBuffer.GetBytes());
                    break;
                case LoadTapeAction loadTapeAction:
                    stream.Write(_coreActionLoadTape);
                    stream.Write(loadTapeAction.Ticks);
                    stream.WriteArray(loadTapeAction.MediaBuffer.GetBytes());
                    break;
                case RunUntilAction runUntilAction:
                    stream.Write(_coreActionRunUntil);
                    stream.Write(runUntilAction.Ticks);
                    stream.Write(runUntilAction.StopTicks);
                    break;
                case LoadCoreAction loadCoreAction:
                    stream.Write(_coreActionLoadCore);
                    stream.Write(loadCoreAction.Ticks);
                    stream.WriteArray(loadCoreAction.State.GetBytes());
                    break;
                case CoreVersionAction coreVersionAction:
                    stream.Write(_coreActionCoreVersion);
                    stream.Write(coreVersionAction.Ticks);
                    stream.Write(coreVersionAction.Version);
                    break;
                case CreateSnapshotAction createSnapshotAction:
                    stream.Write(_coreActionCreateSnapshot);
                    stream.Write(createSnapshotAction.Ticks);
                    stream.Write(createSnapshotAction.SnapshotId);
                    break;
                case DeleteSnapshotAction deleteSnapshotAction:
                    stream.Write(_coreActionDeleteSnapshot);
                    stream.Write(deleteSnapshotAction.Ticks);
                    stream.Write(deleteSnapshotAction.SnapshotId);
                    break;
                case RevertToSnapshotAction revertSnapshotAction:
                    stream.Write(_coreActionRevertToSnapshot);
                    stream.Write(revertSnapshotAction.Ticks);
                    stream.Write(revertSnapshotAction.SnapshotId);
                    break;
                default:
                    throw new Exception(String.Format("Unknown CoreAction type {0}!", action.GetType()));
            }
        }

        static public IMachineAction CoreActionFromBytes(MemoryByteStream stream)
        {
            byte type = stream.ReadByte();
            switch (type)
            {
                case _coreActionKeyPress:
                    {
                        UInt64 ticks = stream.ReadUInt64();
                        byte keyCode = stream.ReadByte();
                        bool keyDown = stream.ReadBool();

                        return new KeyPressAction(ticks, keyCode, keyDown);
                    }
                case _coreActionReset:
                    {
                        UInt64 ticks = stream.ReadUInt64();

                        return new ResetAction(ticks);
                    }
                case _coreActionLoadDisc:
                    {
                        UInt64 ticks = stream.ReadUInt64();
                        byte drive = stream.ReadByte();
                        byte[] media = stream.ReadArray();

                        return new LoadDiscAction(ticks, drive, MemoryBlob.Create(media));
                    }
                case _coreActionLoadTape:
                    {
                        UInt64 ticks = stream.ReadUInt64();
                        byte[] media = stream.ReadArray();

                        return new LoadTapeAction(ticks, MemoryBlob.Create(media));
                    }
                case _coreActionRunUntil:
                    {
                        UInt64 ticks = stream.ReadUInt64();
                        UInt64 stopTicks = stream.ReadUInt64();

                        return new RunUntilAction(ticks, stopTicks, null);
                    }
                case _coreActionLoadCore:
                    {
                        UInt64 ticks = stream.ReadUInt64();
                        byte[] state = stream.ReadArray();

                        return new LoadCoreAction(ticks, MemoryBlob.Create(state), null);
                    }
                case _coreActionCoreVersion:
                    {
                        UInt64 ticks = stream.ReadUInt64();
                        Int32 version = stream.ReadInt32();

                        return new CoreVersionAction(ticks, version);
                    }
                case _coreActionCreateSnapshot:
                    {
                        UInt64 ticks = stream.ReadUInt64();
                        Int32 snapshotId = stream.ReadInt32();

                        return new CreateSnapshotAction(ticks, snapshotId);
                    }
                case _coreActionDeleteSnapshot:
                    {
                        UInt64 ticks = stream.ReadUInt64();
                        Int32 snapshotId = stream.ReadInt32();

                        return new DeleteSnapshotAction(ticks, snapshotId);
                    }
                case _coreActionRevertToSnapshot:
                    {
                        UInt64 ticks = stream.ReadUInt64();
                        Int32 snapshotId = stream.ReadInt32();

                        return new RevertToSnapshotAction(ticks, snapshotId);
                    }
            }

            throw new Exception(String.Format("Unknown CoreAction type {0}!", type));
        }

        static public MachineRequest CoreRequestFromBytes(MemoryByteStream stream)
        {
            byte type = stream.ReadByte();
            switch (type)
            {
                case _coreActionKeyPress:
                    {
                        byte keyCode = stream.ReadByte();
                        bool keyDown = stream.ReadBool();

                        return new KeyPressRequest(keyCode, keyDown);
                    }
                case _coreActionReset:
                    {
                        return new ResetRequest();
                    }
                case _coreActionLoadDisc:
                    {
                        byte drive = stream.ReadByte();
                        byte[] media = stream.ReadArray();

                        return new LoadDiscRequest(drive, MemoryBlob.Create(media));
                    }
                case _coreActionLoadTape:
                    {
                        byte[] media = stream.ReadArray();

                        return new LoadTapeRequest(MemoryBlob.Create(media));
                    }
                case _coreActionRunUntil:
                    {
                        UInt64 stopTicks = stream.ReadUInt64();

                        return new RunUntilRequest(stopTicks);
                    }
                case _coreActionLoadCore:
                    {
                        byte[] state = stream.ReadArray();

                        return new LoadCoreRequest(MemoryBlob.Create(state), null);
                    }
                case _coreActionCoreVersion:
                    {
                        Int32 version = stream.ReadInt32();

                        return new CoreVersionRequest(version);
                    }
                case _coreActionCreateSnapshot:
                    {
                        Int32 snapshotId = stream.ReadInt32();

                        return new CreateSnapshotRequest(snapshotId);
                    }
                case _coreActionDeleteSnapshot:
                    {
                        Int32 snapshotId = stream.ReadInt32();

                        return new DeleteSnapshotRequest(snapshotId);
                    }
                case _coreActionRevertToSnapshot:
                    {
                        Int32 snapshotId = stream.ReadInt32();

                        return new RevertToSnapshotRequest(snapshotId);
                    }
                default:
                    throw new Exception(String.Format("Unknown CoreRequest type {0}!", type));
            }
        }
    }
}
