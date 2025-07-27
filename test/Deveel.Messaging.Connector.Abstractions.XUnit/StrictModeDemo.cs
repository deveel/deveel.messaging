using Deveel.Messaging;

namespace Deveel.Messaging.Demo;

/// <summary>
/// Demonstrates the ChannelSchema strict mode functionality.
/// </summary>
public static class StrictModeDemo
{
    /// <summary>
    /// Runs the strict mode demonstration.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("=== ChannelSchema Strict Mode Demo ===\n");

        // 1. Create schemas with different modes
        Console.WriteLine("? Creating schemas with different validation modes...");

        var strictSchema = new ChannelSchema("Demo", "Email", "1.0.0")
            .WithDisplayName("Strict Email Schema")
            .AddParameter(new ChannelParameter("Host", ParameterType.String) { IsRequired = true })
            .AddParameter(new ChannelParameter("Port", ParameterType.Integer) { DefaultValue = 587 })
            .AddMessageProperty(new MessagePropertyConfiguration("Subject", ParameterType.String) { IsRequired = true });

        var flexibleSchema = new ChannelSchema("Demo", "Email", "1.0.0")
            .WithFlexibleMode()
            .WithDisplayName("Flexible Email Schema")
            .AddParameter(new ChannelParameter("Host", ParameterType.String) { IsRequired = true })
            .AddParameter(new ChannelParameter("Port", ParameterType.Integer) { DefaultValue = 587 })
            .AddMessageProperty(new MessagePropertyConfiguration("Subject", ParameterType.String) { IsRequired = true });

        Console.WriteLine($"   Strict Schema IsStrict: {strictSchema.IsStrict}");
        Console.WriteLine($"   Flexible Schema IsStrict: {flexibleSchema.IsStrict}");
        Console.WriteLine();

        // 2. Test connection settings validation
        Console.WriteLine("? Testing connection settings validation...");

        var connectionSettings = new ConnectionSettings()
            .SetParameter("Host", "smtp.example.com")
            .SetParameter("Port", 587)
            .SetParameter("CustomTimeout", 30000)      // Unknown parameter
            .SetParameter("DebugMode", true);          // Unknown parameter

        var strictConnectionResults = strictSchema.ValidateConnectionSettings(connectionSettings);
        var flexibleConnectionResults = flexibleSchema.ValidateConnectionSettings(connectionSettings);

        Console.WriteLine($"   Strict mode validation errors: {strictConnectionResults.Count()}");
        foreach (var error in strictConnectionResults)
        {
            Console.WriteLine($"   - {error.ErrorMessage}");
        }

        Console.WriteLine($"   Flexible mode validation errors: {flexibleConnectionResults.Count()}");
        Console.WriteLine();

        // 3. Test message properties validation
        Console.WriteLine("? Testing message properties validation...");

        var messageProperties = new Dictionary<string, object?>
        {
            { "Subject", "Test Message" },
            { "CustomTrackingId", "TRACK-123" },      // Unknown property
            { "DeveloperNotes", "Testing emails" }    // Unknown property
        };

        var strictMessageResults = strictSchema.ValidateMessageProperties(messageProperties);
        var flexibleMessageResults = flexibleSchema.ValidateMessageProperties(messageProperties);

        Console.WriteLine($"   Strict mode validation errors: {strictMessageResults.Count()}");
        foreach (var error in strictMessageResults)
        {
            Console.WriteLine($"   - {error.ErrorMessage}");
        }

        Console.WriteLine($"   Flexible mode validation errors: {flexibleMessageResults.Count()}");
        Console.WriteLine();

        // 4. Demonstrate fluent mode switching
        Console.WriteLine("? Demonstrating fluent mode switching...");

        var switchableSchema = new ChannelSchema("Demo", "Switchable", "1.0.0")
            .WithDisplayName("Mode Switchable Schema")
            .AddParameter(new ChannelParameter("Param1", ParameterType.String));

        Console.WriteLine($"   Initial mode (default): {switchableSchema.IsStrict}");

