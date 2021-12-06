using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public interface IPersistableMachine
    {
        bool Persist(IFileSystem fileSystem, string filepath);
        void OpenFromFile(IFileSystem fileSystem);
        string PersistantFilepath { get; }
        bool IsOpen { get; }
    }
}
