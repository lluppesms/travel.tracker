using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using TravelTracker.Data.Configuration;
using TravelTracker.Services.Configuration;

namespace TravelTracker.Tests.Configuration;

public class TravelAssistantOptionsValidatorTests
{
    private const string SecretConnectionString = "Server=tcp:secret.database.windows.net;Password=SuperSecretPassword123;";
    private const string SecretApiKey = "sk-super-secret-key-value";

    private static TravelAssistantOptions CreateAgentFrameworkOptions() => new()
    {
        Provider = ChatProvider.AgentFramework,
        WriteMode = AssistantWriteMode.Confirm,
        TimeZoneId = "America/Chicago"
    };

    private static TravelAssistantOptions CreateCopilotOptions() => new()
    {
        Provider = ChatProvider.CopilotSDK,
        WriteMode = AssistantWriteMode.Confirm,
        TimeZoneId = "America/Chicago",
        ModelDeploymentName = "gpt-4o",
        FoundryEndpoint = "https://example-foundry.services.ai.azure.com",
        TokenScope = "https://ai.azure.com/.default",
        CopilotHome = "/tmp/traveltracker-copilot"
    };

    private static IConfiguration CreateValidConfiguration(IDictionary<string, string?>? overrides = null)
    {
        var values = new Dictionary<string, string?>
        {
            ["AzureAd:TenantId"] = "11111111-1111-1111-1111-111111111111",
            ["AzureAd:ClientId"] = "22222222-2222-2222-2222-222222222222",
            ["SqlServer:ConnectionString"] = SecretConnectionString,
            ["AzureAIFoundry:ApiKey"] = SecretApiKey
        };

        if (overrides is not null)
        {
            foreach (var pair in overrides)
            {
                values[pair.Key] = pair.Value;
            }
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    [Fact]
    public void Validate_ValidConfirmCopilotConfiguration_Succeeds()
    {
        var validator = new TravelAssistantOptionsValidator(CreateValidConfiguration());

        var result = validator.Validate(null, CreateCopilotOptions());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_AgentFrameworkWithoutCopilotSettings_Succeeds()
    {
        var validator = new TravelAssistantOptionsValidator(CreateValidConfiguration());

        var result = validator.Validate(null, CreateAgentFrameworkOptions());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_AutoExecuteWriteMode_Fails()
    {
        var validator = new TravelAssistantOptionsValidator(CreateValidConfiguration());
        var options = CreateAgentFrameworkOptions();
        options.WriteMode = AssistantWriteMode.AutoExecute;

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, f => f.Contains("AutoExecute"));
    }

    [Fact]
    public void Validate_UnknownProvider_Fails()
    {
        var validator = new TravelAssistantOptionsValidator(CreateValidConfiguration());
        var options = CreateAgentFrameworkOptions();
        options.Provider = (ChatProvider)99;

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, f => f.Contains("Provider"));
    }

    [Theory]
    [InlineData("Not/A/Zone")]
    [InlineData("")]
    public void Validate_InvalidTimeZone_Fails(string timeZoneId)
    {
        var validator = new TravelAssistantOptionsValidator(CreateValidConfiguration());
        var options = CreateAgentFrameworkOptions();
        options.TimeZoneId = timeZoneId;

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, f => f.Contains("TimeZoneId"));
    }

    [Fact]
    public void Validate_CopilotWithMissingSettings_FailsForEachRequiredValue()
    {
        var validator = new TravelAssistantOptionsValidator(CreateValidConfiguration());
        var options = CreateCopilotOptions();
        options.ModelDeploymentName = string.Empty;
        options.FoundryEndpoint = string.Empty;
        options.TokenScope = string.Empty;
        options.CopilotHome = string.Empty;

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, f => f.Contains("ModelDeploymentName"));
        Assert.Contains(result.Failures!, f => f.Contains("FoundryEndpoint"));
        Assert.Contains(result.Failures!, f => f.Contains("TokenScope"));
        Assert.Contains(result.Failures!, f => f.Contains("CopilotHome"));
    }

