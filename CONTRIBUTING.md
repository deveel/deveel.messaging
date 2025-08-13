# Contributing to Deveel Messaging Framework

Thank you for your interest in contributing to the Deveel Messaging Framework! This document provides guidelines and information for contributors.

## 🤝 How to Contribute

We welcome contributions of all kinds:

- **Bug reports** - Help us identify and fix issues
- **Feature requests** - Suggest new functionality
- **Code contributions** - Submit bug fixes, new features, or improvements
- **Documentation** - Improve or add to our documentation
- **Testing** - Help improve test coverage and quality

## 🚀 Getting Started

### Prerequisites

- **.NET 8.0 SDK** or later (.NET 9.0 recommended)
- **Visual Studio 2022** (17.4+) or **JetBrains Rider** or **VS Code** with C# extension
- **Git** for source control
- Basic knowledge of **C#**, **async/await**, and **dependency injection**

### Setting Up Your Development Environment

1. **Fork the repository** on GitHub
2. **Clone your fork** locally:
   ```bash
   git clone https://github.com/YOUR_USERNAME/deveel.messaging.git
   cd deveel.messaging
   ```

3. **Add the upstream remote**:
   ```bash
   git remote add upstream https://github.com/deveel/deveel.messaging.git
   ```

4. **Build the solution**:
   ```bash
   dotnet build
   ```

5. **Run the tests**:
   ```bash
   dotnet test
   ```

### Project Structure

```
deveel.messaging/
├── src/                                    # Source code
│   ├── Deveel.Messaging.Abstractions/     # Core abstractions
│   ├── Deveel.Messaging.Connector.Abstractions/ # Connector base classes
│   ├── Deveel.Messaging.Connector.Twilio/ # Twilio SMS / WhatsApp connector
│   └── Deveel.Messaging.Connector.Sendgrid/ # SendGrid email connector
├── test/                                   # Test projects
│   ├── ...
│   └── ...
├── docs/                                   # Documentation
├── .github/                                # GitHub workflows and templates
└── README.md                              # Project overview
```

## 📝 Code Style Guidelines

### Coding Standards

We follow Microsoft's C# coding conventions with some additional guidelines:

#### General Guidelines

- **Use C# 12.0 features** where appropriate
- **Enable nullable reference types** in all projects
- **Follow async/await best practices**
- **Use dependency injection** patterns
- **Write comprehensive unit tests**
- **Include XML documentation** for public APIs

#### Naming Conventions

```csharp
// ✅ Good
public class EmailChannelConnector : ChannelConnectorBase
{
    private readonly ILogger<EmailChannelConnector> _logger;
    
    public async Task<ConnectorResult<MessageResult>> SendMessageAsync(
        IMessage message, 
        CancellationToken cancellationToken = default)
    {
        // Implementation
    }
}

// ❌ Avoid
public class emailconnector
{
    public Task<object> send(object msg)
    {
        // Implementation
    }
}
```

#### Code Organization

- **One class per file** (except for small related classes)
- **Organize using statements** (remove unused, group by type)
- **Use regions sparingly** (prefer smaller classes)
- **Keep methods focused** (single responsibility)

#### Error Handling

```csharp
// ✅ Good - Use ConnectorResult pattern
protected override async Task<ConnectorResult<MessageResult>> SendMessageCoreAsync(
    IMessage message, CancellationToken cancellationToken)
{
    try
    {
        var result = await SendToProviderAsync(message, cancellationToken);
        return ConnectorResult<MessageResult>.Success(result);
    }
    catch (OperationCanceledException)
    {
        return ConnectorResult<MessageResult>.Failure("Operation was cancelled");
    }
    catch (HttpRequestException ex)
    {
        return ConnectorResult<MessageResult>.Failure($"Network error: {ex.Message}", "NETWORK_ERROR");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error sending message {MessageId}", message.Id);
        return ConnectorResult<MessageResult>.Failure($"Unexpected error: {ex.Message}", "UNEXPECTED_ERROR");
    }
}

// ❌ Avoid - Throwing exceptions from connector methods
protected override async Task<ConnectorResult<MessageResult>> SendMessageCoreAsync(
    IMessage message, CancellationToken cancellationToken)
{
    var result = await SendToProviderAsync(message, cancellationToken);
    if (result == null)
        throw new InvalidOperationException("Send failed"); // Don't do this
    
    return ConnectorResult<MessageResult>.Success(result);
}
```

