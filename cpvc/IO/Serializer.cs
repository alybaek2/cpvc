using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public class Serializer
    {
        public const byte _idKeyPress = 1;
        public const byte _idReset = 2;
        public const byte _idLoadDisc = 3;
        public const byte _idLoadTape = 4;
        public const byte _idRunUntil = 5;
        public const byte _idLoadCore = 6;
        public const byte _idCoreVersion = 7;
        public const byte _idAvailableMachines = 8;
        public const byte _idSelectMachine = 9;

        static public void SelectMachineToBytes(MemoryByteStream stream, string machineName)
        {
            stream.Write(_idSelectMachine);
            stream.Write(machineName);
        }

        static public string SelectMachineFromBytes(MemoryByteStream stream)
        {
            byte id = stream.ReadOneByte(); // Should be _idSelectMachine
            string machineName = stream.ReadString();

            return machineName;
        }

        static public void AvailableMachinesToBytes(MemoryByteStream stream, IEnumerable<string> machines)
        {
            stream.Write(_idAvailableMachines);

            int count = machines.Count();
            stream.Write(count);

            for (int i = 0; i < count; i++)
            {
                stream.Write(machines.ElementAt(i));
            }
        }

        static public List<string> AvailableMachinesFromBytes(MemoryByteStream stream)
        {
            byte id = stream.ReadOneByte(); // Should be _idAvailableMachines

            List<string> availableMachines = new List<string>();

            int count = stream.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                availableMachines.Add(stream.ReadString());
            }

            return availableMachines;
        }

        static public void CoreActionToBytes(MemoryByteStream stream, CoreAction action)
        {
            switch (action.Type)
            {
                case CoreRequest.Types.KeyPress:
                    stream.Write(_idKeyPress);
                    stream.Write(action.Ticks);
                    stream.Write(action.KeyCode);
                    stream.Write(action.KeyDown);
                    break;
                case CoreRequest.Types.Reset:
                    stream.Write(_idReset);
                    stream.Write(action.Ticks);
                    break;
                case CoreRequest.Types.LoadDisc:
                    stream.Write(_idLoadDisc);
                    stream.Write(action.Ticks);
                    stream.Write(action.Drive);
                    stream.WriteArray(action.MediaBuffer.GetBytes());
                    break;
                case CoreRequest.Types.LoadTape:
                    stream.Write(_idLoadTape);
                    stream.Write(action.Ticks);
                    stream.WriteArray(action.MediaBuffer.GetBytes());
                    break;
                case CoreRequest.Types.RunUntilForce:
                    stream.Write(_idRunUntil);
                    stream.Write(action.Ticks);
                    stream.Write(action.StopTicks);
                    break;
                case CoreRequest.Types.LoadCore:
                    stream.Write(_idLoadCore);
                    stream.Write(action.Ticks);
                    stream.WriteArray(action.CoreState.GetBytes());
                    break;
                case CoreRequest.Types.CoreVersion:
                    stream.Write(_idCoreVersion);
                    stream.Write(action.Ticks);
                    stream.Write(action.Version);
                    break;
                default:
                    break;
            }
        }

        static public CoreAction CoreActionFromBytes(MemoryByteStream stream)
        {
            byte type = stream.ReadOneByte();
            switch (type)
            {
                case _idKeyPress:
                    {
                        UInt64 ticks = stream.ReadUInt64();
                        byte keyCode = stream.ReadOneByte();
                        bool keyDown = stream.ReadBool();

                        return CoreAction.KeyPress(ticks, keyCode, keyDown);
                    }
                case _idReset:
                    {
                        UInt64 ticks = stream.ReadUInt64();

                        return CoreAction.Reset(ticks);
                    }
                case _idLoadDisc:
                    {
                        UInt64 ticks = stream.ReadUInt64();
                        byte drive = stream.ReadOneByte();
                        byte[] media = stream.ReadArray();

                        return CoreAction.LoadDisc(ticks, drive, new MemoryBlob(media));
                    }
                case _idLoadTape:
                    {
                        UInt64 ticks = stream.ReadUInt64();
                        byte[] media = stream.ReadArray();

                        return CoreAction.LoadTape(ticks, new MemoryBlob(media));
                    }
                case _idRunUntil:
                    {
                        UInt64 ticks = stream.ReadUInt64();
                        UInt64 stopTicks = stream.ReadUInt64();

                        return CoreAction.RunUntilForce(ticks, stopTicks);
                    }
                case _idLoadCore:
                    {
                        UInt64 ticks = stream.ReadUInt64();
                        byte[] state = stream.ReadArray();

                        return CoreAction.LoadCore(ticks, new MemoryBlob(state));
                    }
                case _idCoreVersion:
                    {
                        UInt64 ticks = stream.ReadUInt64();
                        Int32 version = stream.ReadInt32();

                        return CoreAction.CoreVersion(ticks, version);
                    }
                default:
                    break;
            }

            // Should throw an exception?

            return null;
        }
    }
}
