# RLBot Core

RLBot Core is the server component of the RLBot v5 framework. It allows custom bots, scripts, and match managers to communicate with Rocket League in offline mode.

To use RLBot as a player or bot developer, please download the installer at https://rlbot.org/.

## Developer Setup

1. Install [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
1. Initialize submodules: `git submodule update --init`
1. Build: `dotnet build -c Release`

## Deployment

1. Ensure all changes are on the `master` branch.
1. Ensure the version number is correct in `RLBotCS/Main.cs`.
1. In GitHub, create a new release.
    - Name the release and the new tag after the version number, e.g. `v5.0.1`
    - Mark the release as **pre-release** initially.
    - Write/generate an appropriate change log.
1. Wait for the GitHub Actions workflow to build the release and upload it to the release page.
1. After the workflow finishes, edit the release and set it as the **latest release**.

> [!TIP]
> RLBot clients download the version tagged as the latest release. To rollback a release or otherwise change the version being distributed, simply mark an older version at the latest release in GitHub.

## Maintenance

### Formatting

This project uses the CSharpier formatter. You can run it with `dotnet csharpier format .`

### Flatbuffers

Communication with RLBotServer uses [FlatBuffers](https://flatbuffers.dev/).
The schema is held in the [flatbuffers-schema](https://github.com/RLBot/flatbuffers-schema) submodule
and used to generate FlatBuffers classes during compilation.
The generated C# files are written to `FlatBuffer/`.

Update the submodule as follows:

1. `cd flatbuffers-schema`
1. `git checkout main`
1. `git pull`

### Bridge

The `Bridge.dll` file in `RLBotCS/lib` is built from a _closed-source_ repository due to legal reasons.
It is maintained by RLBot developers who have signed an agreement with Psyonix to keep it private.

The dll file is platform-independent and works for building the project on both Windows and Linux.

### rl_ball_sym

The native binaries that live in `RLBotCS/lib/rl_ball_sym` generate ball predictions that core then distributes to bots & scripts that request them.
The `dll`/`so` are dynamically loaded at run time while developing core, and the `a`/`lib` files are statically linked during publishing.

All source code and releases for building the dlls can be found at <https://github.com/VirxEC/rl_ball_sym_dll> but the core of the code is a library that's published for anyone's use at <https://crates.io/crates/rl_ball_sym>.