### EditorConfig

We use an `.editorconfig` file to maintain consistent formatting. Make sure your IDE respects these settings:

```ini
root = true

[*]
charset = utf-8
end_of_line = crlf
insert_final_newline = true
trim_trailing_whitespace = true

[*.cs]
indent_style = space
indent_size = 4

# C# specific rules
dotnet_style_qualification_for_field = false:suggestion
dotnet_style_qualification_for_property = false:suggestion
dotnet_style_qualification_for_method = false:suggestion
dotnet_style_qualification_for_event = false:suggestion
csharp_prefer_braces = true:warning
csharp_using_directive_placement = outside_namespace:warning
```

## 🧪 Testing Guidelines

### Unit Testing Standards

- **Use xUnit** for unit tests
- **Follow AAA pattern** (Arrange, Act, Assert)
- **Use descriptive test names** that explain the scenario
- **Test both success and failure paths**
- **Mock external dependencies**

#### Test Structure

```csharp
public class EmailConnectorTests
{
    [Fact]
    public async Task SendMessageAsync_WithValidEmailMessage_ShouldReturnSuccess()
    {
        // Arrange
        var schema = CreateEmailSchema();
        var connector = new EmailConnector(schema, CreateMockConfiguration());
        var message = new MessageBuilder()
            .WithId("test-001")
            .WithEmailSender("sender@test.com")
            .WithEmailReceiver("recipient@test.com")
            .WithTextContent("Test message")
            .Message;

        await connector.InitializeAsync(CancellationToken.None);

        // Act
        var result = await connector.SendMessageAsync(message, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotEmpty(result.Value.MessageId);
        Assert.Equal(MessageStatus.Sent, result.Value.Status);
    }

    [Fact]
    public async Task SendMessageAsync_WithInvalidEndpoint_ShouldReturnFailure()
    {
        // Arrange
        var schema = CreateEmailSchema();
        var connector = new EmailConnector(schema, CreateMockConfiguration());
        var message = new MessageBuilder()
            .WithId("test-002")
            .WithEmailSender("invalid-email")  // Invalid email format
            .WithEmailReceiver("recipient@test.com")
            .WithTextContent("Test message")
            .Message;

        await connector.InitializeAsync(CancellationToken.None);

        // Act
        var result = await connector.SendMessageAsync(message, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("invalid", result.ErrorMessage?.ToLower() ?? "");
    }

    private static IChannelSchema CreateEmailSchema()
    {
        return new ChannelSchema("Test", "Email", "1.0.0")
            .WithCapabilities(ChannelCapability.SendMessages)
            .AllowsMessageEndpoint(EndpointType.EmailAddress)
            .AddContentType(MessageContentType.PlainText);
    }

    private static Dictionary<string, object> CreateMockConfiguration()
    {
        return new Dictionary<string, object>
        {
            ["Host"] = "smtp.test.com",
            ["Port"] = 587,
            ["Username"] = "test@test.com",
            ["Password"] = "password"
        };
    }
}
```

#### Integration Testing

For connectors that interact with external services:

```csharp
[Collection("Integration Tests")]
public class TwilioConnectorIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task SendSms_WithRealTwilioAccount_ShouldSendMessage()
    {
        // Only run if environment variables are set
        var accountSid = Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID");
        var authToken = Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN");
        var fromNumber = Environment.GetEnvironmentVariable("TWILIO_FROM_NUMBER");
        
        Skip.If(string.IsNullOrEmpty(accountSid), "Twilio credentials not configured");
        
        // Test implementation...
    }
}
```

