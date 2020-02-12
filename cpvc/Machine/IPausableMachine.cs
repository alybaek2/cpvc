using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public interface IPausableMachine
    {
        void Start();
        void Stop();
        void ToggleRunning();
    }
}
