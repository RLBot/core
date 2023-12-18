# About

The RLBotCS project in this repository builds a RLBot.exe binary that allows custom bots and scripts
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

This project generates a RLBot.exe file and some DLLs. They should be copied to
[https://github.com/RLBot/RLBot/tree/master/src/main/python/rlbot/dll], and then
you follow the deployment process for the python package, described at
[https://github.com/RLBot/RLBot/wiki/Deploying-Changes#publishing-python-to-pypi]

## Maintenance

### Flatbuffers

The RLBotCS project uses flatbuffers, which involves generating C# code based on a specification
file called rlbot.fbs. Find this in RLBotCS/FlatBuffer.

The rlbot.fbs file should be kept in sync with
[https://github.com/RLBot/RLBot/blob/master/src/main/flatbuffers/rlbot.fbs]

If there are any changes to rlbot.fbs, you'll need to run RLBotCS/FlatBuffer/generate-flatbuffers.bat
to regenerate rlbot.cs.

We also have a FlatBuffers.dll file in the RLBotCS/lib folder. This was built from the
v23.5.26 tag at [https://github.com/google/flatbuffers/tags] using .NET 6.

### RLBotSecret

The RLBotSecret.dll file in RLBotCS/lib is built from a closed-source repository. It is maintained
by core RLBot developers who have signed an agreement with Psyonix to keep it private.
