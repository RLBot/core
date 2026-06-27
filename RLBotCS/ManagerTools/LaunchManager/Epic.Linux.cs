#if !WINDOWS
namespace RLBotCS.ManagerTools;

public static partial class LaunchManager
{
    private static void LaunchGameViaEpic(int gamePort)
    {
        throw new NotSupportedException(
            "Epic Games Store is not directly supported on Linux."
        );
    }
}
#endif
