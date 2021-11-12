using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public class CompactedMachineFile
    {
        private List<string> _nonBlobCommands;
        private List<string> _blobCommands;

        private int _nextBlobId;

        public CompactedMachineFile()
        {
            _nonBlobCommands = new List<string>();
            _blobCommands = new List<string>();

            _nextBlobId = 0;
        }

        static public void Write(string filepath, string name, MachineHistory history)
        {
            CompactedMachineFile machineFile = new CompactedMachineFile();
            history.Write(machineFile);
            machineFile.WriteName(name);
            machineFile.Save(filepath);
        }

        public void Save(string filepath)
        {
            if (_blobCommands.Count == 0)
            {
                System.IO.File.WriteAllLines(filepath, _nonBlobCommands);

                return;
            }

            // Create a "compound" command for all the blobs.
            string compoundCommand = MachineFile.CompoundCommand(_blobCommands, true);

            System.IO.File.WriteAllLines(filepath, new string[] { compoundCommand });
            System.IO.File.AppendAllLines(filepath, _nonBlobCommands);
        }

        public void WriteAddBookmark(int id, UInt64 ticks, Bookmark bookmark)
        {
            int stateBlobId = _nextBlobId++;
            _blobCommands.Add(MachineFile.BlobCommand(stateBlobId, bookmark.State.GetBytes()));

            int screenBlobId = _nextBlobId++;
            _blobCommands.Add(MachineFile.BlobCommand(screenBlobId, bookmark.Screen.GetBytes()));

            _nonBlobCommands.Add(MachineFile.AddBookmarkCommand(id, ticks, bookmark.System, bookmark.Version, stateBlobId, screenBlobId));
        }

        public void WriteCoreAction(int id, UInt64 ticks, CoreAction action)
        {
            switch (action.Type)
            {
                case CoreRequest.Types.KeyPress:
                    _nonBlobCommands.Add(MachineFile.KeyCommand(id, ticks, action.KeyCode, action.KeyDown));
                    break;
                case CoreRequest.Types.Reset:
                    _nonBlobCommands.Add(MachineFile.ResetCommand(id, ticks));
                    break;
                case CoreRequest.Types.LoadDisc:
                    {
                        int mediaBlobId = _nextBlobId++;
                        _blobCommands.Add(MachineFile.BlobCommand(mediaBlobId, action.MediaBuffer.GetBytes()));

                        _nonBlobCommands.Add(MachineFile.LoadDiscCommand(id, ticks, action.Drive, mediaBlobId));
                    }
                    break;
                case CoreRequest.Types.LoadTape:
                    {
                        int mediaBlobId = _nextBlobId++;
                        _blobCommands.Add(MachineFile.BlobCommand(mediaBlobId, action.MediaBuffer.GetBytes()));

                        _nonBlobCommands.Add(MachineFile.LoadTapeCommand(id, ticks, mediaBlobId));
                    }
                    break;
                case CoreRequest.Types.CoreVersion:
                    _nonBlobCommands.Add(MachineFile.VersionCommand(id, ticks, action.Version));
                    break;
                case CoreRequest.Types.RunUntil:
                    _nonBlobCommands.Add(MachineFile.RunCommand(id, ticks, action.StopTicks));
                    break;
                default:
                    throw new ArgumentException(String.Format("Unrecognized core action type {0}.", action.Type), "type");
            }
        }

        public void WriteDeleteEvent(int persistentId)
        {
            _nonBlobCommands.Add(MachineFile.DeleteEventCommand(persistentId));
        }

        public void WriteDeleteEventAndChildren(int persistentId)
        {
            _nonBlobCommands.Add(MachineFile.DeleteEventAndChildrenCommand(persistentId));
        }

        public void WriteCurrent(int persistentId)
        {
            _nonBlobCommands.Add(MachineFile.CurrentCommand(persistentId));
        }

        public void WriteCurrentRoot()
        {
            _nonBlobCommands.Add(MachineFile.CurrentRootCommand());
        }

        public void WriteName(string name)
        {
            _nonBlobCommands.Add(MachineFile.NameCommand(name));
        }

        public void WriteReset(int id, UInt64 ticks)
        {
            _nonBlobCommands.Add(MachineFile.ResetCommand(id, ticks));
        }

        public void WriteBlob(int blobId, byte[] blob)
        {
            _blobCommands.Add(MachineFile.BlobCommand(blobId, blob));
        }
    }
}
