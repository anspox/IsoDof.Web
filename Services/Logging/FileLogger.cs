namespace IsoDof.Web.Services.Logging;

public class FileLogger : ILogger
{
    private readonly string _categoryName;
    private readonly FileLoggerProvider _provider;

    public FileLogger(string categoryName, FileLoggerProvider provider)
    {
        _categoryName = categoryName;
        _provider = provider;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        var message = formatter(state, exception);
        if (string.IsNullOrEmpty(message) && exception == null) return;

        // Kategori adını sadeleştir (Örn: IsoDof.Web.Middleware.RequestTimingMiddleware => RequestTimingMiddleware)
        var shortCategory = _categoryName.Contains('.')
            ? _categoryName.Substring(_categoryName.LastIndexOf('.') + 1)
            : _categoryName;

        var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{logLevel.ToString().ToUpper(),-5}] [{shortCategory}] {message}";
        if (exception != null)
        {
            logEntry += Environment.NewLine + exception;
        }

        _provider.WriteEntry(logEntry);
    }
}

public class FileLoggerProvider : ILoggerProvider
{
    private readonly string _logDirectory;
    private readonly object _lock = new();

    public FileLoggerProvider(string logDirectory = "logs")
    {
        _logDirectory = logDirectory;
        if (!Directory.Exists(_logDirectory))
        {
            Directory.CreateDirectory(_logDirectory);
        }
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(categoryName, this);
    }

    public void WriteEntry(string message)
    {
        lock (_lock)
        {
            try
            {
                var fileName = $"log-{DateTime.Now:yyyy-MM-dd}.log";
                var filePath = Path.Combine(_logDirectory, fileName);
                File.AppendAllText(filePath, message + Environment.NewLine);
            }
            catch
            {
                // Dosya loglama hatası ana uygulamanın akışını bozmamalı
            }
        }
    }

    public void Dispose()
    {
    }
}

public static class FileLoggerExtensions
{
    public static ILoggingBuilder AddFileLogging(this ILoggingBuilder builder, string logDirectory = "logs")
    {
        builder.AddProvider(new FileLoggerProvider(logDirectory));
        return builder;
    }
}
