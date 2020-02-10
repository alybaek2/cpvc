using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public interface IMachine
    {
        void Start();
        void Stop();
        void ToggleRunning();
        void Reset();
        void Key(byte keycode, bool down);
        void LoadDisc(byte drive, byte[] diskBuffer);
        void LoadTape(byte[] tapeBuffer);
        void EnableTurbo(bool enabled);
        void Close();

        string Name { get; }
        Core Core { get; }

        string Filepath { get; }
    }
}
