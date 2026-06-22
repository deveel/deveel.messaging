# Message Validation in Custom Connectors

This guide covers implementing custom message validation logic in custom connectors. For schema-based validation, see [Message Validation](../message-validation.md).

## Schema Validation Integration

The base class automatically validates messages against the schema before calling your `SendMessageCoreAsync()` method:

```csharp
// Base class validates message automatically
public async ValueTask<OperationResult<SendResult>> SendMessageAsync(
    IMessage message, CancellationToken cancellationToken)
{
    // Validate message against schema
    await foreach (var validationResult in ValidateMessageAsync(message, cancellationToken))
    {
        if (validationResult != ValidationResult.Success)
        {
            return OperationResult<SendResult>.ValidationFailed(
                ConnectorErrorCodes.MessageValidationFailed,
                MessagingErrorCodes.ErrorDomain,
                validationErrors);
        }
    }
    
    // Only calls your SendMessageCoreAsync() if validation passes
    return await SendMessageCoreAsync(message, cancellationToken);
}
```

## Adding Custom Validation Rules

Override `ValidateMessageCoreAsync()` to add connector-specific validation:

```csharp
public class MyConnector : ChannelConnectorBase
{
    protected override async IAsyncEnumerable<ValidationResult> ValidateMessageCoreAsync(
        IMessage message, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // First, run schema validation
        await foreach (var result in base.ValidateMessageCoreAsync(message, cancellationToken))
        {
            yield return result;
        }

        // Then add custom validation
        if (message.Receiver?.Address?.Length > 100)
        {
            yield return new ValidationResult(
                "Receiver address exceeds maximum length of 100 characters",
                new[] { "Receiver.Address" });
        }

        // Validate content-specific rules
        if (message.Content is TextContent text && text.Text.Length > 1000)
        {
            yield return new ValidationResult(
                "Text content exceeds maximum length of 1000 characters",
                new[] { "Content.Text" });
        }
    }
}
```

## Validation Patterns

### Check Message Properties

```csharp
protected override async IAsyncEnumerable<ValidationResult> ValidateMessageCoreAsync(
    IMessage message, [EnumeratorCancellation] CancellationToken cancellationToken)
{
    await foreach (var result in base.ValidateMessageCoreAsync(message, cancellationToken))
    {
        yield return result;
    }

    // Validate sender
    if (message.Sender == null)
    {
        yield return new ValidationResult(
            "Sender is required",
            new[] { "Sender" });
    }

    // Validate receiver
    if (message.Receiver == null)
    {
        yield return new ValidationResult(
            "Receiver is required",
            new[] { "Receiver" });
    }

    // Validate priority
    if (message.Priority > MessagePriority.High)
    {
        yield return new ValidationResult(
            "Priority cannot exceed High",
            new[] { "Priority" });
    }
}
```

### Validate Content

```csharp
protected override async IAsyncEnumerable<ValidationResult> ValidateMessageCoreAsync(
    IMessage message, [EnumeratorCancellation] CancellationToken cancellationToken)
{
    await foreach (var result in base.ValidateMessageCoreAsync(message, cancellationToken))
    {
        yield return result;
    }

    // Content type validation
    if (message.Content == null)
    {
        yield return new ValidationResult(
            "Content is required",
            new[] { "Content" });
        yield break;
    }

    // Text content validation
    if (message.Content is TextContent text)
    {
        if (string.IsNullOrWhiteSpace(text.Text))
        {
            yield return new ValidationResult(
                "Text content cannot be empty",
                new[] { "Content.Text" });
        }

        // Check for prohibited characters
        if (text.Text.Contains("\0"))
        {
            yield return new ValidationResult(
                "Text content contains null characters",
                new[] { "Content.Text" });
        }
    }

    // HTML content validation
    if (message.Content is HtmlContent html)
    {
        if (html.Html.Length > 10000)
        {
            yield return new ValidationResult(
                "HTML content exceeds maximum length",
                new[] { "Content.Html" });
        }

        // Check for prohibited tags
        if (html.Html.Contains("<script", StringComparison.OrdinalIgnoreCase))
        {
            yield return new ValidationResult(
                "Script tags are not allowed",
                new[] { "Content.Html" });
        }
    }

    // Media content validation
    if (message.Content is MediaContent media)
    {
        if (media.Url == null)
        {
            yield return new ValidationResult(
                "Media URL is required",
                new[] { "Content.Media.Url" });
        }
        else if (!Uri.IsWellFormedUriString(media.Url, UriKind.Absolute))
        {
            yield return new ValidationResult(
                "Media URL must be a valid absolute URL",
                new[] { "Content.Media.Url" });
        }

        // Validate file size if known
        if (media.SizeBytes > 10 * 1024 * 1024) // 10MB limit
        {
            yield return new ValidationResult(
                "Media file size exceeds 10MB limit",
                new[] { "Content.Media.SizeBytes" });
        }
    }
}
```

### Validate Message Properties

