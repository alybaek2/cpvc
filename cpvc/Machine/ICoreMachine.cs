﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public interface ICoreMachine
    {
        void AdvancePlayback(int samples);
        int ReadAudio(byte[] buffer, int offset, int samplesRequested);

        UInt64 Ticks { get; }
        bool Running { get; }
        byte Volume { get; set;  }

        string Filepath { get; }
    }
}