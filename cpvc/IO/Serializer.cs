using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        static public void CoreRequestToBytes(MemoryByteStream stream, CoreRequest request)
        {
            switch (request.Type)
            {
                case CoreRequest.Types.KeyPress:
                    stream.Write(_coreActionKeyPress);
                    stream.Write(request.KeyCode);
                    stream.Write(request.KeyDown);
                    break;
                case CoreRequest.Types.Reset:
                    stream.Write(_coreActionReset);
                    break;
                case CoreRequest.Types.LoadDisc:
                    stream.Write(_coreActionLoadDisc);
                    stream.Write(request.Drive);
                    stream.WriteArray(request.MediaBuffer.GetBytes());
                    break;
                case CoreRequest.Types.LoadTape:
                    stream.Write(_coreActionLoadTape);
                    stream.WriteArray(request.MediaBuffer.GetBytes());
                    break;
                case CoreRequest.Types.RunUntilForce:
                    stream.Write(_coreActionRunUntil);
                    stream.Write(request.StopTicks);
                    break;
                case CoreRequest.Types.LoadCore:
                    stream.Write(_coreActionLoadCore);
                    stream.WriteArray(request.CoreState.GetBytes());
                    break;
                case CoreRequest.Types.CoreVersion:
                    stream.Write(_coreActionCoreVersion);
                    stream.Write(request.Version);
                    break;
                default:
                    throw new Exception(String.Format("Unknown CoreRequest type {0}!", request.Type));
            }
        }

        static public void CoreActionToBytes(MemoryByteStream stream, CoreAction action)
        {
            switch (action.Type)
            {
                case CoreRequest.Types.KeyPress:
                    stream.Write(_coreActionKeyPress);
                    stream.Write(action.Ticks);
                    stream.Write(action.KeyCode);
                    stream.Write(action.KeyDown);
                    break;
                case CoreRequest.Types.Reset:
                    stream.Write(_coreActionReset);
                    stream.Write(action.Ticks);
                    break;
                case CoreRequest.Types.LoadDisc:
                    stream.Write(_coreActionLoadDisc);
                    stream.Write(action.Ticks);
                    stream.Write(action.Drive);
                    stream.WriteArray(action.MediaBuffer.GetBytes());
                    break;
                case CoreRequest.Types.LoadTape:
                    stream.Write(_coreActionLoadTape);
                    stream.Write(action.Ticks);
                    stream.WriteArray(action.MediaBuffer.GetBytes());
                    break;
                case CoreRequest.Types.RunUntilForce:
                    stream.Write(_coreActionRunUntil);
                    stream.Write(action.Ticks);
                    stream.Write(action.StopTicks);
                    break;
                case CoreRequest.Types.LoadCore:
                    stream.Write(_coreActionLoadCore);
                    stream.Write(action.Ticks);
                    stream.WriteArray(action.CoreState.GetBytes());
                    break;
                case CoreRequest.Types.CoreVersion:
                    stream.Write(_coreActionCoreVersion);
                    stream.Write(action.Ticks);
                    stream.Write(action.Version);
                    break;
                default:
                    throw new Exception(String.Format("Unknown CoreAction type {0}!", action.Type));
            }
        }

        static public CoreAction CoreActionFromBytes(MemoryByteStream stream)
        {
            byte type = stream.ReadByte();
            switch (type)
            {
                case _coreActionKeyPress:
                    {
                        UInt64 ticks = stream.ReadUInt64();
                        byte keyCode = stream.ReadByte();
                        bool keyDown = stream.ReadBool();

                        return CoreAction.KeyPress(ticks, keyCode, keyDown);
                    }
                case _coreActionReset:
                    {
                        UInt64 ticks = stream.ReadUInt64();

                        return CoreAction.Reset(ticks);
                    }
                case _coreActionLoadDisc:
                    {
                        UInt64 ticks = stream.ReadUInt64();
                        byte drive = stream.ReadByte();
                        byte[] media = stream.ReadArray();

                        return CoreAction.LoadDisc(ticks, drive, new MemoryBlob(media));
                    }
                case _coreActionLoadTape:
                    {
                        UInt64 ticks = stream.ReadUInt64();
                        byte[] media = stream.ReadArray();

                        return CoreAction.LoadTape(ticks, new MemoryBlob(media));
                    }
                case _coreActionRunUntil:
                    {
                        UInt64 ticks = stream.ReadUInt64();
                        UInt64 stopTicks = stream.ReadUInt64();

                        return CoreAction.RunUntilForce(ticks, stopTicks);
                    }
                case _coreActionLoadCore:
                    {
                        UInt64 ticks = stream.ReadUInt64();
                        byte[] state = stream.ReadArray();

                        return CoreAction.LoadCore(ticks, new MemoryBlob(state));
                    }
                case _coreActionCoreVersion:
                    {
                        UInt64 ticks = stream.ReadUInt64();
                        Int32 version = stream.ReadInt32();

                        return CoreAction.CoreVersion(ticks, version);
                    }
                default:
                    throw new Exception(String.Format("Unknown CoreAction type {0}!", type));
            }
        }

        static public CoreRequest CoreRequestFromBytes(MemoryByteStream stream)
        {
            byte type = stream.ReadByte();
            switch (type)
            {
                case _coreActionKeyPress:
                    {
                        byte keyCode = stream.ReadByte();
                        bool keyDown = stream.ReadBool();

                        return CoreRequest.KeyPress(keyCode, keyDown);
                    }
                case _coreActionReset:
                    {
                        return CoreRequest.Reset();
                    }
                case _coreActionLoadDisc:
                    {
                        byte drive = stream.ReadByte();
                        byte[] media = stream.ReadArray();

                        return CoreRequest.LoadDisc(drive, media);
                    }
                case _coreActionLoadTape:
                    {
                        byte[] media = stream.ReadArray();

                        return CoreRequest.LoadTape(media);
                    }
                case _coreActionRunUntil:
                    {
                        UInt64 stopTicks = stream.ReadUInt64();

                        return CoreRequest.RunUntilForce(stopTicks);
                    }
                case _coreActionLoadCore:
                    {
                        byte[] state = stream.ReadArray();

                        return CoreRequest.LoadCore(new MemoryBlob(state));
                    }
                case _coreActionCoreVersion:
                    {
                        Int32 version = stream.ReadInt32();

                        return CoreRequest.CoreVersion(version);
                    }
                default:
                    throw new Exception(String.Format("Unknown CoreRequest type {0}!", type));
            }
        }
    }
}
