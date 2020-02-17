using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public interface IInteractiveMachine : ICoreMachine
    {
        void Reset();
        void Key(byte keycode, bool down);
        void LoadDisc(byte drive, byte[] diskBuffer);
        void LoadTape(byte[] tapeBuffer);
    }
}