        switchableSchema.WithFlexibleMode();
        Console.WriteLine($"   After WithFlexibleMode(): {switchableSchema.IsStrict}");

        switchableSchema.WithStrictMode();
        Console.WriteLine($"   After WithStrictMode(): {switchableSchema.IsStrict}");

        switchableSchema.WithStrictMode(false);
        Console.WriteLine($"   After WithStrictMode(false): {switchableSchema.IsStrict}");
        Console.WriteLine();

        // 5. Demonstrate schema derivation with strict mode
        Console.WriteLine("? Testing schema derivation with strict mode preservation...");

        var baseSchema = new ChannelSchema("Base", "Universal", "1.0.0")
            .AddParameter(new ChannelParameter("BaseParam1", ParameterType.String))
            .AddParameter(new ChannelParameter("BaseParam2", ParameterType.String));

        var derivedSchema = new ChannelSchema(baseSchema, "Derived Schema")
            .RemoveParameter("BaseParam2");

        Console.WriteLine($"   Base schema IsStrict: {baseSchema.IsStrict}");
        Console.WriteLine($"   Derived schema IsStrict: {derivedSchema.IsStrict}");

        // Override strict mode in derived schema
        var flexibleDerived = new ChannelSchema(baseSchema, "Flexible Derived")
            .WithFlexibleMode()
            .RemoveParameter("BaseParam2");

        Console.WriteLine($"   Flexible derived IsStrict: {flexibleDerived.IsStrict}");
        Console.WriteLine();

        // 6. Real-world scenario: Production vs Development
        Console.WriteLine("? Real-world scenario: Production vs Development configurations...");

        // Production schema - strict mode for security (default behavior)
        var productionSchema = new ChannelSchema("SMTP", "Email", "2.0.0")
            .WithDisplayName("Production Email Connector")
            .AddParameter(new ChannelParameter("Host", ParameterType.String) { IsRequired = true })
            .AddParameter(new ChannelParameter("Username", ParameterType.String) { IsRequired = true })
            .AddParameter(new ChannelParameter("Password", ParameterType.String) { IsRequired = true, IsSensitive = true });

        // Development schema - flexible mode for testing
        var developmentSchema = new ChannelSchema("SMTP", "Email", "2.0.0")
            .WithFlexibleMode()
            .WithDisplayName("Development Email Connector")
            .AddParameter(new ChannelParameter("Host", ParameterType.String) { IsRequired = true })
            .AddParameter(new ChannelParameter("Username", ParameterType.String) { IsRequired = true })
            .AddParameter(new ChannelParameter("Password", ParameterType.String) { IsRequired = true, IsSensitive = true });

        // Development settings with custom debugging parameters
        var devSettings = new ConnectionSettings()
            .SetParameter("Host", "localhost")
            .SetParameter("Username", "dev@test.com")
            .SetParameter("Password", "dev-password")
            .SetParameter("DebugMode", true)           // Custom for debugging
            .SetParameter("LogLevel", "verbose")       // Custom for logging
            .SetParameter("TestRecipient", "test@dev.com"); // Custom for testing

        var prodValidation = productionSchema.ValidateConnectionSettings(devSettings);
        var devValidation = developmentSchema.ValidateConnectionSettings(devSettings);

        Console.WriteLine($"   Production schema validation errors: {prodValidation.Count()}");
        Console.WriteLine($"   Development schema validation errors: {devValidation.Count()}");

        Console.WriteLine("\n?? Strict mode demonstration completed successfully!");
        Console.WriteLine("\nKey Benefits:");
        Console.WriteLine("   • Strict mode (default) ensures only predefined parameters/properties are accepted");
        Console.WriteLine("   • Flexible mode allows custom extensions and backwards compatibility");
        Console.WriteLine("   • Mode is configured via fluent methods for cleaner API");
        Console.WriteLine("   • Schema derivation preserves or can override strict mode");
        Console.WriteLine("   • Perfect for production (strict) vs development (flexible) scenarios");
    }
}