using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public interface IBaseMachine
    {
        void Close();

        string Name { get; }
        Core Core { get; }

        string Filepath { get; }
    }
}
