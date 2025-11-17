using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using SyncEnvMcp.Models;
using SyncEnvMcp.Services;
using System.ComponentModel;

namespace SyncEnvMcp.Tools;

[McpServerToolType]
public sealed class GameSyncTools
{
    private static IDataService? _dataService;
    private static IDependencyMapper? _dependencyMapper;
    private static ILogger? _logger;

    public static void Initialize(IDataService dataService, IDependencyMapper dependencyMapper, ILogger logger)
    {
        _dataService = dataService;
        _dependencyMapper = dependencyMapper;
        _logger = logger;
    }

    [McpServerTool, Description("Preview what data would be synced for a specific game (dry-run mode).")]
    public static async Task<string> PreviewGameSync(
        [Description("Game ID to preview")] string gameId,
        [Description("Source environment (Production)")] string sourceEnvironment = "Production")
    {
        if (_dataService == null || _dependencyMapper == null)
            return "Error: Services not initialized";

        try
        {
            var dependencyGraph = await _dependencyMapper.GetGameDependenciesAsync(gameId, sourceEnvironment);

            if (dependencyGraph.GetTotalEntityCount() == 0)
            {
                return $"❌ Game {gameId} not found in {sourceEnvironment}";
            }

            var preview = $"""
                🎯 Sync Preview for Game {gameId} from {sourceEnvironment}:
                
                📋 Entities to be synced:
                """;

            foreach (var collection in dependencyGraph.GetCollections())
            {
                var dependencies = dependencyGraph.GetAllDependencies();
                var entities = dependencyGraph.GetAllEntities();

                var depCount = dependencies.ContainsKey(collection) ? dependencies[collection].Count : 0;
                var entityCount = entities.ContainsKey(collection) ? entities[collection].Count : 0;
                var totalCount = depCount + entityCount;

                if (totalCount > 0)
                {
                    preview += $"\n  • {collection}: {totalCount} documents";
                }
            }

            preview += $"\n\n📊 Total: {dependencyGraph.GetTotalDependencyCount() + dependencyGraph.GetTotalEntityCount()} documents";
            preview += "\n\n⚠️  This is a preview only. Use SyncGame to perform actual sync.";

            return preview;
        }
        catch (Exception ex)
        {
            return $"❌ Error previewing game sync: {ex.Message}";
        }
    }

    [McpServerTool, Description("Sync a specific game and all its dependencies from source to local environment.")]
    public static async Task<string> SyncGame(
        [Description("Game ID to sync")] string gameId,
        [Description("Source environment (Production)")] string sourceEnvironment = "Production",
        [Description("Target environment (usually Local)")] string targetEnvironment = "Local")
    {
        if (_dataService == null || _dependencyMapper == null || _logger == null)
            return "Error: Services not initialized";

        try
        {
            _logger.LogInformation("Starting sync for game {GameId} from {Source} to {Target}",
                gameId, sourceEnvironment, targetEnvironment);

            // Get dependency graph
            var dependencyGraph = await _dependencyMapper.GetGameDependenciesAsync(gameId, sourceEnvironment);

            if (dependencyGraph.GetTotalEntityCount() == 0)
            {
                return $"❌ Game {gameId} not found in {sourceEnvironment}";
            }

            var syncResults = new List<string>();
            var totalSynced = 0;

            // Sync dependencies first, then the game itself
            var collections = dependencyGraph.GetCollections().Where(c => c != "games").ToList();
            collections.Add("games"); // Add games last

            foreach (var collection in collections)
            {
                var result = await SyncCollection(collection, dependencyGraph, sourceEnvironment, targetEnvironment);
                if (result.Count > 0)
                {
                    syncResults.Add($"  • {collection}: {result.Count} documents");
                    totalSynced += result.Count;
                }
            }

            var summary = $"""
                ✅ Game sync completed for {gameId}
                
                📋 Synced from {sourceEnvironment} to {targetEnvironment}:
                {string.Join("\n", syncResults)}
                
                📊 Total: {totalSynced} documents synced
                """;

            _logger.LogInformation("Completed sync for game {GameId}: {Total} documents", gameId, totalSynced);

            return summary;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error syncing game {GameId}", gameId);
            return $"❌ Error syncing game: {ex.Message}";
        }
    }

    [McpServerTool, Description("Find games by team name and date range.")]
    public static async Task<string> FindGames(
        [Description("Team name (partial match)")] string teamName,
        [Description("Start date (YYYY-MM-DD)")] string? startDate = null,
        [Description("End date (YYYY-MM-DD)")] string? endDate = null,
        [Description("Source environment")] string environment = "Production")
    {
        if (_dataService == null)
            return "Error: Service not initialized";

        try
        {
            // Use text search for team names
            var games = await _dataService.FindByTextAsync<Game>("games", teamName, environment);

            // Apply date filters if provided
            if (DateTime.TryParse(startDate, out var start))
            {
                games = games.Where(g => g.Date >= start).ToList();
            }

            if (DateTime.TryParse(endDate, out var end))
            {
                games = games.Where(g => g.Date <= end).ToList();
            }

            // Limit and sort results
            games = games.OrderByDescending(g => g.Date).Take(10).ToList();

            if (!games.Any())
            {
                return $"❌ No games found for team '{teamName}' in {environment}";
            }

            var result = $"🔍 Found {games.Count} games for '{teamName}' in {environment}:\n";

            foreach (var game in games)
            {
                result += $"\n📅 {game.Date:yyyy-MM-dd} - {game.HomeTeam.Name} vs {game.AwayTeam.Name}";
                result += $"\n   ID: {game.id} | Competition: {game.Competition?.Name}";
                if (game.HomeScore.HasValue && game.AwayScore.HasValue)
                {
                    result += $" | Score: {game.HomeScore}-{game.AwayScore}";
                }
                result += "\n";
            }

            return result;
        }
        catch (Exception ex)
        {
            return $"❌ Error finding games: {ex.Message}";
        }
    }

