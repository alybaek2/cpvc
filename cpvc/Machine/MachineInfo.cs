using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace CPvC
{
    /// <summary>
    /// Class exposing minimal information about a machine; used to represent a recently closed machine.
    /// </summary>
    public class MachineInfo
    {
        public string Name { get; }
        public string Filepath { get; }
        private WriteableBitmap _bitmap;

        public object Display
        {
            get
            {
                return new
                {
                    Bitmap = _bitmap
                };
            }
        }

        public MachineInfo(string name, string filepath, IFileSystem fileSystem)
        {
            Name = name;
            Filepath = filepath;

            if (fileSystem != null)
            {
                // This should be revisited at some point; loading the entire machine for one screen
                // isn't the most efficient way to do this...
                using (Machine machine = Machine.Open(filepath, fileSystem))
                {
                    _bitmap = machine.Display.ConvertToGreyscale();
                }
            }
        }

        public MachineInfo(Machine machine)
        {
            Name = machine.Name;
            Filepath = machine.Filepath;
            _bitmap = machine.Display.ConvertToGreyscale();
        }

        static public MachineInfo FromString(string str, IFileSystem fileSystem)
        {
            List<string> tokens = Helpers.SplitWithEscape(';', str);
            if (tokens.Count < 2)
            {
                return null;
            }

            // Since MachineInfo will try to load the machine to get the latest screen, assume that
            // any exception thrown indicates the machine wasn't loadable, and therefore shouldn't
            // be shown in the Home tab.
            try
            {
                return new MachineInfo(tokens[0], tokens[1], fileSystem);
            }
            catch
            {
                return null;
            }
        }

        public string AsString()
        {
            return Helpers.JoinWithEscape(';', new List<string> { Name, Filepath });
        }
    }
}
