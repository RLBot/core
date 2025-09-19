# RLBot Core

The RLBot Core Project builds a `RLBotServer.exe` binary that allows custom bots and scripts
to interface with Rocket League.

This is a C++ rewrite of the old backend that lived at
[RLBot/src/main/cpp/RLBotInterface](https://github.com/RLBot/RLBot/tree/00720d1efc447e5495d3952a03e10b5b762421ee/src/main/cpp/RLBotInterface) in v4.

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

1. Ensure all changes are on the `master` branch.
1. Ensure the version number is correct in `RLBotCS/Main.cs`.
1. Create a new tag with the next version number - e.x. `git tag v0.1.0 -m "Core v0.1.0"`.
   - Preferably sign the tag too - `git tag -s v0.1.0 -m "Core v0.1.0"`.
1. Push the tag - `git push --tags`.
1. Wait for the GitHub Actions to build the release and upload it to the release page!

Further deployment steps for automatic updates are still in progress.

## Maintenance

### Formatting

This project uses the CSharpier formatter. You can run it with `dotnet csharpier format .`

### Flatbuffers

The Core project uses flatbuffers, which involves generating C# code based on a specification
file called `rlbot.fbs`. Find this in `./FlatBuffer` after it's been generated.

The [flatbuffers-schema](https://github.com/RLBot/flatbuffers-schema) submodule should be kept update to date:

- `cd flatbuffers-schema`
- `git checkout main`
- `git pull`

The needed Flatbuffers code is automatically generated upon compilation of the project.

### Bridge

The `Bridge.dll` file in `RLBotCS/lib` is built from a _closed-source_ repository due to legal reasons.
It is maintained by RLBot developers who have signed an agreement with Psyonix to keep it private.

The dll file is platform-independent and works for building the project on both Windows and Linux.

### rl_ball_sym

The native binaries that live in `RLBotCS/lib/rl_ball_sym` generate the ball prediction that core then distributes to bots & scripts that request it.
The `dll`/`so` are dynamically loaded at run time while developing core, and the `a`/`lib` files are statically linked during publishing.

All source code and releases for building the dlls can be found at <https://github.com/VirxEC/rl_ball_sym_dll> but the core of the code is a library that's published for anyone's use at <https://crates.io/crates/rl_ball_sym>.