    private static async Task<List<string>> SyncCollection(
        string collection,
        DependencyGraph dependencyGraph,
        string sourceEnvironment,
        string targetEnvironment)
    {
        var syncedIds = new List<string>();

        try
        {
            var dependencies = dependencyGraph.GetAllDependencies();
            var entities = dependencyGraph.GetAllEntities();

            // Get IDs to sync for this collection
            var idsToSync = new HashSet<string>();

            if (dependencies.ContainsKey(collection))
            {
                foreach (var id in dependencies[collection])
                {
                    idsToSync.Add(id);
                }
            }

            if (entities.ContainsKey(collection))
            {
                foreach (var id in entities[collection].Keys)
                {
                    idsToSync.Add(id);
                }
            }

            if (!idsToSync.Any()) return syncedIds;

            // Perform the actual sync based on collection type
            switch (collection)
            {
                case "games":
                    syncedIds.AddRange(await SyncGames(idsToSync, sourceEnvironment, targetEnvironment));
                    break;
                case "teams":
                    syncedIds.AddRange(await SyncTeams(idsToSync, sourceEnvironment, targetEnvironment));
                    break;
                case "competitions":
                    syncedIds.AddRange(await SyncCompetitions(idsToSync, sourceEnvironment, targetEnvironment));
                    break;
                case "seasons":
                    syncedIds.AddRange(await SyncSeasons(idsToSync, sourceEnvironment, targetEnvironment));
                    break;
                case "players":
                    syncedIds.AddRange(await SyncPlayers(idsToSync, sourceEnvironment, targetEnvironment));
                    break;
                default:
                    _logger?.LogInformation("Sync for collection {Collection} not yet implemented", collection);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error syncing collection {Collection}", collection);
        }

        return syncedIds;
    }

    private static async Task<List<string>> SyncGames(IEnumerable<string> gameIds, string sourceEnv, string targetEnv)
    {
        var syncedIds = new List<string>();

        foreach (var gameId in gameIds)
        {
            var game = await _dataService!.FindOneAsync<Game>("games", g => g.id == gameId, sourceEnv);
            if (game != null)
            {
                // Add sync metadata
                game.SyncInfo = new SyncMetadata
                {
                    LastSynced = DateTime.UtcNow,
                    SourceEnvironment = sourceEnv,
                    SyncedBy = "MCP-Server"
                };

                await _dataService.ReplaceOneAsync("games", g => g.id == gameId, game, targetEnv);
                syncedIds.Add(gameId);
            }
        }

        return syncedIds;
    }

    private static async Task<List<string>> SyncTeams(IEnumerable<string> teamIds, string sourceEnv, string targetEnv)
    {
        var syncedIds = new List<string>();

        foreach (var teamId in teamIds)
        {
            var team = await _dataService!.FindOneAsync<Team>("teams", t => t.id == teamId, sourceEnv);
            if (team != null)
            {
                // Add sync metadata
                team.SyncInfo = new SyncMetadata
                {
                    LastSynced = DateTime.UtcNow,
                    SourceEnvironment = sourceEnv,
                    SyncedBy = "MCP-Server"
                };

                await _dataService.ReplaceOneAsync("teams", t => t.id == teamId, team, targetEnv);
                syncedIds.Add(teamId);
            }
        }

        return syncedIds;
    }

    private static async Task<List<string>> SyncCompetitions(IEnumerable<string> competitionIds, string sourceEnv, string targetEnv)
    {
        var syncedIds = new List<string>();

        foreach (var competitionId in competitionIds)
        {
            var competition = await _dataService!.FindOneAsync<Competition>("competitions", c => c.id == competitionId, sourceEnv);
            if (competition != null)
            {
                competition.SyncInfo = new SyncMetadata
                {
                    LastSynced = DateTime.UtcNow,
                    SourceEnvironment = sourceEnv,
                    SyncedBy = "MCP-Server"
                };

                await _dataService.ReplaceOneAsync("competitions", c => c.id == competitionId, competition, targetEnv);
                syncedIds.Add(competitionId);
            }
        }

        return syncedIds;
    }

    private static async Task<List<string>> SyncSeasons(IEnumerable<string> seasonIds, string sourceEnv, string targetEnv)
    {
        var syncedIds = new List<string>();

        foreach (var seasonId in seasonIds)
        {
            var season = await _dataService!.FindOneAsync<Season>("seasons", s => s.id == seasonId, sourceEnv);
            if (season != null)
            {
                season.SyncInfo = new SyncMetadata
                {
                    LastSynced = DateTime.UtcNow,
                    SourceEnvironment = sourceEnv,
                    SyncedBy = "MCP-Server"
                };

                await _dataService.ReplaceOneAsync("seasons", s => s.id == seasonId, season, targetEnv);
                syncedIds.Add(seasonId);
            }
        }

        return syncedIds;
    }

    private static async Task<List<string>> SyncPlayers(IEnumerable<string> playerIds, string sourceEnv, string targetEnv)
    {
        var syncedIds = new List<string>();

        foreach (var playerId in playerIds)
        {
            var player = await _dataService!.FindOneAsync<Player>("players", p => p.id == playerId, sourceEnv);
            if (player != null)
            {
                player.SyncInfo = new SyncMetadata
                {
                    LastSynced = DateTime.UtcNow,
                    SourceEnvironment = sourceEnv,
                    SyncedBy = "MCP-Server"
                };

                await _dataService.ReplaceOneAsync("players", p => p.id == playerId, player, targetEnv);
                syncedIds.Add(playerId);
            }
        }

        return syncedIds;
    }
}