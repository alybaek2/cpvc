using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace CPvC
{
    /// <summary>
    /// Class exposing minimal information about a machine; used in "Recently opened machine" on the home tab.
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

            return new MachineInfo(tokens[0], tokens[1], fileSystem);
        }

        public string AsString()
        {
            return Helpers.JoinWithEscape(';', new List<string> { Name, Filepath });
        }
    }
}
