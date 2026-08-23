using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Shared.Guardian.Data;

namespace Shared.Tests.Guardian;

internal sealed class RecordingLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter) =>
        Entries.Add((logLevel, formatter(state, exception), exception));
}

internal static class GuardianLinkFailures
{
    public static DbUpdateException DuplicatePair() =>
        new("insert failed", new PostgresException(
            $"duplicate key value violates unique constraint \"{GuardianLinkSchema.GuardianWardUniqueIndex}\"",
            "ERROR", "ERROR", PostgresErrorCodes.UniqueViolation,
            constraintName: GuardianLinkSchema.GuardianWardUniqueIndex));
}
