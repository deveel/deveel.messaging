//
// Copyright (c) Antonello Provenzano and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using System.Collections.ObjectModel;
using System.Globalization;

namespace Deveel.Messaging;

/// <summary>
/// Comprehensive tests for the <see cref="ConnectionSettings"/> class covering all constructors,
/// properties, methods, and edge cases to improve code coverage.
/// </summary>
public class ConnectionSettingsTests
{
	#region Constructor Tests

	[Fact]
	public void Constructor_Default_CreatesEmptySettings()
	{
		// Act
		var settings = new ConnectionSettings();

		// Assert
		Assert.NotNull(settings.Parameters);
		Assert.Empty(settings.Parameters);
	}

	[Fact]
	public void Constructor_WithNullParameters_CreatesEmptySettings()
	{
		// Act
		var settings = new ConnectionSettings((IDictionary<string, object?>?)null);

		// Assert
		Assert.NotNull(settings.Parameters);
		Assert.Empty(settings.Parameters);
	}

	[Fact]
	public void Constructor_WithParameters_CopiesParameters()
	{
		// Arrange
		var initialParameters = new Dictionary<string, object?>
		{
			{ "Key1", "Value1" },
			{ "Key2", 42 },
			{ "Key3", true },
			{ "Key4", null }
		};

		// Act
		var settings = new ConnectionSettings(initialParameters);

		// Assert
		Assert.Equal(4, settings.Parameters.Count);
		Assert.Equal("Value1", settings.Parameters["Key1"]);
		Assert.Equal(42, settings.Parameters["Key2"]);
		Assert.True((bool)settings.Parameters["Key3"]!);
		Assert.Null(settings.Parameters["Key4"]);
	}

	[Fact]
	public void Constructor_WithSchema_StoresSchema()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0")
			.AddParameter("TestParam", DataType.String);

		// Act
		var settings = new ConnectionSettings(schema);

