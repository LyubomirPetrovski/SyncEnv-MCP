using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SyncEnvMcp.Models;
using SyncEnvMcp.Services;
using SyncEnvMcp.Tools;

namespace SyncEnv.Mcp.Tests.Integration;

/// <summary>
/// Integration tests specifically for GameSyncTools functionality
/// </summary>
public class GameSyncToolsIntegrationTests : IDisposable
{
    private readonly IHost _host;
    private readonly IServiceProvider _services;
    private readonly IDataService _dataService;
    private readonly ISampleDataGenerator _sampleDataGenerator;

    public GameSyncToolsIntegrationTests()
    {
        // Create a test host
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

        _host = builder.Build();
        _services = _host.Services;

        _dataService = _services.GetRequiredService<IDataService>();
        _sampleDataGenerator = _services.GetRequiredService<ISampleDataGenerator>();

        // Initialize tools
        ToolInitializer.InitializeTools(_services);
    }

    [Fact]
    public async Task FindGames_WithTeamName_ShouldReturnMatchingGames()
    {
        // Arrange - Setup test data with known teams
        await SetupTestData();

        // Act
        var result = await GameSyncTools.FindGames("Arsenal", environment: "Production");

        // Assert
        Assert.Contains("Found", result);
        Assert.Contains("Arsenal", result);
        Assert.DoesNotContain("No games found", result);
    }

    [Fact]
    public async Task FindGames_WithNonExistentTeam_ShouldReturnNoResults()
    {
        // Arrange
        await SetupTestData();

        // Act
        var result = await GameSyncTools.FindGames("NonExistentTeam", environment: "Production");

        // Assert
        Assert.Contains("No games found", result);
        Assert.Contains("NonExistentTeam", result);
    }

    [Fact]
    public async Task FindGames_WithDateRange_ShouldFilterResults()
    {
        // Arrange
        await SetupTestData();

        // Get a known date from test data
        var games = await _dataService.GetAllAsync<Game>("games", "Production");
        var testDate = games.First().Date;

        // Act
        var result = await GameSyncTools.FindGames(
            teamName: "Arsenal",
            startDate: testDate.ToString("yyyy-MM-dd"),
            endDate: testDate.AddDays(1).ToString("yyyy-MM-dd"),
            environment: "Production");

        // Assert
        Assert.Contains("Found", result);
        Assert.Contains(testDate.ToString("yyyy-MM-dd"), result);
    }

    [Fact]
    public async Task PreviewGameSync_WithValidGame_ShouldReturnPreview()
    {
        // Arrange
        await SetupTestData();

        // Get a real game ID
        var games = await _dataService.GetAllAsync<Game>("games", "Production");
        var testGameId = games.First().id;

        // Act
        var result = await GameSyncTools.PreviewGameSync(testGameId, "Production");

        // Assert
        Assert.Contains("Sync Preview for Game", result);
        Assert.Contains(testGameId, result);
        Assert.Contains("Total:", result);
        Assert.Contains("This is a preview only", result);
    }

    [Fact]
    public async Task PreviewGameSync_WithNonExistentGame_ShouldReturnNotFound()
    {
        // Arrange
        await SetupTestData();

        // Act
        var result = await GameSyncTools.PreviewGameSync("non-existent-game-id", "Production");

        // Assert
        Assert.Contains("not found", result);
        Assert.Contains("non-existent-game-id", result);
    }

    [Fact]
    public async Task SyncGame_ValidGameFromProductionToLocal_ShouldSyncSuccessfully()
    {
        // Arrange
        await SetupTestData();

        // Ensure Local environment is clear
        await _dataService.ClearEnvironmentAsync("Local");

        // Get a game to sync
        var games = await _dataService.GetAllAsync<Game>("games", "Production");
        var testGameId = games.First().id;

        // Act
        var result = await GameSyncTools.SyncGame(testGameId, "Production", "Local");

        // Assert
        Assert.Contains("Game sync completed", result);
        Assert.Contains(testGameId, result);
        Assert.Contains("documents synced", result);

        // Verify the game was actually synced
        var syncedGame = await _dataService.FindOneAsync<Game>("games", g => g.id == testGameId, "Local");
        Assert.NotNull(syncedGame);
        Assert.NotNull(syncedGame.SyncInfo);
        Assert.Equal("Production", syncedGame.SyncInfo.SourceEnvironment);
        Assert.Equal("MCP-Server", syncedGame.SyncInfo.SyncedBy);
    }

