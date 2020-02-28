using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public interface IPrerecordedMachine : ICoreMachine
    {
        void SeekToStart();
        void SeekToPreviousBookmark();
        void SeekToNextBookmark();
    }
}
