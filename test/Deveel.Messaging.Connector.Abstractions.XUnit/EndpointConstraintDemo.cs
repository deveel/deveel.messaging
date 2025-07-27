using Deveel.Messaging;

// Example demonstrating the new unique endpoint type constraint

Console.WriteLine("=== ChannelSchema Unique Endpoint Type Constraint Demo ===\n");

// 1. Valid usage - different endpoint types
try
{
    Console.WriteLine("? Creating schema with different endpoint types...");
    var validSchema = new ChannelSchema("Demo", "Multi", "1.0.0")
        .HandlesMessageEndpoint(new ChannelEndpointConfiguration(EndpointType.EmailAddress)
        {
            CanSend = true,
            CanReceive = false
        })
        .HandlesMessageEndpoint(new ChannelEndpointConfiguration(EndpointType.PhoneNumber)
        {
            CanSend = false,
            CanReceive = true
        })
        .HandlesMessageEndpoint(new ChannelEndpointConfiguration(EndpointType.Url)
        {
            CanSend = true,
            CanReceive = true
        });
    
    Console.WriteLine($"   Schema created successfully with {validSchema.Endpoints.Count} endpoint types.");
    foreach (var endpoint in validSchema.Endpoints)
    {
        Console.WriteLine($"   - {endpoint.Type}: Send={endpoint.CanSend}, Receive={endpoint.CanReceive}");
    }
    Console.WriteLine();
}
catch (Exception ex)
{
    Console.WriteLine($"   ? Unexpected error: {ex.Message}\n");
}

// 2. Invalid usage - duplicate endpoint type
try
{
    Console.WriteLine("? Attempting to add duplicate endpoint type...");
    var invalidSchema = new ChannelSchema("Demo", "Invalid", "1.0.0")
        .HandlesMessageEndpoint(new ChannelEndpointConfiguration(EndpointType.EmailAddress)
        {
            CanSend = true,
            CanReceive = false
        });
    
    // This should throw an exception
    invalidSchema.HandlesMessageEndpoint(new ChannelEndpointConfiguration(EndpointType.EmailAddress)
    {
        CanSend = false,
        CanReceive = true
    });
    
    Console.WriteLine("   ? ERROR: Duplicate was allowed (this shouldn't happen!)\n");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"   ? Correctly prevented duplicate: {ex.Message}\n");
}

// 3. Wildcard endpoint constraint
try
{
    Console.WriteLine("? Attempting to add multiple wildcard endpoints...");
    var wildcardSchema = new ChannelSchema("Demo", "Wildcard", "1.0.0")
        .AllowsAnyMessageEndpoint();
    
    // This should throw an exception
    wildcardSchema.AllowsAnyMessageEndpoint();
    
    Console.WriteLine("   ? ERROR: Multiple wildcards were allowed (this shouldn't happen!)\n");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"   ? Correctly prevented multiple wildcards: {ex.Message}\n");
}

// 4. Test EndpointType.Any functionality
try
{
    Console.WriteLine("? Testing EndpointType.Any functionality...");
    var anySchema = new ChannelSchema("Demo", "Any", "1.0.0")
        .AllowsAnyMessageEndpoint();
    
    var anyEndpoint = anySchema.Endpoints.First();
    Console.WriteLine($"   Any endpoint type: {anyEndpoint.Type}");
    Console.WriteLine($"   Is wildcard: {anyEndpoint.IsWildcard}");
    Console.WriteLine($"   Matches EmailAddress: {anyEndpoint.Matches(EndpointType.EmailAddress)}");
    Console.WriteLine($"   Matches PhoneNumber: {anyEndpoint.Matches(EndpointType.PhoneNumber)}");
    Console.WriteLine();
}
catch (Exception ex)
{
    Console.WriteLine($"   ? Unexpected error: {ex.Message}\n");
}

Console.WriteLine("Demo completed successfully! ??");