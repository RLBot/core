using System.Text;
using Microsoft.Extensions.Logging;

namespace RLBotCS.ManagerTools;

public class Logging : ILogger
{
    private const string Grey = "\x1b[38;20m";
    private const string LightBlue = "\x1b[94;20m";
    private const string Yellow = "\x1b[33;20m";
    private const string Green = "\x1b[32;20m";
    private const string Red = "\x1b[31;20m";
    private const string BoldRed = "\x1b[31;1m";
    private const string Reset = "\x1b[0m";

    private static readonly LogLevel LoggingLevel = Environment.GetEnvironmentVariable(
        "RLBOT_LOG_LEVEL"
    ) switch
    {
        "debug" => LogLevel.Debug,
        "info" => LogLevel.Information,
        "warn" => LogLevel.Warning,
        "error" => LogLevel.Error,
        "critical" => LogLevel.Critical,
        _ => LogLevel.Information,
    };

    private static readonly string AppName = "RLBotServer".PadLeft(12);

    private readonly string _name;
    private readonly LogLevel _minLevel;
    private static readonly object _lock = new object();

    public Logging(string name, LogLevel minLevel)
    {
        // Longest logger: "FlatBuffersSession" with 18 characters
        _name = name.PadLeft(18);
        _minLevel = minLevel;
    }

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= _minLevel;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter
    )
    {
        if (!IsEnabled(logLevel))
            return;

        var logLevelColors = GetLogLevelColors(logLevel);
        var logLevelString = GetLogLevelString(logLevel);
        var message = formatter(state, exception);

        lock (_lock)
        {
            var logBuilder = new StringBuilder();
            logBuilder.Append($"{logLevelColors[0]}{DateTime.Now:HH:mm:ss}{Reset} ");
            logBuilder.Append(
                $"{logLevelColors[1]}{logLevelString}:{Reset}{Green}{AppName}{Reset}"
            );
            logBuilder.Append($"{logLevelColors[2]}[ {_name} ]{Reset} ");
            logBuilder.Append($"{logLevelColors[3]}{message}{Reset}");

            Console.WriteLine(logBuilder.ToString());
        }
    }

    private string[] GetLogLevelColors(LogLevel logLevel) =>
        logLevel switch
        {
            LogLevel.Trace => new[] { Grey, Grey, Grey, Grey },
            LogLevel.Debug => new[] { Grey, Grey, Grey, LightBlue },
            LogLevel.Information => new[] { Grey, LightBlue, Grey, LightBlue },
            LogLevel.Warning => new[] { Yellow, Yellow, Yellow, Yellow },
            LogLevel.Error => new[] { Red, Red, Red, Red },
            LogLevel.Critical => new[] { Red, BoldRed, Red, BoldRed },
            _ => new[] { Grey, Grey, Grey, Grey },
        };

    private string GetLogLevelString(LogLevel logLevel) =>
        logLevel switch
        {
            LogLevel.Trace => "TRACE".PadLeft(8),
            LogLevel.Debug => "DEBUG".PadLeft(8),
            LogLevel.Information => "INFO".PadLeft(8),
            LogLevel.Warning => "WARNING".PadLeft(8),
            LogLevel.Error => "ERROR".PadLeft(8),
            LogLevel.Critical => "CRITICAL".PadLeft(8),
            _ => "UNKNOWN".PadLeft(8),
        };

    public class CustomConsoleLoggerProvider : ILoggerProvider
    {
        private readonly LogLevel _minLevel;

        public CustomConsoleLoggerProvider(LogLevel minLevel)
        {
            _minLevel = minLevel;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new Logging(categoryName, _minLevel);
        }

        public void Dispose() { }
    }

    public static ILogger GetLogger(string loggerName)
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .SetMinimumLevel(LoggingLevel)
                .AddProvider(new CustomConsoleLoggerProvider(LoggingLevel));
        });

        var logger = loggerFactory.CreateLogger(loggerName);
        return logger;
    }
}
