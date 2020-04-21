using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public interface IOpenableMachine
    {
        void Open();
        void Close();

        bool RequiresOpen { get; }
    }
}
