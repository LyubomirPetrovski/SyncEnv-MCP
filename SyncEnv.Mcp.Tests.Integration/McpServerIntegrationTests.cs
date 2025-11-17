using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SyncEnvMcp.Models;
using SyncEnvMcp.Services;
using SyncEnvMcp.Tools;

namespace SyncEnv.Mcp.Tests.Integration;

/// <summary>
/// Integration tests for the MCP server and its tools
/// </summary>
public class McpServerIntegrationTests : IDisposable
{
    private readonly IHost _host;
    private readonly IServiceProvider _services;
    private readonly IDataService _dataService;
    private readonly ISampleDataGenerator _sampleDataGenerator;
    private readonly IDependencyMapper _dependencyMapper;

    public McpServerIntegrationTests()
    {
        // Create a test host similar to Program.cs but for testing
        var builder = Host.CreateApplicationBuilder();

        // Add configuration
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            {"SyncConfiguration:Environments:0:Name", "Production"},
            {"SyncConfiguration:Environments:0:Type", "InMemory"},
            {"SyncConfiguration:Environments:1:Name", "Local"},
            {"SyncConfiguration:Environments:1:Type", "InMemory"}
        });

        // Configure logging for tests - avoid EventLog to prevent permission issues  
        builder.Logging.ClearProviders().AddConsole().SetMinimumLevel(LogLevel.Error);

        // Register services
        builder.Services.AddSingleton<IDataService, InMemoryDataService>();
        builder.Services.AddSingleton<IDependencyMapper, DependencyMapper>();
        builder.Services.AddSingleton<ISampleDataGenerator, SampleDataGenerator>();
        builder.Services.AddSingleton<IDataInitializationHelperService, DataInitializationHelperService>();

        // Add MCP Server (without transport for testing)
        builder.Services.AddMcpServer().WithToolsFromAssembly();

        _host = builder.Build();
        _services = _host.Services;

        // Get required services
        _dataService = _services.GetRequiredService<IDataService>();
        _sampleDataGenerator = _services.GetRequiredService<ISampleDataGenerator>();
        _dependencyMapper = _services.GetRequiredService<IDependencyMapper>();

        // Initialize tools
        ToolInitializer.InitializeTools(_services);
    }

    [Fact]
    public async Task Server_Initialization_ShouldSucceed()
    {
        // Arrange & Act
        await _host.StartAsync();

        // Assert
        Assert.NotNull(_dataService);
        Assert.NotNull(_sampleDataGenerator);
        Assert.NotNull(_dependencyMapper);

        // Verify environments are available
        var environments = _dataService.GetAvailableEnvironments();
        Assert.Contains("Production", environments);
        Assert.Contains("Local", environments);

        await _host.StopAsync();
    }

    [Fact]
    public async Task DataInitialization_ShouldSetupSampleData()
    {
        // Arrange
        var dataInitService = _services.GetRequiredService<IDataInitializationHelperService>();

        // Act
        await dataInitService.InitializeAsync();

        // Assert - Verify sample data was created in Production environment
        var teams = await _dataService.GetAllAsync<Team>("teams", "Production");
        var competitions = await _dataService.GetAllAsync<Competition>("competitions", "Production");
        var games = await _dataService.GetAllAsync<Game>("games", "Production");

        Assert.NotEmpty(teams);
        Assert.NotEmpty(competitions);
        Assert.NotEmpty(games);
    }

    [Fact]
    public void EnvironmentTools_ListEnvironments_ShouldReturnValidEnvironments()
    {
        // Act
        var result = EnvironmentTools.ListEnvironments();

        // Assert
        Assert.Contains("Production", result);
        Assert.Contains("Local", result);
    }

    [Fact]
    public async Task EnvironmentTools_TestConnection_ShouldSucceedForValidEnvironments()
    {
        // Act
        var productionResult = await EnvironmentTools.TestConnection("Production");
        var localResult = await EnvironmentTools.TestConnection("Local");

        // Assert
        Assert.Contains("Successfully connected to Production", productionResult);
        Assert.Contains("Successfully connected to Local", localResult);
    }

    [Fact]
    public async Task EnvironmentTools_GetDatabaseStats_ShouldReturnStats()
    {
        // Act
        var result = await EnvironmentTools.GetDatabaseStats("Production");

        // Assert
        Assert.Contains("Database Stats for Production", result);
        Assert.Contains("Collections:", result);
        Assert.Contains("Documents:", result);
    }

    public void Dispose()
    {
        _host?.Dispose();
    }
}