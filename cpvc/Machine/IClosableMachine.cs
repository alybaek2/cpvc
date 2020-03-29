using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public delegate void OnCloseDelegate();

    public interface IClosableMachine
    {
        void Close();

        OnCloseDelegate OnClose { get; set; }
    }
}
