using System.ComponentModel.DataAnnotations;

namespace Deveel.Messaging;

/// <summary>
/// Tests for the strict mode functionality of the <see cref="ChannelSchema"/> class.
/// </summary>
public class ChannelSchemaStrictModeTests
{
	[Fact]
	public void Constructor_DefaultIsStrict_ShouldBeTrue()
	{
		// Arrange & Act
		var schema = new ChannelSchema("Provider", "Type", "1.0.0");

		// Assert
		Assert.True(schema.IsStrict);
	}

	[Fact]
	public void Constructor_WithStrictModeConfigured_ShouldSetCorrectly()
	{
		// Arrange & Act
		var strictSchema = new ChannelSchema("Provider", "Type", "1.0.0");
		var flexibleSchema = new ChannelSchema("Provider", "Type", "1.0.0").WithFlexibleMode();

		// Assert
		Assert.True(strictSchema.IsStrict);
		Assert.False(flexibleSchema.IsStrict);
	}

	[Fact]
	public void CopyConstructor_CopiesStrictMode_FromSourceSchema()
	{
		// Arrange
		var strictSourceSchema = new ChannelSchema("Provider", "Type", "1.0.0");
		var flexibleSourceSchema = new ChannelSchema("Provider", "Type", "1.0.0").WithFlexibleMode();

		// Act
		var strictCopy = new ChannelSchema(strictSourceSchema, "Strict Copy");
		var flexibleCopy = new ChannelSchema(flexibleSourceSchema, "Flexible Copy");

		// Assert
		Assert.True(strictCopy.IsStrict);
		Assert.False(flexibleCopy.IsStrict);
	}

	[Fact]
	public void WithStrictMode_SetsStrictModeCorrectly()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0").WithFlexibleMode();

		// Act
		var result = schema.WithStrictMode(true);