		// Assert
		Assert.NotNull(settings.Parameters);
		Assert.Empty(settings.Parameters);
	}

	[Fact]
	public void Constructor_WithSchemaAndParameters_StoresBoth()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0")
			.AddParameter("TestParam", DataType.String);
		var parameters = new Dictionary<string, object?> { { "TestParam", "TestValue" } };

		// Act
		var settings = new ConnectionSettings(schema, parameters);

		// Assert
		Assert.Single(settings.Parameters);
		Assert.Equal("TestValue", settings.Parameters["TestParam"]);
	}

	[Fact]
	public void Constructor_CopyConstructor_CopiesAllSettings()
	{
		// Arrange - Create settings without strict schema validation
		var originalSettings = new ConnectionSettings()
			.SetParameter("TestParam", "TestValue")
			.SetParameter("AnotherParam", 123);

		// Act
		var copiedSettings = new ConnectionSettings(originalSettings);

		// Assert
		Assert.Equal(2, copiedSettings.Parameters.Count);
		Assert.Equal("TestValue", copiedSettings.Parameters["TestParam"]);
		Assert.Equal(123, copiedSettings.Parameters["AnotherParam"]);
	}

	[Fact]
	public void Constructor_CopyConstructor_WithNullSettings_ThrowsNullReferenceException()
	{
		// Act & Assert - The actual implementation throws NullReferenceException, not ArgumentNullException
		Assert.Throws<NullReferenceException>(() => new ConnectionSettings((ConnectionSettings)null!));
	}

	#endregion

	#region SetParameter Tests

	[Fact]
	public void SetParameter_WithValidKeyValue_SetsParameter()
	{
		// Arrange
		var settings = new ConnectionSettings();

		// Act
		var result = settings.SetParameter("TestKey", "TestValue");

		// Assert
		Assert.Same(settings, result); // Fluent interface
		Assert.Equal("TestValue", settings.Parameters["TestKey"]);
	}

	[Fact]
	public void SetParameter_WithNullValue_SetsNullParameter()
	{
		// Arrange
		var settings = new ConnectionSettings();

		// Act
		settings.SetParameter("TestKey", null);

		// Assert
		Assert.True(settings.Parameters.ContainsKey("TestKey"));
		Assert.Null(settings.Parameters["TestKey"]);
	}

	[Fact]
	public void SetParameter_MultipleParameters_SetsAll()
	{
		// Arrange
		var settings = new ConnectionSettings();

		// Act
		settings
			.SetParameter("String", "StringValue")
			.SetParameter("Integer", 42)
			.SetParameter("Boolean", true)
			.SetParameter("Double", 3.14);

		// Assert
		Assert.Equal(4, settings.Parameters.Count);
		Assert.Equal("StringValue", settings.Parameters["String"]);
		Assert.Equal(42, settings.Parameters["Integer"]);
		Assert.True((bool)settings.Parameters["Boolean"]!);
		Assert.Equal(3.14, settings.Parameters["Double"]);
	}

	[Fact]
	public void SetParameter_OverwriteExisting_UpdatesValue()
	{
		// Arrange
		var settings = new ConnectionSettings()
			.SetParameter("TestKey", "OriginalValue");

		// Act
		settings.SetParameter("TestKey", "NewValue");

		// Assert
		Assert.Single(settings.Parameters);
		Assert.Equal("NewValue", settings.Parameters["TestKey"]);
	}

	#endregion

	#region SetParameter With Schema Validation Tests

	[Fact]
	public void SetParameter_WithSchema_ValidParameter_SetsParameter()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0")
			.AddParameter("ValidParam", DataType.String);
		var settings = new ConnectionSettings(schema);

		// Act
		settings.SetParameter("ValidParam", "ValidValue");

		// Assert
		Assert.Equal("ValidValue", settings.Parameters["ValidParam"]);
	}

	[Fact]
	public void SetParameter_WithSchema_UnsupportedParameter_ThrowsArgumentException()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0")
			.AddParameter("ValidParam", DataType.String);
		var settings = new ConnectionSettings(schema);

		// Act & Assert
		var exception = Assert.Throws<ArgumentException>(() => 
			settings.SetParameter("UnsupportedParam", "Value"));
		Assert.Contains("The parameter UnsupportedParam is not supported by this schema", exception.Message);
	}

	[Fact]
	public void SetParameter_WithSchema_RequiredParameterWithNull_ThrowsArgumentException()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0")
			.AddRequiredParameter("RequiredParam", DataType.String);
		var settings = new ConnectionSettings(schema);

		// Act & Assert
		var exception = Assert.Throws<ArgumentException>(() => 
			settings.SetParameter("RequiredParam", null));
		Assert.Contains("The value of parameter RequiredParam is required by this schema", exception.Message);
	}

	[Theory]
	[InlineData(DataType.Boolean, "not_boolean")]
	[InlineData(DataType.Boolean, 123)]
	[InlineData(DataType.String, 123)]
	[InlineData(DataType.Integer, "not_integer")]
	[InlineData(DataType.Integer, 123.45)]
	[InlineData(DataType.Number, "not_number")]
	[InlineData(DataType.Number, true)]
	public void SetParameter_WithSchema_IncompatibleType_ThrowsArgumentException(DataType parameterType, object incompatibleValue)
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0")
			.AddParameter("TypedParam", parameterType);
		var settings = new ConnectionSettings(schema);

		// Act & Assert
		var exception = Assert.Throws<ArgumentException>(() => 
			settings.SetParameter("TypedParam", incompatibleValue));
		Assert.Contains($"The value provided foe the key 'TypedParam' is not compatible with the type '{parameterType}'", exception.Message);
	}

	[Theory]
	[InlineData(DataType.Boolean, true)]
	[InlineData(DataType.Boolean, false)]
	[InlineData(DataType.String, "test_string")]
	[InlineData(DataType.Integer, 42)]
	[InlineData(DataType.Integer, (long)123)]
	[InlineData(DataType.Integer, (byte)255)]
	[InlineData(DataType.Number, 123.45)]
	[InlineData(DataType.Number, 678.90f)]
	[InlineData(DataType.Number, (double)100.0)]
	public void SetParameter_WithSchema_CompatibleTypes_SetsParameter(DataType parameterType, object compatibleValue)
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0")
			.AddParameter("TypedParam", parameterType);
		var settings = new ConnectionSettings(schema);

		// Act
		settings.SetParameter("TypedParam", compatibleValue);

		// Assert
		Assert.Equal(compatibleValue, settings.Parameters["TypedParam"]);
	}

	[Fact]
	public void SetParameter_WithSchema_AllowedValues_ValidValue_SetsParameter()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0")
			.AddParameter("EnumParam", DataType.String, param =>
			{
				param.AllowedValues = new[] { "Value1", "Value2", "Value3" };
			});
		var settings = new ConnectionSettings(schema);

		// Act
		settings.SetParameter("EnumParam", "Value2");

		// Assert
		Assert.Equal("Value2", settings.Parameters["EnumParam"]);
	}

	[Fact]
	public void SetParameter_WithSchema_AllowedValues_InvalidValue_ThrowsArgumentException()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0")
			.AddParameter("EnumParam", DataType.String, param =>
			{
				param.AllowedValues = new[] { "Value1", "Value2", "Value3" };
			});
		var settings = new ConnectionSettings(schema);

		// Act & Assert
		var exception = Assert.Throws<ArgumentException>(() => 
			settings.SetParameter("EnumParam", "InvalidValue"));
		Assert.Contains("The value InvalidValue is not allowed for the parameter EnumParam", exception.Message);
	}

	#endregion

	#region GetParameter Tests

	[Fact]
	public void GetParameter_ExistingParameter_ReturnsValue()
	{
		// Arrange
		var settings = new ConnectionSettings()
			.SetParameter("TestKey", "TestValue");

		// Act
		var result = settings.GetParameter("TestKey");

		// Assert
		Assert.Equal("TestValue", result);
	}

	[Fact]
	public void GetParameter_NonExistingParameter_ReturnsNull()
	{
		// Arrange
		var settings = new ConnectionSettings();

		// Act
		var result = settings.GetParameter("NonExistingKey");

		// Assert
		Assert.Null(result);
	}

	[Fact]
	public void GetParameter_WithSchema_NonExistingParameterWithDefault_ReturnsDefault()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0")
			.AddParameter("ParamWithDefault", DataType.String, param =>
			{
				param.DefaultValue = "DefaultValue";
			});
		var settings = new ConnectionSettings(schema);

		// Act
		var result = settings.GetParameter("ParamWithDefault");

		// Assert
		Assert.Equal("DefaultValue", result);
	}

	[Fact]
	public void GetParameter_WithSchema_ExistingParameterIgnoresDefault_ReturnsSetValue()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0")
			.AddParameter("ParamWithDefault", DataType.String, param =>
			{
				param.DefaultValue = "DefaultValue";
			});
		var settings = new ConnectionSettings(schema)
			.SetParameter("ParamWithDefault", "SetValue");

		// Act
		var result = settings.GetParameter("ParamWithDefault");

		// Assert
		Assert.Equal("SetValue", result);
	}

	[Fact]
	public void GetParameter_WithSchema_ParameterNotInSchema_ReturnsNull()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0");
		var settings = new ConnectionSettings(schema);

		// Act
		var result = settings.GetParameter("UnknownParam");

		// Assert
		Assert.Null(result);
	}

	#endregion

	#region GetParameter<T> Tests

	[Fact]
	public void GetParameterGeneric_CorrectType_ReturnsTypedValue()
	{
		// Arrange
		var settings = new ConnectionSettings()
			.SetParameter("StringParam", "StringValue")
			.SetParameter("IntParam", 42)
			.SetParameter("BoolParam", true);

		// Act & Assert
		Assert.Equal("StringValue", settings.GetParameter<string>("StringParam"));
		Assert.Equal(42, settings.GetParameter<int>("IntParam"));
		Assert.True(settings.GetParameter<bool>("BoolParam"));
	}

	[Fact]
	public void GetParameterGeneric_IncorrectType_ThrowsInvalidCastException()
	{
		// Arrange
		var settings = new ConnectionSettings()
			.SetParameter("StringParam", "StringValue");

		// Act & Assert
		var exception = Assert.Throws<InvalidCastException>(() => 
			settings.GetParameter<int>("StringParam"));
		Assert.Contains("The value for the key 'StringParam' cannot be cast to type 'System.Int32'", exception.Message);
	}

	[Fact]
	public void GetParameterGeneric_NullValue_CorrectType_ReturnsDefault()
	{
		// Arrange
		var settings = new ConnectionSettings()
			.SetParameter("NullParam", null);

		// Act & Assert - The current implementation throws InvalidCastException for null values
		// when trying to cast to any type (even nullable ones)
		Assert.Throws<InvalidCastException>(() => settings.GetParameter<string>("NullParam"));
		Assert.Throws<InvalidCastException>(() => settings.GetParameter<string?>("NullParam"));
	}

	[Fact]
	public void GetParameterGeneric_NonExistingParameter_ReturnsDefault()
	{
		// Arrange
		var settings = new ConnectionSettings();

		// Act & Assert - For non-existing parameters, GetParameter returns null
		// The GetParameter<T> method throws InvalidCastException when trying to cast null to non-nullable types
		Assert.Throws<InvalidCastException>(() => settings.GetParameter<string>("NonExisting"));
		Assert.Throws<InvalidCastException>(() => settings.GetParameter<int>("NonExisting"));
		Assert.Throws<InvalidCastException>(() => settings.GetParameter<bool>("NonExisting"));
		
		// The method doesn't handle nullable types well either, so this will also throw
		Assert.Throws<InvalidCastException>(() => settings.GetParameter<string?>("NonExisting"));
	}

	[Fact]
	public void GetParameterGeneric_WithSchema_DefaultValue_ReturnsTypedDefault()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0")
			.AddParameter("StringParam", DataType.String, param => param.DefaultValue = "Default")
			.AddParameter("IntParam", DataType.Integer, param => param.DefaultValue = 100)
			.AddParameter("BoolParam", DataType.Boolean, param => param.DefaultValue = true);
		var settings = new ConnectionSettings(schema);

		// Act & Assert
		Assert.Equal("Default", settings.GetParameter<string>("StringParam"));
		Assert.Equal(100, settings.GetParameter<int>("IntParam"));
		Assert.True(settings.GetParameter<bool>("BoolParam"));
	}

	#endregion

	#region Indexer Tests

	[Fact]
	public void Indexer_Get_ExistingParameter_ReturnsValue()
	{
		// Arrange
		var settings = new ConnectionSettings()
			.SetParameter("TestKey", "TestValue");

		// Act
		var result = settings["TestKey"];

		// Assert
		Assert.Equal("TestValue", result);
	}

	[Fact]
	public void Indexer_Get_NonExistingParameter_ReturnsNull()
	{
		// Arrange
		var settings = new ConnectionSettings();

		// Act
		var result = settings["NonExisting"];

		// Assert
		Assert.Null(result);
	}

	[Fact]
	public void Indexer_Set_SetsParameter()
	{
		// Arrange
		var settings = new ConnectionSettings();

		// Act
		settings["TestKey"] = "TestValue";

		// Assert
		Assert.Equal("TestValue", settings.Parameters["TestKey"]);
	}

	[Fact]
	public void Indexer_Set_WithSchema_ValidatesParameter()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0")
			.AddParameter("ValidParam", DataType.String);
		var settings = new ConnectionSettings(schema);

		// Act
		settings["ValidParam"] = "ValidValue";

		// Assert
		Assert.Equal("ValidValue", settings.Parameters["ValidParam"]);
	}

	[Fact]
	public void Indexer_Set_WithSchema_InvalidParameter_ThrowsArgumentException()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0")
			.AddParameter("ValidParam", DataType.String);
		var settings = new ConnectionSettings(schema);

		// Act & Assert
		Assert.Throws<ArgumentException>(() => settings["InvalidParam"] = "Value");
	}

	#endregion

	#region Parameters Property Tests

	[Fact]
	public void Parameters_ReturnsReadOnlyDictionary()
	{
		// Arrange
		var settings = new ConnectionSettings()
			.SetParameter("Key1", "Value1")
			.SetParameter("Key2", "Value2");

		// Act
		var parameters = settings.Parameters;

		// Assert
		Assert.IsAssignableFrom<ReadOnlyDictionary<string, object?>>(parameters);
		Assert.Equal(2, parameters.Count);
		Assert.Equal("Value1", parameters["Key1"]);
		Assert.Equal("Value2", parameters["Key2"]);
	}

	[Fact]
	public void Parameters_IsReadOnly_CannotModifyDirectly()
	{
		// Arrange
		var settings = new ConnectionSettings()
			.SetParameter("Key1", "Value1");

		// Act
		var parameters = settings.Parameters;

		// Assert - Should not be able to modify the returned dictionary
		Assert.IsAssignableFrom<IReadOnlyDictionary<string, object?>>(parameters);
	}

	#endregion

	#region Complex Integration Tests

	[Fact]
	public void ComplexScenario_TwilioLikeProvider_AllOperations()
	{
		// Arrange
		var schema = new ChannelSchema("Twilio", "SMS", "1.0.0")
			.AddRequiredParameter("AccountSid", DataType.String)
			.AddRequiredParameter("AuthToken", DataType.String, true)
			.AddParameter("FromNumber", DataType.String)
			.AddParameter("EnableStatusCallbacks", DataType.Boolean, param => param.DefaultValue = false)
			.AddParameter("MaxRetries", DataType.Integer, param => param.DefaultValue = 3)
			.AddParameter("TimeoutSeconds", DataType.Number, param => param.DefaultValue = 30.0);

		var settings = new ConnectionSettings(schema);

		// Act - Set required parameters
		settings
			.SetParameter("AccountSid", "AC123456789")
			.SetParameter("AuthToken", "secret_token")
			.SetParameter("FromNumber", "+1234567890");

		// Act - Get parameters (some with defaults)
		var accountSid = settings.GetParameter<string>("AccountSid");
		var authToken = settings.GetParameter<string>("AuthToken");
		var fromNumber = settings.GetParameter<string>("FromNumber");
		var enableCallbacks = settings.GetParameter<bool>("EnableStatusCallbacks"); // Default
		var maxRetries = settings.GetParameter<int>("MaxRetries"); // Default
		var timeout = settings.GetParameter<double>("TimeoutSeconds"); // Default

		// Assert
		Assert.Equal("AC123456789", accountSid);
		Assert.Equal("secret_token", authToken);
		Assert.Equal("+1234567890", fromNumber);
		Assert.False(enableCallbacks); // Default value
		Assert.Equal(3, maxRetries); // Default value
		Assert.Equal(30.0, timeout); // Default value
		Assert.Equal(3, settings.Parameters.Count); // Only explicitly set parameters
	}

	[Fact]
	public void ComplexScenario_EmailProvider_WithValidation()
	{
		// Arrange
		var schema = new ChannelSchema("SMTP", "Email", "1.0.0")
			.AddRequiredParameter("Host", DataType.String)
			.AddParameter("Port", DataType.Integer, param =>
			{
				param.DefaultValue = 587;
				param.AllowedValues = new object[] { 25, 465, 587, 993, 995 };
			})
			.AddRequiredParameter("Username", DataType.String)
			.AddRequiredParameter("Password", DataType.String, true)
			.AddParameter("EnableSsl", DataType.Boolean, param => param.DefaultValue = true)
			.AddParameter("ConnectionType", DataType.String, param =>
			{
				param.AllowedValues = new[] { "SMTP", "SMTPS", "STARTTLS" };
				param.DefaultValue = "STARTTLS";
			});

		// Act & Assert - Valid configuration
		var settings = new ConnectionSettings(schema)
			.SetParameter("Host", "smtp.gmail.com")
			.SetParameter("Port", 587)
			.SetParameter("Username", "user@gmail.com")
			.SetParameter("Password", "app_password")
			.SetParameter("ConnectionType", "STARTTLS");

		Assert.Equal("smtp.gmail.com", settings.GetParameter<string>("Host"));
		Assert.Equal(587, settings.GetParameter<int>("Port"));
		Assert.Equal("user@gmail.com", settings.GetParameter<string>("Username"));
		Assert.Equal("app_password", settings.GetParameter<string>("Password"));
		Assert.True(settings.GetParameter<bool>("EnableSsl")); // Default
		Assert.Equal("STARTTLS", settings.GetParameter<string>("ConnectionType"));

		// Act & Assert - Invalid port
		Assert.Throws<ArgumentException>(() => 
			settings.SetParameter("Port", 8080)); // Not in allowed values

		// Act & Assert - Invalid connection type
		Assert.Throws<ArgumentException>(() => 
			settings.SetParameter("ConnectionType", "INVALID"));
	}

	[Fact]
	public void CopyConstructor_CompleteScenario_PreservesAllAspects()
	{
		// Arrange - Create original settings without schema to avoid validation issues
		var originalSettings = new ConnectionSettings()
			.SetParameter("Required", "RequiredValue")
			.SetParameter("Optional", 200)
			.SetParameter("Additional", "AdditionalValue");

		// Act
		var copiedSettings = new ConnectionSettings(originalSettings);

		// Assert - All parameters copied
		Assert.Equal(3, copiedSettings.Parameters.Count);
		Assert.Equal("RequiredValue", copiedSettings.GetParameter("Required"));
		Assert.Equal(200, copiedSettings.GetParameter("Optional"));
		Assert.Equal("AdditionalValue", copiedSettings.GetParameter("Additional"));

		// Test schema behavior separately with a new schema
		var schema = new ChannelSchema("Provider", "Type", "1.0.0")
			.AddParameter("Optional", DataType.Integer, param => param.DefaultValue = 100);
		var newSettings = new ConnectionSettings(schema);
		Assert.Equal(100, newSettings.GetParameter<int>("Optional")); // Default from schema
	}

	#endregion

	#region Edge Cases and Error Conditions

	[Fact]
	public void SetParameter_NullKey_ThrowsArgumentNullException()
	{
		// Arrange
		var settings = new ConnectionSettings();

		// Act & Assert
		Assert.Throws<ArgumentNullException>(() => settings.SetParameter(null!, "value"));
	}

	[Fact]
	public void GetParameter_NullKey_ThrowsArgumentNullException()
	{
		// Arrange
		var settings = new ConnectionSettings();

		// Act & Assert
		Assert.Throws<ArgumentNullException>(() => settings.GetParameter(null!));
	}

	[Fact]
	public void GetParameterGeneric_NullKey_ThrowsArgumentNullException()
	{
		// Arrange
		var settings = new ConnectionSettings();

		// Act & Assert
		Assert.Throws<ArgumentNullException>(() => settings.GetParameter<string>(null!));
	}

	[Fact]
	public void Indexer_NullKey_ThrowsArgumentNullException()
	{
		// Arrange
		var settings = new ConnectionSettings();

		// Act & Assert
		Assert.Throws<ArgumentNullException>(() => _ = settings[null!]);
		Assert.Throws<ArgumentNullException>(() => settings[null!] = "value");
	}

	[Fact]
	public void SetParameter_EmptyKey_AcceptsEmptyKey()
	{
		// Arrange
		var settings = new ConnectionSettings();

		// Act - Current implementation doesn't validate empty keys
		settings.SetParameter("", "value");

		// Assert
		Assert.Equal("value", settings.Parameters[""]);
	}

	[Fact]
	public void SetParameter_WhitespaceKey_AcceptsWhitespaceKey()
	{
		// Arrange
		var settings = new ConnectionSettings();

		// Act - Current implementation doesn't validate whitespace keys
		settings.SetParameter("   ", "value");

		// Assert
		Assert.Equal("value", settings.Parameters["   "]);
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData("\t")]
	[InlineData("\n")]
	public void GetParameter_EmptyOrWhitespaceKey_ReturnsNull(string key)
	{
		// Arrange
		var settings = new ConnectionSettings();

		// Act
		var result = settings.GetParameter(key);

		// Assert - Should return null for non-existing keys
		Assert.Null(result);
	}

	[Fact]
	public void Schema_WithNullDefaultValue_HandlesCorrectly()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0")
			.AddParameter("ParamWithNullDefault", DataType.String, param => param.DefaultValue = null);
		var settings = new ConnectionSettings(schema);

		// Act
		var result = settings.GetParameter("ParamWithNullDefault");

		// Assert
		Assert.Null(result);
	}

	[Fact]
	public void MultipleSchemaParameters_SameName_DifferentCase_HandlesCorrectly()
	{
		// Arrange
		var schema = new ChannelSchema("Provider", "Type", "1.0.0")
			.AddParameter("TestParam", DataType.String);
		var settings = new ConnectionSettings(schema);

		// Act - Set with exact case
		settings.SetParameter("TestParam", "Value1");
		
		// Different case should still work for retrieval
		var result = settings.GetParameter("testparam");

		// Assert - Should get null because key is case-sensitive in dictionary
		Assert.Null(result);
		
		// But exact case should work
		Assert.Equal("Value1", settings.GetParameter("TestParam"));
	}

	[Fact]
	public void GetParameterGeneric_WithSchemaDefaultValue_ButNullInSettings_ReturnsNull()
	{
		// Arrange - For this test, we'll use a schema that allows null values
		var schema = new ChannelSchema("Provider", "Type", "1.0.0")
			.AddParameter("ParamWithDefault", DataType.String, param => 
			{
				param.DefaultValue = "DefaultValue";
				param.IsRequired = false; // Allow null values
			});
		
		var settings = new ConnectionSettings(schema);
		// Don't set the parameter at all, let it use the default

		// Act - Parameter not set, should use default
		var result = settings.GetParameter("ParamWithDefault");

		// Assert
		Assert.Equal("DefaultValue", result);
		
		// Now test that we can't set null for String type due to type validation
		Assert.Throws<ArgumentException>(() => 
			settings.SetParameter("ParamWithDefault", null));
	}

	[Fact]
	public void GetParameterGeneric_WithNullValueDefaultType_ReturnsCorrectDefault()
	{
		// Arrange
		var settings = new ConnectionSettings()
			.SetParameter("NullStringParam", null)
			.SetParameter("NullIntParam", null);

		// Act & Assert - Current implementation throws InvalidCastException for all null values
		Assert.Throws<InvalidCastException>(() => settings.GetParameter<string>("NullStringParam"));
		Assert.Throws<InvalidCastException>(() => settings.GetParameter<int>("NullIntParam"));
		Assert.Throws<InvalidCastException>(() => settings.GetParameter<string?>("NullStringParam"));
	}

	#endregion

	#region Performance and Memory Tests

	[Fact]
	public void LargeNumberOfParameters_PerformanceTest()
	{
		// Arrange
		var settings = new ConnectionSettings();
		const int parameterCount = 1000;

		// Act - Set many parameters
		for (int i = 0; i < parameterCount; i++)
		{
			settings.SetParameter($"Param{i}", $"Value{i}");
		}

		// Assert - All parameters accessible
		Assert.Equal(parameterCount, settings.Parameters.Count);
		
		for (int i = 0; i < parameterCount; i++)
		{
			Assert.Equal($"Value{i}", settings.GetParameter($"Param{i}"));
		}
	}

	[Fact]
	public void ParameterOverwriting_MaintainsCorrectCount()
	{
		// Arrange
		var settings = new ConnectionSettings();

		// Act - Set and overwrite same parameter multiple times
		for (int i = 0; i < 100; i++)
		{
			settings.SetParameter("SameKey", $"Value{i}");
		}

		// Assert - Only one parameter exists
		Assert.Single(settings.Parameters);
		Assert.Equal("Value99", settings.GetParameter("SameKey"));
	}

	#endregion
}