#if WINDOWS
using System.Runtime.InteropServices;
using System.Text;

namespace RLBotCS.ManagerTools;

public static class ReadLog
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

    private static string LogPath { get; } =
        Path.Combine(
            GetMyDocumentsFolder(),
            "My Games",
            "Rocket League",
            "TAGame",
            "Logs",
            "Launch.log"
        );

    public static void DeleteLog()
    {
        if (File.Exists(LogPath))
            File.Delete(LogPath);
    }

    public static (string, string)? GetGamePathAndAuth()
    {
        if (!File.Exists(LogPath))
            return null;

        try
        {
            string? auth = null;
            string? path = null;

            using var stream = new FileStream(
                LogPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite
            );
            using var reader = new StreamReader(
                stream,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: true
            );

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (
                    auth == null
                    && line.StartsWith(AUTH_LINE_PREFIX, StringComparison.Ordinal)
                )
                {
                    auth = line[AUTH_LINE_PREFIX.Length..].TrimEnd('\r', '\n');
                }
                else if (
                    path == null
                    && line.StartsWith(PATH_LINE_PREFIX, StringComparison.Ordinal)
                )
                {
                    path = line[PATH_LINE_PREFIX.Length..].TrimEnd('\r', '\n');
                }

                if (auth != null && path != null)
                {
                    return (Path.Combine(path, BINARY_NAME), auth);
                }
            }

            return null;
        }
        catch (IOException)
        {
            // Rocket League may still be writing to the log file.
            // Return null so the caller retries.
            return null;
        }
    }
}
#endif
