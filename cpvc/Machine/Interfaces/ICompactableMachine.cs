using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public interface ICompactableMachine : ICoreMachine
    {
        void Compact(bool enableDiffs);
    }
}
