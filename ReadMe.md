# CPvC

An Amstrad CPC emulator written in C++ and C# for Windows.

## How is CPvC different from every other CPC emulator?

CPvC has additional features on top of the standard ones offered by most emulators:

* A more "persistent" emulator model, where one or more virtual CPC instances can be created and persist even after the application is closed. These instances can be loaded again and resume from where they left off.
* Snapshots (or "bookmarks" as they're called in CPvC) are organized as part of a branching timeline, instead of as separate .SNA files. 

## What is CPvC short for?

Colour Personal *Virtual* Computer.

## Which CPC models are supported?

Currently, only the 6128 model is emulated. Support for other models (464, 664, 464+, and 6128+) will be added in the future.

## How to build

Before building, the Amstrad CPC roms must be manually copied to the `roms` folder. See [ReadMe.md](roms/ReadMe.md) in the `roms` folder for more details.

Open the `cpvc.sln` solution file in Visual Studio (File -> Open -> Project/Solution...), select the solution configuration and platform (Build -> Configuration Manager...), then build the solution (Build -> Build Solution).

Note that CPvC is currently being developed in Visual Studio Community 2017 (version 15.5.7). It may be buildable in other versions of Visual Studio.

## How to run unit tests

In Visual Studio 2017, set the Default Processor Architecture to match the currently selected platform (x64 or x86). This option is under Test -> Test Settings -> Default Processor Architecture. Build the solution, then run all tests (Test -> Run -> All Tests).

## Running CPvC

Once built, launch the application (set the `cpvc` project as the startup project for the solution) and create a new instance (File -> New...), or open an existing one (File -> Open...).
