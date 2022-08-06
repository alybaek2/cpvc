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
            switch (request.Type)
            {
                case MachineRequest.Types.KeyPress:
                    stream.Write(_coreActionKeyPress);
                    stream.Write(request.KeyCode);
                    stream.Write(request.KeyDown);
                    break;
                case MachineRequest.Types.Reset:
                    stream.Write(_coreActionReset);
                    break;
                case MachineRequest.Types.LoadDisc:
                    stream.Write(_coreActionLoadDisc);
                    stream.Write(request.Drive);
                    stream.WriteArray(request.MediaBuffer.GetBytes());
                    break;
                case MachineRequest.Types.LoadTape:
                    stream.Write(_coreActionLoadTape);
                    stream.WriteArray(request.MediaBuffer.GetBytes());
                    break;
                case MachineRequest.Types.RunUntil:
                    stream.Write(_coreActionRunUntil);
                    stream.Write(request.StopTicks);
                    break;
                case MachineRequest.Types.LoadCore:
                    stream.Write(_coreActionLoadCore);
                    stream.WriteArray(request.CoreState.GetBytes());
                    break;
                case MachineRequest.Types.CoreVersion:
                    stream.Write(_coreActionCoreVersion);
                    stream.Write(request.Version);
                    break;
                case MachineRequest.Types.CreateSnapshot:
                    stream.Write(_coreActionCreateSnapshot);
                    stream.Write(request.SnapshotId);
                    break;
                case MachineRequest.Types.DeleteSnapshot:
                    stream.Write(_coreActionDeleteSnapshot);
                    stream.Write(request.SnapshotId);
                    break;
                case MachineRequest.Types.RevertToSnapshot:
                    stream.Write(_coreActionRevertToSnapshot);
                    stream.Write(request.SnapshotId);
                    break;
                default:
                    throw new Exception(String.Format("Unknown CoreRequest type {0}!", request.Type));
            }
        }

        static public void CoreActionToBytes(MemoryByteStream stream, MachineAction action)
        {
            switch (action.Type)
            {
                case MachineRequest.Types.KeyPress:
                    stream.Write(_coreActionKeyPress);
                    stream.Write(action.Ticks);
                    stream.Write(action.KeyCode);
                    stream.Write(action.KeyDown);
                    break;
                case MachineRequest.Types.Reset:
                    stream.Write(_coreActionReset);
                    stream.Write(action.Ticks);
                    break;
                case MachineRequest.Types.LoadDisc:
                    stream.Write(_coreActionLoadDisc);
                    stream.Write(action.Ticks);
                    stream.Write(action.Drive);
                    stream.WriteArray(action.MediaBuffer.GetBytes());
                    break;
                case MachineRequest.Types.LoadTape:
                    stream.Write(_coreActionLoadTape);
                    stream.Write(action.Ticks);
                    stream.WriteArray(action.MediaBuffer.GetBytes());
                    break;
                case MachineRequest.Types.RunUntil:
                    stream.Write(_coreActionRunUntil);
                    stream.Write(action.Ticks);
                    stream.Write(action.StopTicks);
                    break;
                case MachineRequest.Types.LoadCore:
                    stream.Write(_coreActionLoadCore);
                    stream.Write(action.Ticks);
                    stream.WriteArray(action.CoreState.GetBytes());
                    break;
                case MachineRequest.Types.CoreVersion:
                    stream.Write(_coreActionCoreVersion);
                    stream.Write(action.Ticks);
                    stream.Write(action.Version);
                    break;
                case MachineRequest.Types.CreateSnapshot:
                    stream.Write(_coreActionCreateSnapshot);
                    stream.Write(action.Ticks);
                    stream.Write(action.SnapshotId);
                    break;
                case MachineRequest.Types.DeleteSnapshot:
                    stream.Write(_coreActionDeleteSnapshot);
                    stream.Write(action.Ticks);
                    stream.Write(action.SnapshotId);
                    break;
                case MachineRequest.Types.RevertToSnapshot:
                    stream.Write(_coreActionRevertToSnapshot);
                    stream.Write(action.Ticks);
                    stream.Write(action.SnapshotId);
                    break;
                default:
                    throw new Exception(String.Format("Unknown CoreAction type {0}!", action.Type));
            }
        }

        static public MachineAction CoreActionFromBytes(MemoryByteStream stream)
        {
            byte type = stream.ReadByte();
            switch (type)
            {
                case _coreActionKeyPress:
                    {
                        UInt64 ticks = stream.ReadUInt64();
                        byte keyCode = stream.ReadByte();
                        bool keyDown = stream.ReadBool();

                        return MachineAction.KeyPress(ticks, keyCode, keyDown);
                    }
                case _coreActionReset:
                    {
                        UInt64 ticks = stream.ReadUInt64();

                        return MachineAction.Reset(ticks);
                    }
                case _coreActionLoadDisc:
                    {
                        UInt64 ticks = stream.ReadUInt64();
                        byte drive = stream.ReadByte();
                        byte[] media = stream.ReadArray();

                        return MachineAction.LoadDisc(ticks, drive, new MemoryBlob(media));
                    }
                case _coreActionLoadTape:
                    {
                        UInt64 ticks = stream.ReadUInt64();
                        byte[] media = stream.ReadArray();

                        return MachineAction.LoadTape(ticks, new MemoryBlob(media));
                    }
                case _coreActionRunUntil:
                    {
                        UInt64 ticks = stream.ReadUInt64();
                        UInt64 stopTicks = stream.ReadUInt64();

                        return MachineAction.RunUntil(ticks, stopTicks, null);
                    }
                case _coreActionLoadCore:
                    {
                        UInt64 ticks = stream.ReadUInt64();
                        byte[] state = stream.ReadArray();

                        return MachineAction.LoadCore(ticks, new MemoryBlob(state));
                    }
                case _coreActionCoreVersion:
                    {
                        UInt64 ticks = stream.ReadUInt64();
                        Int32 version = stream.ReadInt32();

                        return MachineAction.CoreVersion(ticks, version);
                    }
                case _coreActionCreateSnapshot:
                    {
                        UInt64 ticks = stream.ReadUInt64();
                        Int32 snapshotId = stream.ReadInt32();

                        return MachineAction.CreateSnapshot(ticks, snapshotId);
                    }
                case _coreActionDeleteSnapshot:
                    {
                        UInt64 ticks = stream.ReadUInt64();
                        Int32 snapshotId = stream.ReadInt32();

                        return MachineAction.DeleteSnapshot(ticks, snapshotId);
                    }
                case _coreActionRevertToSnapshot:
                    {
                        UInt64 ticks = stream.ReadUInt64();
                        Int32 snapshotId = stream.ReadInt32();

                        return MachineAction.RevertToSnapshot(ticks, snapshotId);
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

                        return MachineRequest.KeyPress(keyCode, keyDown);
                    }
                case _coreActionReset:
                    {
                        return MachineRequest.Reset();
                    }
                case _coreActionLoadDisc:
                    {
                        byte drive = stream.ReadByte();
                        byte[] media = stream.ReadArray();

                        return MachineRequest.LoadDisc(drive, media);
                    }
                case _coreActionLoadTape:
                    {
                        byte[] media = stream.ReadArray();

                        return MachineRequest.LoadTape(media);
                    }
                case _coreActionRunUntil:
                    {
                        UInt64 stopTicks = stream.ReadUInt64();

                        return MachineRequest.RunUntil(stopTicks);
                    }
                case _coreActionLoadCore:
                    {
                        byte[] state = stream.ReadArray();

                        return MachineRequest.LoadCore(new MemoryBlob(state));
                    }
                case _coreActionCoreVersion:
                    {
                        Int32 version = stream.ReadInt32();

                        return MachineRequest.CoreVersion(version);
                    }
                case _coreActionCreateSnapshot:
                    {
                        Int32 snapshotId = stream.ReadInt32();

                        return MachineRequest.CreateSnapshot(snapshotId);
                    }
                case _coreActionDeleteSnapshot:
                    {
                        Int32 snapshotId = stream.ReadInt32();

                        return MachineRequest.DeleteSnapshot(snapshotId);
                    }
                case _coreActionRevertToSnapshot:
                    {
                        Int32 snapshotId = stream.ReadInt32();

                        return MachineRequest.RevertToSnapshot(snapshotId);
                    }
                default:
                    throw new Exception(String.Format("Unknown CoreRequest type {0}!", type));
            }
        }
    }
}