### Test Coverage

- **Aim for 80%+ code coverage**
- **Focus on business logic** and critical paths
- **Don't test framework code** or simple properties
- **Use code coverage tools** to identify gaps

```bash
# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage

# Generate coverage reports (requires reportgenerator tool)
reportgenerator -reports:"coverage/**/*.xml" -targetdir:"coverage/report" -reporttypes:Html
```

## 📋 Pull Request Process

### Before Submitting

1. **Ensure all tests pass**: `dotnet test`
2. **Check code formatting**: Ensure your IDE follows the EditorConfig rules
3. **Update documentation** if you've changed public APIs
4. **Add tests** for new functionality
5. **Update CHANGELOG.md** if applicable

### Pull Request Checklist

- [ ] **Clear title** and description explaining the changes
- [ ] **Reference related issues** (e.g., "Fixes #123")
- [ ] **All tests pass** locally
- [ ] **No merge conflicts** with the main branch
- [ ] **Documentation updated** if needed
- [ ] **Breaking changes** are clearly marked and explained

### Pull Request Template

```markdown
## Description
Brief description of what this PR does.

## Type of Change
- [ ] Bug fix (non-breaking change which fixes an issue)
- [ ] New feature (non-breaking change which adds functionality)
- [ ] Breaking change (fix or feature that would cause existing functionality to not work as expected)
- [ ] Documentation update

## Testing
- [ ] I have added tests that prove my fix is effective or that my feature works
- [ ] New and existing unit tests pass locally with my changes
- [ ] I have tested this with real providers (if applicable)

## Documentation
- [ ] I have updated the documentation accordingly
- [ ] I have added XML documentation for new public APIs

## Related Issues
Fixes #(issue_number)

## Additional Notes
Any additional information, configuration, or data that might be necessary to reproduce the issue.
```

## 🐛 Bug Reports

When reporting bugs, please include:

### Bug Report Template

```markdown
**Describe the bug**
A clear and concise description of what the bug is.

**To Reproduce**
Steps to reproduce the behavior:
1. Go to '...'
2. Click on '....'
3. Scroll down to '....'
4. See error

**Expected behavior**
A clear and concise description of what you expected to happen.

**Code Sample**
```csharp
// Minimal code sample that reproduces the issue
var connector = new SomeConnector(schema);
var result = await connector.SomeMethod();
// Result is not what's expected
```

**Environment:**
- OS: [e.g., Windows 11, Ubuntu 20.04]
- .NET Version: [e.g., .NET 8.0, .NET 9.0]
- Package Version: [e.g., 2.1.0]
- Provider: [e.g., Twilio, SendGrid]

**Additional context**
Add any other context about the problem here.
```

## 💡 Feature Requests

When suggesting new features:

### Feature Request Template

```markdown
**Is your feature request related to a problem? Please describe.**
A clear and concise description of what the problem is. Ex. I'm always frustrated when [...]

**Describe the solution you'd like**
A clear and concise description of what you want to happen.

**Describe alternatives you've considered**
A clear and concise description of any alternative solutions or features you've considered.

**Use Case**
Describe the specific use case this feature would address.

**Additional context**
Add any other context or screenshots about the feature request here.
```

## 🏗️ Architecture Guidelines

### Adding New Connectors

When adding a new connector:

1. **Create the connector class** extending `ChannelConnectorBase`
2. **Define the schema** with appropriate capabilities and parameters
3. **Implement required abstract methods**
4. **Add comprehensive tests**
5. **Create documentation**
6. **Add integration tests** (if possible)

### Example New Connector Structure

