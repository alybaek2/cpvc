using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    interface IPersistableMachine
    {
        bool Persist(IFileSystem fileSystem, string filepath);
        string PersistantFilepath { get; }
        bool IsOpen { get; }
    }
}
