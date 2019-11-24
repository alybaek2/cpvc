# CPvC

[![Build status](https://badge.buildkite.com/d5ba19b44c23f47b75db350b462738bf1dc048311058e34b16.svg)](https://buildkite.com/cpvc/build-and-test)
[![CodeFactor](https://www.codefactor.io/repository/github/alybaek2/cpvc/badge/master)](https://www.codefactor.io/repository/github/alybaek2/cpvc/overview/master)
[![Coverage Status](https://coveralls.io/repos/github/alybaek2/cpvc/badge.svg?branch=master)](https://coveralls.io/github/alybaek2/cpvc?branch=master)

An Amstrad CPC emulator written in C++ and C# for Windows.

## Introduction

The Amstrad CPC ("Colour Personal Computer") is a range of 8-bit computers produced by Amstrad and popular in the UK and Europe during the mid-to-late 1980s.

See the [CPC Wiki](http://www.cpcwiki.eu/) for more information.

## What is CPvC short for?

Colour Personal *Virtual* Computer.

## How is CPvC different from every other CPC emulator?

CPvC has additional features on top of the standard ones offered by most emulators:

* A more "persistent" emulator model, where one or more virtual CPC instances can be created and persist even after the application is closed. These instances can be loaded again and resume from where they left off.
* Snapshots (or "bookmarks" as they're called in CPvC) are organized as part of a branching timeline, instead of as separate .SNA files. 

## Which CPC models are supported?

Currently, only the 6128 model is emulated. Support for other models (464, 664, 464+, and 6128+) will be added in the future.

## How to build

Before building, the Amstrad CPC roms must be manually copied to the `roms` folder. See [ReadMe.md](roms/ReadMe.md) in the `roms` folder for more details.

Open the `cpvc.sln` solution file in Visual Studio (File -> Open -> Project/Solution...), select the solution configuration and platform (Build -> Configuration Manager...), then build the solution (Build -> Build Solution).

Note that CPvC is currently being developed in Visual Studio Community 2017 (version 15.5.6). Note that since CPvC is written in both C++ and C#, Visual Studio needs at least the following Workloads/Components installed:

* .NET desktop development
* Desktop development with C++
  * Windows 10 SDK (10.0.16299.0)
* Universal Windows Platform development

The solution can also be built in the latest version of Visual Studio Community 2019 (16.3.7), though note that the Visual Studio 2017 C++ x64/x86 build tools (v14.16) need to be installed.

## How to run unit tests

In Visual Studio 2017, set the Default Processor Architecture to match the currently selected platform (x64 or x86). This option is under Test -> Test Settings -> Default Processor Architecture. Build the solution, then run all tests (Test -> Run -> All Tests).

## Running CPvC

Once built, launch the application (set the `cpvc` project as the startup project for the solution) and create a new instance (File -> New...), or open an existing one (File -> Open...).

## Features

See the [Features.md](docs/Features.md) file for more details.

## See also

For more details about the code, see [this file](docs/Code.md).