    [Fact]
    public void Validate_NonPositiveLimits_Fail()
    {
        var validator = new TravelAssistantOptionsValidator(CreateValidConfiguration());
        var options = CreateAgentFrameworkOptions();
        options.MaxPromptCharacters = 0;
        options.TurnTimeoutSeconds = -1;
        options.MaxSessionsPerUser = 0;

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, f => f.Contains("MaxPromptCharacters"));
        Assert.Contains(result.Failures!, f => f.Contains("TurnTimeoutSeconds"));
    }

    [Fact]
    public void Validate_MissingAuthentication_Fails()
    {
        var configuration = CreateValidConfiguration(new Dictionary<string, string?>
        {
            ["AzureAd:TenantId"] = null,
            ["AzureAd:ClientId"] = null
        });
        var validator = new TravelAssistantOptionsValidator(configuration);

        var result = validator.Validate(null, CreateAgentFrameworkOptions());

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, f => f.Contains("AzureAd:TenantId"));
        Assert.Contains(result.Failures!, f => f.Contains("AzureAd:ClientId"));
    }

    [Fact]
    public void Validate_MissingSqlActionStorage_Fails()
    {
        var configuration = CreateValidConfiguration(new Dictionary<string, string?>
        {
            ["SqlServer:ConnectionString"] = null
        });
        var validator = new TravelAssistantOptionsValidator(configuration);

        var result = validator.Validate(null, CreateAgentFrameworkOptions());

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, f => f.Contains("SqlServer:ConnectionString"));
    }

    [Fact]
    public void Validate_FailureMessages_DoNotContainSecrets()
    {
        var configuration = CreateValidConfiguration();
        var validator = new TravelAssistantOptionsValidator(configuration);
        var options = CreateCopilotOptions();
        options.WriteMode = AssistantWriteMode.AutoExecute;
        options.TimeZoneId = "Not/A/Zone";
        options.ModelDeploymentName = string.Empty;

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
        foreach (var failure in result.Failures!)
        {
            Assert.DoesNotContain(SecretConnectionString, failure);
            Assert.DoesNotContain(SecretApiKey, failure);
            Assert.DoesNotContain("Password", failure);
        }
    }

    [Fact]
    public void AddTravelAssistantOptions_InvalidConfiguration_ThrowsOnValidation()
    {
        var configuration = CreateValidConfiguration(new Dictionary<string, string?>
        {
            ["TravelAssistant:Provider"] = "CopilotSDK",
            ["TravelAssistant:WriteMode"] = "AutoExecute"
        });

        var services = new ServiceCollection();
        services.AddTravelAssistantOptions(configuration);
        using var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<TravelAssistantOptions>>().Value);
        Assert.Contains(exception.Failures, f => f.Contains("AutoExecute"));
    }

    [Fact]
    public void AddTravelAssistantOptions_ValidConfiguration_BindsValues()
    {
        var configuration = CreateValidConfiguration(new Dictionary<string, string?>
        {
            ["TravelAssistant:Provider"] = "CopilotSDK",
            ["TravelAssistant:ModelDeploymentName"] = "gpt-4o",
            ["TravelAssistant:FoundryEndpoint"] = "https://example-foundry.services.ai.azure.com",
            ["TravelAssistant:TokenScope"] = "https://ai.azure.com/.default",
            ["TravelAssistant:CopilotHome"] = "/tmp/traveltracker-copilot"
        });

        var services = new ServiceCollection();
        services.AddTravelAssistantOptions(configuration);
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<TravelAssistantOptions>>().Value;

        Assert.Equal(ChatProvider.CopilotSDK, options.Provider);
        Assert.Equal(AssistantWriteMode.Confirm, options.WriteMode);
        Assert.Equal(60, options.TurnTimeoutSeconds);
        Assert.Equal(15, options.SessionIdleTimeoutMinutes);
        Assert.Equal(3, options.MaxSessionsPerUser);
        Assert.Equal(100, options.MaxSessionsPerInstance);
    }
}
