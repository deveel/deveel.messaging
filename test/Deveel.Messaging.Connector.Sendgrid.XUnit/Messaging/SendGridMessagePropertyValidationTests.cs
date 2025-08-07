using System.ComponentModel.DataAnnotations;

namespace Deveel.Messaging;

/// <summary>
/// Tests for the SendGrid message property validation to verify
/// message property validation and processing for SendGrid email messages.
/// </summary>
public class SendGridMessagePropertyValidationTests
{
    [Fact]
    public void SendGridEmail_SubjectProperty_ValidatesCorrectly()
    {
        // Arrange
        var schema = SendGridChannelSchemas.SendGridEmail;
        var subjectProperty = schema.MessageProperties.First(p => p.Name == "Subject");

        // Act & Assert - Valid subject
        var validResults = subjectProperty.Validate("Test Email Subject");
        Assert.Empty(validResults);

        // Act & Assert - Empty subject
        var emptyResults = subjectProperty.Validate("").ToList();
        Assert.Single(emptyResults);
        Assert.Contains("cannot be empty", emptyResults[0].ErrorMessage);

        // Act & Assert - Too long subject
        var longSubject = new string('a', 999);
        var longResults = subjectProperty.Validate(longSubject).ToList();
        Assert.Single(longResults);
        Assert.Contains("cannot exceed 998 characters", longResults[0].ErrorMessage);
    }

    [Fact]
    public void SendGridEmail_PriorityProperty_ValidatesCorrectly()
    {
        // Arrange
        var schema = SendGridChannelSchemas.SendGridEmail;
        var priorityProperty = schema.MessageProperties.First(p => p.Name == "Priority");

        // Act & Assert - Valid priorities
        Assert.Empty(priorityProperty.Validate("low"));
        Assert.Empty(priorityProperty.Validate("normal"));
        Assert.Empty(priorityProperty.Validate("high"));

        // Act & Assert - Invalid priority
        var invalidResults = priorityProperty.Validate("invalid_priority").ToList();
        Assert.Single(invalidResults);
        Assert.Contains("invalid value", invalidResults[0].ErrorMessage);
    }

    [Fact]
    public void SendGridEmail_CategoriesProperty_ValidatesCorrectly()
    {
        // Arrange
        var schema = SendGridChannelSchemas.SendGridEmail;
        var categoriesProperty = schema.MessageProperties.First(p => p.Name == "Categories");

        // Act & Assert - Valid categories
        var validResults = categoriesProperty.Validate("newsletter,marketing");
        Assert.Empty(validResults);

        // Act & Assert - Too many categories
        var categories = string.Join(",", Enumerable.Range(1, 15).Select(i => $"category{i}"));
        var tooManyResults = categoriesProperty.Validate(categories).ToList();
        Assert.Single(tooManyResults);
        Assert.Contains("more than 10 categories", tooManyResults[0].ErrorMessage);

        // Act & Assert - Category name too long
        var longCategoryName = new string('a', 260);
        var longNameResults = categoriesProperty.Validate(longCategoryName).ToList();
        Assert.Single(longNameResults);
        Assert.Contains("exceed 255 characters", longNameResults[0].ErrorMessage);
    }

    [Fact]
    public void SendGridEmail_CustomArgsProperty_ValidatesCorrectly()
    {
        // Arrange
        var schema = SendGridChannelSchemas.SendGridEmail;
        var customArgsProperty = schema.MessageProperties.First(p => p.Name == "CustomArgs");

        // Act & Assert - Valid JSON
        var validResults = customArgsProperty.Validate("{\"userId\":\"123\",\"campaignId\":\"abc\"}");
        Assert.Empty(validResults);

        // Act & Assert - Invalid JSON
        var invalidResults = customArgsProperty.Validate("invalid json").ToList();
        Assert.Single(invalidResults);
        Assert.Contains("valid JSON", invalidResults[0].ErrorMessage);
    }

    [Fact]
    public void SendGridEmail_AsmGroupIdProperty_ValidatesCorrectly()
    {
        // Arrange
        var schema = SendGridChannelSchemas.SendGridEmail;
        var asmGroupIdProperty = schema.MessageProperties.First(p => p.Name == "AsmGroupId");

        // Act & Assert - Valid positive integer
        Assert.Empty(asmGroupIdProperty.Validate(12345));

        // Act & Assert - Invalid negative integer
        var negativeResults = asmGroupIdProperty.Validate(-1).ToList();
        Assert.Single(negativeResults);
        Assert.Contains("at least 1", negativeResults[0].ErrorMessage);

        // Act & Assert - Invalid type
        var invalidTypeResults = asmGroupIdProperty.Validate("not_a_number").ToList();
        Assert.Single(invalidTypeResults);
        Assert.Contains("incompatible type", invalidTypeResults[0].ErrorMessage);
    }