		// Assert
		Assert.Same(schema, result);
		Assert.True(schema.IsStrict);
	}

	[Fact]
	public void WithStrictMode_NoParameters_EnablesStrictMode()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0").WithFlexibleMode();

		// Act
		var result = schema.WithStrictMode();

		// Assert
		Assert.Same(schema, result);
		Assert.True(schema.IsStrict);
	}

	[Fact]
	public void WithFlexibleMode_DisablesStrictMode()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0");

		// Act
		var result = schema.WithFlexibleMode();

		// Assert
		Assert.Same(schema, result);
		Assert.False(schema.IsStrict);
	}

	[Fact]
	public void ValidateConnectionSettings_StrictMode_RejectsUnknownParameters()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0")
			.AddParameter("KnownParam", DataType.String);

		var connectionSettings = new ConnectionSettings()
			.SetParameter("KnownParam", "value")
			.SetParameter("UnknownParam", "unknown value");

		// Act
		var results = schema.ValidateConnectionSettings(connectionSettings).ToList();

		// Assert
		Assert.Single(results);
		Assert.Contains("Unknown parameter 'UnknownParam' is not supported by this schema", results[0].ErrorMessage);
		Assert.Contains("UnknownParam", results[0].MemberNames);
	}

	[Fact]
	public void ValidateConnectionSettings_FlexibleMode_AllowsUnknownParameters()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0")
			.WithFlexibleMode()
			.AddParameter("KnownParam", DataType.String);

		var connectionSettings = new ConnectionSettings()
			.SetParameter("KnownParam", "value")
			.SetParameter("UnknownParam", "unknown value");

		// Act
		var results = schema.ValidateConnectionSettings(connectionSettings);

		// Assert
		Assert.Empty(results);
	}

	[Fact]
	public void ValidateMessageProperties_StrictMode_RejectsUnknownProperties()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0")
			.AddMessageProperty(new MessagePropertyConfiguration("KnownProperty", DataType.String));

		var messageProperties = new Dictionary<string, object?>
		{
			{ "KnownProperty", "value" },
			{ "UnknownProperty", "unknown value" }
		};

		// Act
		var results = schema.ValidateMessageProperties(messageProperties).ToList();

		// Assert
		Assert.Single(results);
		Assert.Contains("Unknown message property 'UnknownProperty' is not supported by this schema", results[0].ErrorMessage);
		Assert.Contains("UnknownProperty", results[0].MemberNames);
	}

	[Fact]
	public void ValidateMessageProperties_FlexibleMode_AllowsUnknownProperties()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0")
			.WithFlexibleMode()
			.AddMessageProperty(new MessagePropertyConfiguration("KnownProperty", DataType.String));

		var messageProperties = new Dictionary<string, object?>
		{
			{ "KnownProperty", "value" },
			{ "UnknownProperty", "unknown value" }
		};

		// Act
		var results = schema.ValidateMessageProperties(messageProperties);

		// Assert
		Assert.Empty(results);
	}

	[Fact]
	public void ValidateConnectionSettings_StrictMode_StillValidatesRequiredParameters()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0")
			.AddRequiredParameter("RequiredParam", DataType.String);

		var connectionSettings = new ConnectionSettings();

		// Act
		var results = schema.ValidateConnectionSettings(connectionSettings).ToList();

		// Assert
		Assert.Single(results);
		Assert.Contains("Required parameter 'RequiredParam' is missing", results[0].ErrorMessage);
	}

	[Fact]
	public void ValidateConnectionSettings_FlexibleMode_StillValidatesRequiredParameters()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0")
			.WithFlexibleMode()
			.AddRequiredParameter("RequiredParam", DataType.String);

		var connectionSettings = new ConnectionSettings();

		// Act
		var results = schema.ValidateConnectionSettings(connectionSettings).ToList();

		// Assert
		Assert.Single(results);
		Assert.Contains("Required parameter 'RequiredParam' is missing", results[0].ErrorMessage);
	}

	[Fact]
	public void ValidateConnectionSettings_StrictMode_StillValidatesParameterTypes()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0")
			.AddParameter("IntParam", DataType.Integer);

		var connectionSettings = new ConnectionSettings()
			.SetParameter("IntParam", "not an integer");

		// Act
		var results = schema.ValidateConnectionSettings(connectionSettings).ToList();

		// Assert
		Assert.Single(results);
		Assert.Contains("Parameter 'IntParam' has an incompatible type", results[0].ErrorMessage);
	}

	[Fact]
	public void ValidateConnectionSettings_FlexibleMode_StillValidatesParameterTypes()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0")
			.WithFlexibleMode()
			.AddParameter("IntParam", DataType.Integer);

		var connectionSettings = new ConnectionSettings()
			.SetParameter("IntParam", "not an integer");

		// Act
		var results = schema.ValidateConnectionSettings(connectionSettings).ToList();

		// Assert
		Assert.Single(results);
		Assert.Contains("Parameter 'IntParam' has an incompatible type", results[0].ErrorMessage);
	}

	[Fact]
	public void ValidateMessageProperties_StrictMode_StillValidatesRequiredProperties()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0")
			.AddMessageProperty(new MessagePropertyConfiguration("RequiredProp", DataType.String) { IsRequired = true });

		var messageProperties = new Dictionary<string, object?>();

		// Act
		var results = schema.ValidateMessageProperties(messageProperties).ToList();

		// Assert
		Assert.Single(results);
		Assert.Contains("Required message property 'RequiredProp' is missing", results[0].ErrorMessage);
	}

	[Fact]
	public void ValidateMessageProperties_FlexibleMode_StillValidatesRequiredProperties()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0")
			.WithFlexibleMode()
			.AddMessageProperty(new MessagePropertyConfiguration("RequiredProp", DataType.String) { IsRequired = true });

		var messageProperties = new Dictionary<string, object?>();

		// Act
		var results = schema.ValidateMessageProperties(messageProperties).ToList();

		// Assert
		Assert.Single(results);
		Assert.Contains("Required message property 'RequiredProp' is missing", results[0].ErrorMessage);
	}

	[Fact]
	public void ValidateMessageProperties_StrictMode_StillValidatesPropertyTypes()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0")
			.AddMessageProperty(new MessagePropertyConfiguration("IntProp", DataType.Integer));

		var messageProperties = new Dictionary<string, object?>
		{
			{ "IntProp", "not an integer" }
		};

		// Act
		var results = schema.ValidateMessageProperties(messageProperties).ToList();

		// Assert
		Assert.Single(results);
		Assert.Contains("Message property 'IntProp' has an incompatible type", results[0].ErrorMessage);
	}

	[Fact]
	public void ValidateMessageProperties_FlexibleMode_StillValidatesPropertyTypes()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0")
			.WithFlexibleMode()
			.AddMessageProperty(new MessagePropertyConfiguration("IntProp", DataType.Integer));

		var messageProperties = new Dictionary<string, object?>
		{
			{ "IntProp", "not an integer" }
		};

		// Act
		var results = schema.ValidateMessageProperties(messageProperties).ToList();

		// Assert
		Assert.Single(results);
		Assert.Contains("Message property 'IntProp' has an incompatible type", results[0].ErrorMessage);
	}

	[Fact]
	public void FluentConfiguration_StrictModeIntegration_WorksCorrectly()
	{
		// Arrange & Act
		var strictSchema = new ChannelSchema("Provider", "Type", "1.0.0")
			.WithDisplayName("Strict Schema")
			.WithStrictMode()
			.AddParameter("Param1", DataType.String)
			.AddMessageProperty(new MessagePropertyConfiguration("Prop1", DataType.String));

		var flexibleSchema = new ChannelSchema("Provider", "Type", "1.0.0")
			.WithDisplayName("Flexible Schema")
			.WithFlexibleMode()
			.AddParameter("Param1", DataType.String)
			.AddMessageProperty(new MessagePropertyConfiguration("Prop1", DataType.String));

		// Assert
		Assert.True(strictSchema.IsStrict);
		Assert.False(flexibleSchema.IsStrict);
		Assert.Equal("Strict Schema", strictSchema.DisplayName);
		Assert.Equal("Flexible Schema", flexibleSchema.DisplayName);
	}

	[Fact]
	public void StrictModeToggle_CanChangeBetweenModes()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0");

		// Act & Assert - Start strict (default)
		Assert.True(schema.IsStrict);

		// Switch to flexible
		schema.WithFlexibleMode();
		Assert.False(schema.IsStrict);

		// Switch back to strict
		schema.WithStrictMode();
		Assert.True(schema.IsStrict);

		// Switch using boolean parameter
		schema.WithStrictMode(false);
		Assert.False(schema.IsStrict);

		schema.WithStrictMode(true);
		Assert.True(schema.IsStrict);
	}

	[Fact]
	public void ComplexValidationScenario_StrictVsFlexible_DemonstratesDifference()
	{
		// Arrange
		var strictSchema = new ChannelSchema("Provider", "Type", "1.0.0")
			.AddRequiredParameter("RequiredParam", DataType.String)
			.AddParameter("OptionalParam", DataType.Integer)
			.AddMessageProperty(new MessagePropertyConfiguration("RequiredProp", DataType.String) { IsRequired = true })
			.AddMessageProperty(new MessagePropertyConfiguration("OptionalProp", DataType.Boolean) { IsRequired = false });

		var flexibleSchema = new ChannelSchema("Provider", "Type", "1.0.0")
			.WithFlexibleMode()
			.AddRequiredParameter("RequiredParam", DataType.String)
			.AddParameter("OptionalParam", DataType.Integer)
			.AddMessageProperty(new MessagePropertyConfiguration("RequiredProp", DataType.String) { IsRequired = true })
			.AddMessageProperty(new MessagePropertyConfiguration("OptionalProp", DataType.Boolean) { IsRequired = false });

		var connectionSettings = new ConnectionSettings()
			.SetParameter("RequiredParam", "valid value")
			.SetParameter("OptionalParam", 123)
			.SetParameter("CustomParam1", "custom value 1")
			.SetParameter("CustomParam2", "custom value 2");

		var messageProperties = new Dictionary<string, object?>
		{
			{ "RequiredProp", "valid value" },
			{ "OptionalProp", true },
			{ "CustomProp1", "custom value 1" },
			{ "CustomProp2", "custom value 2" }
		};

		// Act - Validate with strict schema
		var strictConnectionResults = strictSchema.ValidateConnectionSettings(connectionSettings).ToList();
		var strictMessageResults = strictSchema.ValidateMessageProperties(messageProperties).ToList();

		// Act - Validate with flexible schema
		var flexibleConnectionResults = flexibleSchema.ValidateConnectionSettings(connectionSettings).ToList();
		var flexibleMessageResults = flexibleSchema.ValidateMessageProperties(messageProperties).ToList();

		// Assert - Strict schema rejects unknown parameters and properties
		Assert.Equal(2, strictConnectionResults.Count);
		Assert.Contains(strictConnectionResults, r => r.ErrorMessage!.Contains("Unknown parameter 'CustomParam1'"));
		Assert.Contains(strictConnectionResults, r => r.ErrorMessage!.Contains("Unknown parameter 'CustomParam2'"));

		Assert.Equal(2, strictMessageResults.Count);
		Assert.Contains(strictMessageResults, r => r.ErrorMessage!.Contains("Unknown message property 'CustomProp1'"));
		Assert.Contains(strictMessageResults, r => r.ErrorMessage!.Contains("Unknown message property 'CustomProp2'"));

		// Assert - Flexible schema allows unknown parameters and properties
		Assert.Empty(flexibleConnectionResults);
		Assert.Empty(flexibleMessageResults);
	}

	[Fact]
	public void SchemaDerivation_PreservesStrictMode()
	{
		// Arrange
		var strictBaseSchema = new ChannelSchema("Provider", "Type", "1.0.0")
			.AddParameter("Param1", DataType.String)
			.AddParameter("Param2", DataType.String);

		var flexibleBaseSchema = new ChannelSchema("Provider", "Type", "1.0.0")
			.WithFlexibleMode()
			.AddParameter("Param1", DataType.String)
			.AddParameter("Param2", DataType.String);

		// Act
		var strictDerived = new ChannelSchema(strictBaseSchema, "Strict Derived")
			.RemoveParameter("Param2");

		var flexibleDerived = new ChannelSchema(flexibleBaseSchema, "Flexible Derived")
			.RemoveParameter("Param2");

		// Assert
		Assert.True(strictDerived.IsStrict);
		Assert.False(flexibleDerived.IsStrict);

		// Verify strict derived still rejects unknown parameters
		var connectionSettings = new ConnectionSettings()
			.SetParameter("Param1", "value")
			.SetParameter("UnknownParam", "unknown");

		var strictResults = strictDerived.ValidateConnectionSettings(connectionSettings).ToList();
		var flexibleResults = flexibleDerived.ValidateConnectionSettings(connectionSettings).ToList();

		Assert.Single(strictResults);
		Assert.Contains("Unknown parameter 'UnknownParam'", strictResults[0].ErrorMessage);

		Assert.Empty(flexibleResults);
	}

	[Fact]
	public void SchemaDerivation_CanOverrideStrictMode()
	{
		// Arrange
		var strictBaseSchema = new ChannelSchema("Provider", "Type", "1.0.0");

		// Act - Derive from strict base but make it flexible
		var flexibleDerived = new ChannelSchema(strictBaseSchema, "Flexible Derived")
			.WithFlexibleMode();

		// Assert
		Assert.True(strictBaseSchema.IsStrict);
		Assert.False(flexibleDerived.IsStrict);
	}
}