# Code details

## Solution structure

The CPvC solution, `cpvc.sln`, contains five projects as follows:

* `cpvc-core`: This C++ project implements the `Core` class, which virtualizes the CPC's hardware.
* `cpvc-core-clr`: A C++/CLI project providing a simple wrapper class, `CoreCLR`, that allows the `cpvc` project to access the `Core` class.
* `cpvc`: A C# project providing the UI for CPvC. This project should be set as the StartUp project in Visual Studio.
* `cpvc-core-test`: A C++ Google Test project containing unit tests for `cpvc-core`.
* `cpvc-test`: A C# NUnit test project containing unit tests for `cpvc`.

## cpvc-core

The `Core` class implemented by this project encapsulates the state of the CPC's hardware, including the Z80 CPU, Gate Array, PSG, PPI, etc. Interaction with this class is done via the following methods:

* `Ticks()`: Returns the number of ticks of the CPC's 4MHz clock that have elapsed since the core was created. Note that this doesn't refer to "real" time, but rather the "virtual" time that elapses as a result of calls to `RunUntil()`.
* `RunUntil(qword stopTicks, byte stopReason)`: Runs the core until the return value of `Ticks()` is equal to or greater than `stopTicks`. This method may stop earlier if one of the conditions specified by `stopReasons` is met.
* `Reset()`: Performs a soft reset of the core. Equivalent of performing a `Call 0` or pressing `Ctrl`+`Shift`+`Esc`.
* `KeyPress(byte keycode, bool down)`: Changes the state of a key on the keyboard. Note that `keycode` is a two-digit decimal number (first digit is the keyboard bit, the second is the keyboard line).
* `LoadDisc(byte drive, const byte* pBuffer, int size)`: Loads a floppy disc into the specified drive.
* `LoadTape(const byte* pBuffer, int size)`: Loads and begins playback of a cassette tape.
* `SetScreen(dword* pBuffer, word pitch, word height)`: Assigns a buffer to the core to be used for video rendering. Note that the core renders video in 32-bit RGB.
* `GetAudioBuffers(int numSamples, byte* (&pChannels)[3])`: Reads audio data from the core. Data from all three PSG channels is returned.
* `SetLowerRom(Mem16k& lowerRom)`: Sets the lower ROM for the core.
* `SetUpperRom(byte slot, Mem16k& rom)`: Sets an expansion ROM for the core.

Some important notes:

* Upon instantiation, a `Core` object does not have any of the lower or upper ROMs initialized. This means these ROMs must be set via calls to `SetLowerROM` and `SetUpperROM` prior to running the core.
* The core's internal audio buffer is of a fixed size, so calls to `RunUntil(...)` will prematurely stop (provided `stopReason` is set to an appropriate value) if that buffer becomes full. Calls to `GetAudioBuffers(...)` retrieve data from the buffer, effectively "draining" it and allowing the core to continue running. This is how the the core can be run at normal speed by the `cpvc` project, which makes calls in real-time to `GetAudioBuffers(...)` while continually attempting calls to `RunUntil(...)`.
* The core was implemented in C++ to allow for the best possible performance and lowest CPU usage.
* The core was not implmented in C++/CLI to allow for easier portability should this project ever be ported to a different platform (e.g. Linux).

## cpvc-core-clr

This project provides the `CoreCLR` class, which wraps the `Core` class so that the `cpvc` C# project can use it. Most methods in the `Core` class have a corresponding wrapper method in the `CoreCLR` class, with the following additions:

* `AdvancePlayback(int samples)`: Effectively the same as `GetAudioBuffers(...)`, but without actually returning the audio data. Used by `cpvc` to keep a background instance running.
* `GetState()`: Returns a buffer containing a serialized copy of the core.
* `LoadState(array<byte>^ state)`: Loads a serialized core.

## cpvc

The WPF UI project for CPvC. Important classes include:

* `Core`: A wrapper for the `CoreCLR` class, with the ability to run the core continually in a background thread and provide delegates for VSync events and auditing.
* `Machine`: Represents an instance of a CPvC virtual machine. In addition to running the core, this class also maintains the history of the machine in the form of a branching timeline. Bookmarks (essentially just snapshots) are organized within this timeline. Other events (such as key presses) are also captured, though not currently used.
