using Moq;
using SyncEnvMcp.Models;
using SyncEnvMcp.Services;
using SyncEnvMcp.Tools;

namespace SyncEnvMcp.Tests.Unit;

public class GameSyncToolsTests
{
    private readonly Mock<IDependencyMapper> _mockDependencyMapper;

    public GameSyncToolsTests()
    {
        _mockDependencyMapper = new Mock<IDependencyMapper>();
    }

    [Fact]
    public async Task PreviewGameSync_ValidGameId_ReturnsExpectedPreview()
    {
        // Arrange
        var gameId = "test-game-id";
        var sourceEnv = "Staging";
        var mockGraph = CreateMockDependencyGraph();

        _mockDependencyMapper
            .Setup(x => x.GetGameDependenciesAsync(gameId, sourceEnv))
            .ReturnsAsync(mockGraph);

        // Act
        var result = await GameSyncTools.PreviewGameSync(gameId, sourceEnv);

        // Assert
        Assert.Contains("Sync Preview for Game", result);
        Assert.Contains("teams: 2 documents", result);
        Assert.Contains("games: 1 documents", result);
        Assert.Contains("This is a preview only", result);
    }

    [Fact]
    public async Task PreviewGameSync_GameNotFound_ReturnsNotFoundMessage()
    {
        // Arrange
        var gameId = "non-existent-game";
        var emptyGraph = new DependencyGraph();

        _mockDependencyMapper
            .Setup(x => x.GetGameDependenciesAsync(gameId, "Staging"))
            .ReturnsAsync(emptyGraph);

        // Act
        var result = await GameSyncTools.PreviewGameSync(gameId);

        // Assert
        Assert.Contains("Game non-existent-game not found", result);
    }

    [Fact]
    public async Task FindGames_ValidTeamName_ReturnsGamesList()
    {
        // Arrange
        var teamName = "Manchester";
        var mockGames = CreateMockGames();

        // Act
        var result = await GameSyncTools.FindGames(teamName);

        // Assert
        Assert.Contains("Found 2 games for 'Manchester'", result);
        Assert.Contains("Manchester United vs Liverpool", result);
    }

    private DependencyGraph CreateMockDependencyGraph()
    {
        var graph = new DependencyGraph();
        graph.AddEntity("games", "game1", new Game());
        graph.AddDependency("teams", "team1");
        graph.AddDependency("teams", "team2");
        return graph;
    }

    private List<Game> CreateMockGames()
    {
        return new List<Game>
        {
            new Game
            {
                id = "game1",
                Date = DateTime.Today,
                HomeTeam = new TeamRef { Name = "Manchester United" },
                AwayTeam = new TeamRef { Name = "Liverpool" },
                Competition = new BasicMonikerRef { Name = "Premier League" },
                HomeScore = 2,
                AwayScore = 1
            },
            new Game
            {
                id = "game2",
                Date = DateTime.Today.AddDays(-7),
                HomeTeam = new TeamRef { Name = "Manchester City" },
                AwayTeam = new TeamRef { Name = "Chelsea" },
                Competition = new BasicMonikerRef { Name = "Premier League" }
            }
        };
    }
}