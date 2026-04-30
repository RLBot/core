#if WINDOWS
using System.Runtime.InteropServices;
using System.Text;

public class WinReadLog
{
    private const int CSIDL_PERSONAL = 0x0005;
    private const int SHGFP_TYPE_CURRENT = 0;
    private const string AUTH_LINE_PREFIX = "Init: Command line: ";
    private const string PATH_LINE_PREFIX = "Init: Base directory: ";
    private const string BINARY_NAME = "RocketLeague.exe";

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHGetFolderPathW(
        IntPtr hwnd,
        int csidl,
        IntPtr hToken,
        int dwFlags,
        StringBuilder pszPath
    );

    static string GetMyDocumentsFolder()
    {
        var sb = new StringBuilder(260);
        SHGetFolderPathW(IntPtr.Zero, CSIDL_PERSONAL, IntPtr.Zero, SHGFP_TYPE_CURRENT, sb);
        return sb.ToString();
    }

    private string LogPath;

    public WinReadLog()
    {
        LogPath = Path.Combine(
            GetMyDocumentsFolder(),
            "My Games",
            "Rocket League",
            "TAGame",
            "Logs",
            "Launch.log"
        );
    }

    public (string, string)? GetGamePathAndAuth()
    {
        string logContent = File.ReadAllText(LogPath);

        int authStart =
            logContent.IndexOf(AUTH_LINE_PREFIX, StringComparison.Ordinal)
            + AUTH_LINE_PREFIX.Length;
        int pathStart =
            logContent.IndexOf(PATH_LINE_PREFIX, StringComparison.Ordinal)
            + PATH_LINE_PREFIX.Length;
        if (authStart == -1 || pathStart == -1)
            return null;

        int authEnd = logContent.IndexOf('\n', authStart);
        int pathEnd = logContent.IndexOf('\n', pathStart);
        if (authEnd == -1 || pathEnd == -1)
            return null;

        string auth = logContent[authStart..authEnd].TrimEnd('\r', '\n');
        string path = logContent[pathStart..pathEnd].TrimEnd('\r', '\n');

        return (Path.Combine(path, BINARY_NAME), auth);
    }
}
#endif
