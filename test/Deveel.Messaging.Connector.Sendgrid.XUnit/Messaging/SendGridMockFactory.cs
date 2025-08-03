using Microsoft.Extensions.Logging;
using Moq;
using SendGrid;
using SendGrid.Helpers.Mail;

using System.Net.Http.Headers;

namespace Deveel.Messaging;

/// <summary>
/// Mock factory for creating SendGrid service mocks for testing.
/// </summary>
public static class SendGridMockFactory
{
    /// <summary>
    /// Creates a mock SendGrid service that simulates successful operations.
    /// </summary>
    /// <returns>A configured mock SendGrid service.</returns>
    public static Mock<ISendGridService> CreateSuccessfulMock()
    {
        var mock = new Mock<ISendGridService>();

        // Setup Initialize method
        mock.Setup(x => x.Initialize(It.IsAny<string>()))
            .Verifiable();

        // Setup TestConnectionAsync to return true
        mock.Setup(x => x.TestConnectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .Verifiable();

        // Setup SendEmailAsync to return successful response
        mock.Setup(x => x.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => CreateSuccessfulResponse())
            .Verifiable();

        // Setup GetEmailActivityAsync to return successful response
        mock.Setup(x => x.GetEmailActivityAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => CreateSuccessfulResponse())
            .Verifiable();

        return mock;
    }

    /// <summary>
    /// Creates a mock SendGrid service that simulates connection failures.
    /// </summary>
    /// <returns>A configured mock SendGrid service for failure scenarios.</returns>
    public static Mock<ISendGridService> CreateConnectionFailureMock()
    {
        var mock = new Mock<ISendGridService>();

        // Setup Initialize method
        mock.Setup(x => x.Initialize(It.IsAny<string>()))
            .Verifiable();

        // Setup TestConnectionAsync to return false
        mock.Setup(x => x.TestConnectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false)
            .Verifiable();

        return mock;
    }

    /// <summary>
    /// Creates a mock SendGrid service that simulates API errors.
    /// </summary>
    /// <returns>A configured mock SendGrid service for error scenarios.</returns>
    public static Mock<ISendGridService> CreateApiErrorMock()
    {
        var mock = new Mock<ISendGridService>();

        // Setup Initialize method
        mock.Setup(x => x.Initialize(It.IsAny<string>()))
            .Verifiable();

        // Setup TestConnectionAsync to return true (connection works but API calls fail)
        mock.Setup(x => x.TestConnectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .Verifiable();

        // Setup SendEmailAsync to return error response
        mock.Setup(x => x.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => CreateErrorResponse())
            .Verifiable();

        return mock;
    }

    /// <summary>
    /// Creates a mock SendGrid service that simulates rate limiting.
    /// </summary>
    /// <returns>A configured mock SendGrid service for rate limit scenarios.</returns>
    public static Mock<ISendGridService> CreateRateLimitMock()
    {
        var mock = new Mock<ISendGridService>();

        // Setup Initialize method
        mock.Setup(x => x.Initialize(It.IsAny<string>()))
            .Verifiable();

        // Setup TestConnectionAsync to return true
        mock.Setup(x => x.TestConnectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .Verifiable();

        // Setup SendEmailAsync to return rate limit response
        mock.Setup(x => x.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => CreateRateLimitResponse())
            .Verifiable();

        return mock;
    }

    /// <summary>
    /// Creates a mock SendGrid service that throws exceptions.
    /// </summary>
    /// <returns>A configured mock SendGrid service for exception scenarios.</returns>
    public static Mock<ISendGridService> CreateExceptionMock()
    {
        var mock = new Mock<ISendGridService>();

        // Setup Initialize method to throw
        mock.Setup(x => x.Initialize(It.IsAny<string>()))
            .Throws(new InvalidOperationException("Mock exception for testing"));

        // Setup other methods to throw
        mock.Setup(x => x.TestConnectionAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Mock network exception"));

        mock.Setup(x => x.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Mock timeout exception"));

        return mock;
    }

    /// <summary>
    /// Creates valid connection settings for testing.
    /// </summary>
    /// <returns>A configured ConnectionSettings instance.</returns>
    public static ConnectionSettings CreateValidConnectionSettings()
    {
        var settings = new ConnectionSettings();
        settings.SetParameter("ApiKey", "SG.test_api_key_for_unit_tests");
        settings.SetParameter("SandboxMode", true);
        settings.SetParameter("TrackingSettings", true);
        settings.SetParameter("DefaultFromName", "Test Sender");
        settings.SetParameter("DefaultReplyTo", "noreply@test.com");
        return settings;
    }

    /// <summary>
    /// Creates minimal connection settings for testing.
    /// </summary>
    /// <returns>A minimal ConnectionSettings instance.</returns>
    public static ConnectionSettings CreateMinimalConnectionSettings()
    {
        var settings = new ConnectionSettings();
        settings.SetParameter("ApiKey", "SG.minimal_test_api_key");
        return settings;
    }

    /// <summary>
    /// Creates a test logger for SendGrid connector testing.
    /// </summary>
    /// <returns>A test logger instance.</returns>
    public static TestLogger<SendGridEmailConnector> CreateTestLogger()
    {
        return new TestLogger<SendGridEmailConnector>();
    }

    private static Response CreateSuccessfulResponse()
    {
        var content = new StringContent("{\"message\":\"success\"}");
        var headers = new HttpResponseMessage().Headers;
        headers.Add("X-Message-Id", Guid.NewGuid().ToString());

        return new Response(System.Net.HttpStatusCode.Accepted, content, headers);
    }

    private static Response CreateErrorResponse()
    {
        var content = new StringContent("{\"errors\":[{\"message\":\"Bad Request\",\"field\":\"from\",\"help\":\"The from email address is not valid\"}]}");
        var headers = new HttpResponseMessage().Headers;

        return new Response(System.Net.HttpStatusCode.BadRequest, content, headers);
    }

    private static Response CreateRateLimitResponse()
    {
        var content = new StringContent("{\"errors\":[{\"message\":\"Rate limit exceeded\"}]}");
        var headers = new HttpResponseMessage().Headers;
        headers.Add("X-RateLimit-Remaining", "0");
        headers.Add("X-RateLimit-Reset", "3600");

        return new Response(System.Net.HttpStatusCode.TooManyRequests, content, headers);
    }
}

/// <summary>
/// A test implementation of <see cref="ILogger{T}"/> for unit testing scenarios.
/// </summary>
/// <typeparam name="T">The type whose name is used for the logger category name.</typeparam>
public class TestLogger<T> : ILogger<T>
{
    private readonly List<LogEntry> _logs = new();

    /// <summary>
    /// Gets the collection of log entries that have been recorded.
    /// </summary>
    public IReadOnlyList<LogEntry> Logs => _logs.AsReadOnly();

    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc/>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        _logs.Add(new LogEntry(logLevel, eventId, message, exception));
    }

    /// <summary>
    /// Clears all recorded log entries.
    /// </summary>
    public void Clear() => _logs.Clear();

    /// <summary>
    /// Gets all log entries for the specified log level.
    /// </summary>
    /// <param name="logLevel">The log level to filter by.</param>
    /// <returns>A collection of log entries at the specified level.</returns>
    public IEnumerable<LogEntry> GetLogs(LogLevel logLevel) => 
        _logs.Where(log => log.LogLevel == logLevel);

    /// <summary>
    /// Checks if any log entries contain the specified message.
    /// </summary>
    /// <param name="message">The message to search for.</param>
    /// <returns>True if any log entry contains the message, false otherwise.</returns>
    public bool ContainsMessage(string message) => 
        _logs.Any(log => log.Message.Contains(message, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Represents a single log entry.
    /// </summary>
    public record LogEntry(LogLevel LogLevel, EventId EventId, string Message, Exception? Exception);
}