    [Fact]
    public void SendGridEmail_SendAtProperty_ValidatesCorrectly()
    {
        // Arrange
        var schema = SendGridChannelSchemas.SendGridEmail;
        var sendAtProperty = schema.MessageProperties.First(p => p.Name == "SendAt");

        // Act & Assert - Valid future time
        var futureTime = DateTime.UtcNow.AddHours(2);
        var validResults = sendAtProperty.Validate(futureTime);
        Assert.Empty(validResults);

        // Act & Assert - Past time
        var pastTime = DateTime.UtcNow.AddHours(-1);
        var pastResults = sendAtProperty.Validate(pastTime).ToList();
        Assert.Single(pastResults);
        Assert.Contains("future date", pastResults[0].ErrorMessage);

        // Act & Assert - Too far in future
        var tooFarFuture = DateTime.UtcNow.AddDays(80);
        var tooFarResults = sendAtProperty.Validate(tooFarFuture).ToList();
        Assert.Single(tooFarResults);
        Assert.Contains("72 hours", tooFarResults[0].ErrorMessage);
    }

    [Fact]
    public void SendGridEmail_SchemaValidation_WithValidProperties_ReturnsNoErrors()
    {
        // Arrange
        var schema = SendGridChannelSchemas.SendGridEmail;
        var properties = new Dictionary<string, object?>
        {
            ["Subject"] = "Test Email Subject",
            ["Priority"] = "high",
            ["Categories"] = "newsletter,marketing",
            ["CustomArgs"] = "{\"userId\":\"123\",\"campaignId\":\"abc\"}",
            ["AsmGroupId"] = 12345,
            ["IpPoolName"] = "marketing_pool"
        };

        // Act
        var message = CreateTestMessage(properties);
        var results = schema.ValidateMessage(message);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void SendGridEmail_SchemaValidation_WithMissingSubject_ReturnsValidationError()
    {
        // Arrange
        var schema = SendGridChannelSchemas.SendGridEmail;
        var properties = new Dictionary<string, object?>
        {
            ["Priority"] = "high"
        };

        // Act
        var message = CreateTestMessage(properties);
        var results = schema.ValidateMessage(message).ToList();

        // Assert
        Assert.Single(results);
        Assert.Contains("Required message property 'Subject' is missing", results[0].ErrorMessage);
    }

    [Fact]
    public void SendGridEmail_SchemaValidation_WithMultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var schema = SendGridChannelSchemas.SendGridEmail;
        var properties = new Dictionary<string, object?>
        {
            ["Subject"] = "", // Empty subject
            ["Priority"] = "invalid", // Invalid priority
            ["Categories"] = string.Join(",", Enumerable.Range(1, 15).Select(i => $"cat{i}")), // Too many categories
            ["CustomArgs"] = "invalid json", // Invalid JSON
            ["AsmGroupId"] = -1, // Negative integer
            ["SendAt"] = DateTime.UtcNow.AddHours(-1) // Past time
        };

        // Act
        var message = CreateTestMessage(properties);
        var results = schema.ValidateMessage(message).ToList();

        // Assert
        Assert.True(results.Count >= 6); // Should have at least 6 validation errors
        Assert.Contains(results, r => r.ErrorMessage!.Contains("Subject"));
        Assert.Contains(results, r => r.ErrorMessage!.Contains("Priority"));
        Assert.Contains(results, r => r.ErrorMessage!.Contains("categories"));
        Assert.Contains(results, r => r.ErrorMessage!.Contains("JSON"));
        Assert.Contains(results, r => r.ErrorMessage!.Contains("AsmGroupId"));
        Assert.Contains(results, r => r.ErrorMessage!.Contains("SendAt"));
    }

    [Fact]
    public void SimpleEmail_Schema_DoesNotHaveAdvancedProperties()
    {
        // Arrange
        var schema = SendGridChannelSchemas.SimpleEmail;

        // Act & Assert
        Assert.DoesNotContain(schema.MessageProperties, p => p.Name == "Categories");
        Assert.DoesNotContain(schema.MessageProperties, p => p.Name == "CustomArgs");
        Assert.DoesNotContain(schema.MessageProperties, p => p.Name == "SendAt");
        Assert.DoesNotContain(schema.MessageProperties, p => p.Name == "BatchId");
        Assert.DoesNotContain(schema.MessageProperties, p => p.Name == "AsmGroupId");
        
        // But should still have basic properties
        Assert.Contains(schema.MessageProperties, p => p.Name == "Subject");
        Assert.Contains(schema.MessageProperties, p => p.Name == "Priority");
    }

    [Fact]
    public void TemplateEmail_Schema_HasTemplateProperties()
    {
        // Arrange
        var schema = SendGridChannelSchemas.TemplateEmail;

        // Act & Assert
        Assert.Contains(schema.MessageProperties, p => p.Name == "TemplateId");
        Assert.Contains(schema.MessageProperties, p => p.Name == "TemplateData");
        
        // Template ID should be required
        var templateIdProperty = schema.MessageProperties.First(p => p.Name == "TemplateId");
        Assert.True(templateIdProperty.IsRequired);
    }
    
    #region Helper Methods

    private static Message CreateTestMessage(IDictionary<string, object?> properties)
    {
        return new Message
        {
            Id = "test-message-id",
            Content = new TextContent("Test email content"),
            Properties = properties?.ToDictionary(
                kvp => kvp.Key,
                kvp => new MessageProperty(kvp.Key, kvp.Value),
                StringComparer.OrdinalIgnoreCase)
        };
    }

    #endregion
}