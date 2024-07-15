# About

The RLBot Core Project builds a `RLBotServer.exe` binary that allows custom bots and scripts
to interface with Rocket League.

This is a rewrite a C++ version that had lived at
[https://github.com/RLBot/RLBot/tree/00720d1efc447e5495d3952a03e10b5b762421ee/src/main/cpp/RLBotInterface]

## Developer Setup

- Install .NET 8 SDK [https://dotnet.microsoft.com/en-us/download/dotnet/8.0]
- Install an IDE
  - Visual Studio 2022 was used for initial development.
  - Rider and VS Code are also known to work.
- Ensure submodules got cloned
  - `git submodule update --init`

## Building

In Visual Studio 2022, you can build the solution in Release mode, and find the
compiled binaries at `RLBotCS\bin\Release\net8.0`.

- Note: You can also build with the command `dotnet build -c "Release"`

## Deployment

New deployment method is still TBD.

## Maintenance

### Formatting

This project uses the CSharpier formatter. You can run it with `dotnet csharpier .`

### Flatbuffers

The Core project uses flatbuffers, which involves generating C# code based on a specification
file called `rlbot.fbs`. Find this in `RLBotCS/FlatBuffer` after it's been generated.

The [flatbuffers-schema](https://github.com/RLBot/flatbuffers-schema) submodule should be kept update to date:

- `cd RLBotCS/flatbuffers-schema`
- `git checkout main`
- `git fetch`
- `git pull`

The needed Flatbuffers code is automatically generated upon compilation of the project.

### Bridge

The `Bridge.dll` file in `RLBotCS/lib` is built from a _closed-source_ repository due to legal reasons.
It is maintained by RLBot developers who have signed an agreement with Psyonix to keep it private.

The dll file is platform-independent and works for building the project on both Windows and Linux.