    [Fact]
    public async Task SyncGame_WithDependencies_ShouldSyncAllRelatedData()
    {
        // Arrange
        await SetupTestData();
        await _dataService.ClearEnvironmentAsync("Local");

        var games = await _dataService.GetAllAsync<Game>("games", "Production");
        var testGameId = games.First().id;
        var testGame = games.First();

        // Act
        var result = await GameSyncTools.SyncGame(testGameId, "Production", "Local");

        // Assert
        Assert.Contains("Game sync completed", result);

        // Verify related teams were synced
        if (testGame.HomeTeam?.id != null)
        {
            var homeTeam = await _dataService.FindOneAsync<Team>("teams", t => t.id == testGame.HomeTeam.id, "Local");
            Assert.NotNull(homeTeam);
        }

        if (testGame.AwayTeam?.id != null)
        {
            var awayTeam = await _dataService.FindOneAsync<Team>("teams", t => t.id == testGame.AwayTeam.id, "Local");
            Assert.NotNull(awayTeam);
        }
    }

    [Fact]
    public async Task SyncGame_NonExistentGame_ShouldReturnError()
    {
        // Arrange
        await SetupTestData();

        // Act
        var result = await GameSyncTools.SyncGame("non-existent-game", "Production", "Local");

        // Assert
        Assert.Contains("not found", result);
        Assert.Contains("non-existent-game", result);
    }

    [Fact]
    public async Task GameSync_EndToEndWorkflow_ShouldWork()
    {
        // Arrange - This tests the complete workflow
        await SetupTestData();
        await _dataService.ClearEnvironmentAsync("Local");

        var games = await _dataService.GetAllAsync<Game>("games", "Production");
        var testGameId = games.First().id;

        // Step 1: Find games
        var findResult = await GameSyncTools.FindGames("Arsenal", environment: "Production");
        Assert.Contains("Found", findResult);

        // Step 2: Preview sync
        var previewResult = await GameSyncTools.PreviewGameSync(testGameId, "Production");
        Assert.Contains("Sync Preview", previewResult);

        // Step 3: Perform actual sync
        var syncResult = await GameSyncTools.SyncGame(testGameId, "Production", "Local");
        Assert.Contains("Game sync completed", syncResult);

        // Step 4: Verify sync worked
        var syncedGame = await _dataService.FindOneAsync<Game>("games", g => g.id == testGameId, "Local");
        Assert.NotNull(syncedGame);
        Assert.Equal("Production", syncedGame.SyncInfo.SourceEnvironment);

        // Step 5: Verify we can find the synced game in Local
        var findInLocalResult = await GameSyncTools.FindGames("Arsenal", environment: "Local");
        Assert.Contains("Found", findInLocalResult);
    }

    private async Task SetupTestData()
    {
        // Clear and generate fresh test data
        await _dataService.ClearEnvironmentAsync("Production");

        var teams = _sampleDataGenerator.GenerateSampleTeams();
        var competitions = _sampleDataGenerator.GenerateSampleCompetitions();
        var seasons = _sampleDataGenerator.GenerateSampleSeasons();
        var games = _sampleDataGenerator.GenerateSampleGames(teams, competitions, seasons);
        var players = _sampleDataGenerator.GenerateSamplePlayers(teams);

        await _dataService.InsertManyAsync("teams", teams, "Production");
        await _dataService.InsertManyAsync("competitions", competitions, "Production");
        await _dataService.InsertManyAsync("seasons", seasons, "Production");
        await _dataService.InsertManyAsync("games", games, "Production");
        await _dataService.InsertManyAsync("players", players, "Production");
    }

    public void Dispose()
    {
        _host?.Dispose();
    }
}