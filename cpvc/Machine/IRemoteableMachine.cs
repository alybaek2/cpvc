using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public interface IRemoteableMachine
    {
        void StartServer(UInt16 port);
        void StopServer();
    }
}
