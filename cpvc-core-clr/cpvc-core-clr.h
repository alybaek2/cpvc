#pragma once

#include "ICoreCLR.h"
#include "..\cpvc-core\Core.h"

using namespace System;

namespace CPvC {

    namespace CORE_NAMESPACE {

        public ref class CoreCLR : public ICoreCLR
        {
        private:
            Core * _pCore = new Core();

        public:
            CoreCLR()
            {
            }

            ~CoreCLR()
            {
                this->!CoreCLR();
            }

            !CoreCLR()
            {
                delete _pCore;
                _pCore = nullptr;
            }

            virtual void LoadLowerROM(array<byte>^ lowerRom)
            {
                if (lowerRom->Length != 0x4000)
                {
                    throw gcnew ArgumentException("Lower rom size is not 16384 bytes!");
                }

                pin_ptr<byte> pBuffer = &lowerRom[0];
                _pCore->SetLowerRom(Mem16k(pBuffer));
            }

            virtual void LoadUpperROM(byte slotIndex, array<byte>^ rom)
            {
                if (rom->Length != 0x4000)
                {
                    throw gcnew ArgumentException("Upper rom size is not 16384 bytes!");
                }

                pin_ptr<byte> pBuffer = &rom[0];
                _pCore->SetUpperRom(slotIndex, Mem16k(pBuffer));
            }

            virtual byte RunUntil(UInt64 stopTicks, byte stopReason)
            {
                return _pCore->RunUntil(stopTicks, stopReason);
            }

            virtual void Reset()
            {
                _pCore->Reset();
            }

            virtual void SetScreen(IntPtr pBuffer, UInt16 pitch, UInt16 height, UInt16 width)
            {
                _pCore->SetScreen((byte*)pBuffer.ToPointer(), pitch, height, width);
            }

            virtual bool KeyPress(byte keycode, bool down)
            {
                return _pCore->KeyPress(keycode, down);
            }

            virtual qword Ticks()
            {
                return _pCore->Ticks();
            }

            virtual void LoadTape(array<byte>^ tapeBuffer)
            {
                if (tapeBuffer == nullptr)
                {
                    _pCore->LoadTape(nullptr, 0);
                    return;
                }

                pin_ptr<byte> pTapeBuffer = &tapeBuffer[0];
                _pCore->LoadTape(pTapeBuffer, tapeBuffer->Length);
            }

            virtual void LoadDisc(byte drive, array<byte>^ discBuffer)
            {
                if (discBuffer == nullptr)
                {
                    _pCore->LoadDisc(drive, nullptr, 0);
                    return;
                }

                pin_ptr<byte> pDiscBuffer = &discBuffer[0];
                _pCore->LoadDisc(drive, pDiscBuffer, discBuffer->Length);
            }

            virtual int GetAudioBuffers(int samples, array<byte>^ channelA, array<byte>^ channelB, array<byte>^ channelC)
            {
                pin_ptr<byte> ppChannelA = &channelA[0];
                pin_ptr<byte> ppChannelB = &channelB[0];
                pin_ptr<byte> ppChannelC = &channelC[0];

                byte* ppChannels[3] = { ppChannelA, ppChannelB, ppChannelC };

                return _pCore->GetAudioBuffers(samples, ppChannels);
            }

            virtual void AdvancePlayback(int samples)
            {
                byte* pChannels[3] = { nullptr, nullptr, nullptr };
                _pCore->GetAudioBuffers(samples, pChannels);
            }

            virtual void AudioSampleFrequency(dword frequency)
            {
                _pCore->SetFrequency(frequency);
            }

            virtual array<byte>^ GetState()
            {
                StreamWriter s;
                s << (*_pCore);

                size_t size = s.Size();
                array<byte>^ state = gcnew array<byte>((int)size);
                pin_ptr<byte> ppState = &state[0];
                s.CopyTo(ppState, size);

                return state;
            }

            virtual void LoadState(array<byte>^ state)
            {
                StreamReader s;

                for (int i = 0; i < state->Length; i++)
                {
                    s.Push(state[i]);
                }

                s >> (*_pCore);
            }
        };
    }
}
