# About

The Core Project builds a RLBotServer.exe binary that allows custom bots and scripts
to interface with Rocket League.

This is a rewrite a C++ version that had lived at
[https://github.com/RLBot/RLBot/tree/00720d1efc447e5495d3952a03e10b5b762421ee/src/main/cpp/RLBotInterface]

## Developer Setup

- Install .NET 8 SDK [https://dotnet.microsoft.com/en-us/download/dotnet/8.0]
- Install an IDE
  - Visual Studio 2022 was used for initial development.
  - Rider is also known to work.

## Building

In Visual Studio 2022, you can build the solution in Release mode, and find the
compiled binaries at RLBotCS\bin\Release\net8.0.

## Deployment

This project generates a RLBot.exe file and some DLLs. New deployment method is still TODO.

## Maintenance

### Formatting

This project uses the CSharpier formatter. You can run it with `dotnet csharpier .`

### Flatbuffers

The Core project uses flatbuffers, which involves generating C# code based on a specification
file called rlbot.fbs. Find this in RLBotCS/FlatBuffer.

The rlbot.fbs file should be kept in sync with
[https://github.com/RLBot/flatbuffers-schema/blob/main/rlbot.fbs]

If there are any changes to rlbot.fbs, you'll need to run RLBotCS/FlatBuffer/generate-flatbuffers.bat
to regenerate rlbot.cs.

### RLBotSecret

The RLBotSecret.dll file in RLBotCS/lib is built from a closed-source repository. It is maintained
by RLBot developers who have signed an agreement with Psyonix to keep it private.
