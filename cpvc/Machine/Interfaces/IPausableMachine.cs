using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public interface IPausableMachine : ICoreMachine
    {
        void Start();
        void Stop();
        void ToggleRunning();
    }
}
