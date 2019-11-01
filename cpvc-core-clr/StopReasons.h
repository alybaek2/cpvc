#pragma once

#include "..\cpvc-core\Core.h"

namespace CPvC {
    public ref class StopReasons
    {
    public:
        static const byte None = stopNone;
        static const byte AudioOverrun = stopAudioOverrun;
        static const byte VSync = stopVSync;
    };
}