```csharp
protected override async IAsyncEnumerable<ValidationResult> ValidateMessageCoreAsync(
    IMessage message, [EnumeratorCancellation] CancellationToken cancellationToken)
{
    await foreach (var result in base.ValidateMessageCoreAsync(message, cancellationToken))
    {
        yield return result;
    }

    // Validate validity period
    if (message.ValidityPeriod.HasValue)
    {
        if (message.ValidityPeriod.Value <= 0)
        {
            yield return new ValidationResult(
                "Validity period must be positive",
                new[] { "ValidityPeriod" });
        }
        else if (message.ValidityPeriod.Value > TimeSpan.FromDays(7))
        {
            yield return new ValidationResult(
                "Validity period cannot exceed 7 days",
                new[] { "ValidityPeriod" });
        }
    }

    // Validate max price
    if (message.MaxPrice.HasValue)
    {
        if (message.MaxPrice.Value < 0)
        {
            yield return new ValidationResult(
                "Max price cannot be negative",
                new[] { "MaxPrice" });
        }
        else if (message.MaxPrice.Value > 100)
        {
            yield return new ValidationResult(
                "Max price cannot exceed 100",
                new[] { "MaxPrice" });
        }
    }

    // Validate callback URL
    if (!string.IsNullOrWhiteSpace(message.CallbackUrl))
    {
        if (!Uri.IsWellFormedUriString(message.CallbackUrl, UriKind.Absolute))
        {
            yield return new ValidationResult(
                "Callback URL must be a valid absolute URL",
                new[] { "CallbackUrl" });
        }
        else if (!message.CallbackUrl.StartsWith("https://"))
        {
            yield return new ValidationResult(
                "Callback URL must use HTTPS",
                new[] { "CallbackUrl" });
        }
    }
}
```

## ValidationResult Creation

Create validation results with clear error messages:

```csharp
// Simple validation error
yield return new ValidationResult(
    "Receiver address is required",
    new[] { "Receiver.Address" });

// Multiple field validation
yield return new ValidationResult(
    "Either Sender or From must be specified",
    new[] { "Sender", "From" });

// With error code
yield return new ValidationResult(
    "Invalid phone number format",
    new[] { "Receiver.Address" },
    errorCode: "INVALID_PHONE_FORMAT");
```

## Error Handling

Validation errors are automatically wrapped in `OperationResult<T>`:

```csharp
// Base class returns:
OperationResult<SendResult>.ValidationFailed(
    ConnectorErrorCodes.MessageValidationFailed,
    MessagingErrorCodes.ErrorDomain,
    validationErrors);  // List of ValidationResult
```

### Handling in Application Code

```csharp
var result = await connector.SendMessageAsync(message, ct);

if (result.IsFailure() && result.Error is IValidationError validationError)
{
    Console.WriteLine("Validation failed:");
    foreach (var error in validationError.Errors)
    {
        Console.WriteLine($"  - {error.ErrorMessage}");
        Console.WriteLine($"    Fields: {string.Join(", ", error.MemberNames)}");
    }
}
```

## Best Practices

### ✅ DO: Validate Early

Catch validation errors before making API calls:

```csharp
// ✅ Good - validates before API call
if (message.Content is TextContent text && text.Text.Length > 1000)
{
    yield return new ValidationResult("Text too long", new[] { "Content.Text" });
}
```

### ✅ DO: Provide Clear Error Messages

Help users fix validation errors:

```csharp
// ✅ Good - clear and actionable
yield return new ValidationResult(
    "Phone number must include country code (e.g., +1234567890)",
    new[] { "Receiver.Address" });

// ❌ Bad - vague
yield return new ValidationResult(
    "Invalid phone number",
    new[] { "Receiver.Address" });
```

### ✅ DO: Specify Affected Fields

Help users locate the problem:

```csharp
yield return new ValidationResult(
    "Required field",
    new[] { "Receiver.Address" });  // ← Field path
```

### ❌ DON'T: Validate What Schema Already Validates

Don't duplicate schema validation:

```csharp
// ❌ Redundant - schema already validates this
if (message.Receiver == null)
{
    yield return new ValidationResult("Receiver is required", ...);
}

// ✅ Good - add value beyond schema
if (message.Receiver?.Address?.Length > 100)
{
    yield return new ValidationResult("Address too long", ...);
}
```

### ❌ DON'T: Make External Calls

Validation should be fast and local:

```csharp
// ❌ Bad - makes API call during validation
var isValid = await _externalService.ValidateAddress(message.Receiver.Address);
if (!isValid) { ... }

// ✅ Good - validate locally
if (!IsValidPhoneNumberFormat(message.Receiver.Address))
{
    yield return new ValidationResult("Invalid format", ...);
}
```

## Testing Validation

Test validation logic without hitting the provider:

```csharp
[Fact]
public async Task ValidateMessageAsync_RejectsLongText()
{
    var connector = CreateTestConnector();
    await connector.InitializeAsync(CancellationToken.None);

    var message = new MessageBuilder()
        .WithId("test-1")
        .To(Endpoint.Id("recipient"))
        .WithText(new string('x', 2000))  // 2000 chars - too long
        .Build();

    var errors = new List<ValidationResult>();
    await foreach (var error in connector.ValidateMessageAsync(message, CancellationToken.None))
    {
        if (error != ValidationResult.Success)
            errors.Add(error);
    }

    Assert.NotEmpty(errors);
    Assert.Contains(errors, e => 
        e.ErrorMessage?.Contains("length", StringComparison.OrdinalIgnoreCase) == true);
}
```

## See Also

- [Message Validation](../message-validation.md) - Schema-based validation
- [Channel Schema](../channel-schema.md) - Defining validation rules
- [Minimum Implementation](minimum-implementation.md) - Basic connector setup
