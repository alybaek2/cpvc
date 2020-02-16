using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public interface ITurboableMachine : IBaseMachine
    {
        void EnableTurbo(bool enabled);
    }
}