```
src/Deveel.Messaging.Connector.NewProvider/
├── NewProviderConnector.cs              # Main connector implementation
├── NewProviderConfiguration.cs          # Configuration model
├── NewProviderException.cs              # Provider-specific exceptions
├── NewProviderMessageResult.cs          # Provider-specific result models
└── Deveel.Messaging.Connector.NewProvider.csproj

test/Deveel.Messaging.Connector.NewProvider.XUnit/
├── NewProviderConnectorTests.cs         # Unit tests
├── NewProviderIntegrationTests.cs       # Integration tests (optional)
└── Deveel.Messaging.Connector.NewProvider.XUnit.csproj
```

### Schema Design Guidelines

- **Use semantic versioning** for schema versions
- **Be conservative** with breaking changes
- **Provide good defaults** for optional parameters
- **Mark sensitive parameters** appropriately
- **Document all parameters** clearly

## 📚 Documentation Standards

### XML Documentation

All public APIs must include XML documentation:

```csharp
/// <summary>
/// Sends a message through the configured messaging provider.
/// </summary>
/// <param name="message">The message to send. Must not be null.</param>
/// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
/// <returns>
/// A <see cref="ConnectorResult{T}"/> containing the message result if successful,
/// or error information if the send operation failed.
/// </returns>
/// <exception cref="ArgumentNullException">Thrown when <paramref name="message"/> is null.</exception>
/// <exception cref="InvalidOperationException">Thrown when the connector is not initialized.</exception>
public async Task<ConnectorResult<MessageResult>> SendMessageAsync(
    IMessage message, 
    CancellationToken cancellationToken = default)
{
    // Implementation
}
```

### Markdown Documentation

- **Use clear headings** and structure
- **Include code examples** for all features
- **Provide working samples** that can be copy-pasted
- **Keep examples up-to-date** with the current API

## 🎯 Performance Guidelines

### Performance Considerations

- **Use async/await** properly (don't block on async calls)
- **Dispose resources** appropriately
- **Consider memory usage** for large message batches
- **Implement caching** where appropriate
- **Use connection pooling** for HTTP-based connectors

### Benchmarking

For performance-critical changes, include benchmarks:

```csharp
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class MessageBuildingBenchmarks
{
    [Benchmark]
    public IMessage CreateMessageWithBuilder()
    {
        return new MessageBuilder()
            .WithId("test-001")
            .WithEmailSender("sender@test.com")
            .WithEmailReceiver("recipient@test.com")
            .WithTextContent("Benchmark test message")
            .Message;
    }
}
```

## 🚀 Release Process

### Versioning

We follow **Semantic Versioning** (SemVer):

- **Major** (X.0.0): Breaking changes
- **Minor** (X.Y.0): New features, backward compatible
- **Patch** (X.Y.Z): Bug fixes, backward compatible

### Changelog

Update `CHANGELOG.md` for all notable changes:

```markdown
## [2.1.0] - 2024-XX-XX

### Added
- New webhook connector for HTTP-based messaging
- Support for custom parameter validation
- Batch processing capabilities

### Changed
- Improved error handling in base connector class
- Enhanced schema validation performance

### Fixed
- Issue with endpoint type validation for custom endpoints
- Memory leak in connector pool implementation

### Breaking Changes
- Removed deprecated `IMessage.LegacyProperty` property
- Changed signature of `IChannelConnector.SendAsync` method
```

## 🏆 Recognition

Contributors will be recognized in:

- **CONTRIBUTORS.md** file
- **Release notes** for significant contributions
- **GitHub contributors** page

## 💬 Community

- **GitHub Discussions**: Ask questions and share ideas
- **GitHub Issues**: Report bugs and request features
- **Email**: For private/security concerns, contact support@deveel.com

## 📄 License

By contributing to this project, you agree that your contributions will be licensed under the MIT License.

## ❓ Questions?

If you have questions about contributing:

1. Check existing [GitHub Discussions](https://github.com/deveel/deveel.messaging/discussions)
2. Review [documentation](docs/README.md)
3. Open a new discussion or issue
4. Contact the maintainers

Thank you for contributing to the Deveel Messaging Framework! 🙏