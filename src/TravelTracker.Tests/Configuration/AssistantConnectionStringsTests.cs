using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

using TravelTracker.Services.Configuration;

namespace TravelTracker.Tests.Configuration;

public class AssistantConnectionStringsTests
{
    private const string PrimaryConnectionString = "Server=tcp:primary.database.windows.net;Database=travel;";
    private const string DefaultConnectionString = "Server=tcp:fallback.database.windows.net;Database=travel;";

    private static IConfiguration Build(params (string Key, string? Value)[] values)
    {
        var dictionary = new Dictionary<string, string?>();
        foreach (var (key, value) in values)
        {
            dictionary[key] = value;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(dictionary).Build();
    }

    [Fact]
    public void Resolve_NullConfiguration_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => AssistantConnectionStrings.Resolve(null!));
    }

    [Fact]
    public void Resolve_OnlySqlServerKey_ReturnsSqlServerValue()
    {
        var configuration = Build(("SqlServer:ConnectionString", PrimaryConnectionString));

        Assert.Equal(PrimaryConnectionString, AssistantConnectionStrings.Resolve(configuration));
    }

    [Fact]
    public void Resolve_OnlyDefaultConnection_ReturnsDefaultConnection()
    {
        var configuration = Build(("ConnectionStrings:DefaultConnection", DefaultConnectionString));

        Assert.Equal(DefaultConnectionString, AssistantConnectionStrings.Resolve(configuration));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_BlankSqlServerKeyWithDefaultConnection_ReturnsDefaultConnection(string primary)
    {
        var configuration = Build(
            ("SqlServer:ConnectionString", primary),
            ("ConnectionStrings:DefaultConnection", DefaultConnectionString));

        Assert.Equal(DefaultConnectionString, AssistantConnectionStrings.Resolve(configuration));
    }

    [Fact]
    public void Resolve_BothKeysSet_PrefersSqlServerKey()
    {
        var configuration = Build(
            ("SqlServer:ConnectionString", PrimaryConnectionString),
            ("ConnectionStrings:DefaultConnection", DefaultConnectionString));

        Assert.Equal(PrimaryConnectionString, AssistantConnectionStrings.Resolve(configuration));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("   ", "  ")]
    public void Resolve_BothBlank_ReturnsNull(string? primary, string? fallback)
    {
        var configuration = Build(
            ("SqlServer:ConnectionString", primary),
            ("ConnectionStrings:DefaultConnection", fallback));

        Assert.Null(AssistantConnectionStrings.Resolve(configuration));
    }

    [Fact]
    public void ValidateActionStorage_OnlyDefaultConnection_Succeeds()
    {
        var configuration = Build(("ConnectionStrings:DefaultConnection", DefaultConnectionString));

        Assert.Empty(TravelAssistantOptionsValidator.ValidateActionStorage(configuration));
    }

    [Fact]
    public void ValidateActionStorage_EmptySqlServerKeyWithDefaultConnection_Succeeds()
    {
        var configuration = Build(
            ("SqlServer:ConnectionString", string.Empty),
            ("ConnectionStrings:DefaultConnection", DefaultConnectionString));

        Assert.Empty(TravelAssistantOptionsValidator.ValidateActionStorage(configuration));
    }

    [Fact]
    public void ValidateActionStorage_BothBlank_ReportsKeyOnlyFailureMentioningBothKeys()
    {
        var configuration = Build(
            ("SqlServer:ConnectionString", "   "),
            ("ConnectionStrings:DefaultConnection", null));

        var failures = TravelAssistantOptionsValidator.ValidateActionStorage(configuration);

        var failure = Assert.Single(failures);
        Assert.Contains("SqlServer:ConnectionString", failure);
        Assert.Contains("ConnectionStrings:DefaultConnection", failure);
        Assert.DoesNotContain(PrimaryConnectionString, failure);
        Assert.DoesNotContain(DefaultConnectionString, failure);
    }
}
