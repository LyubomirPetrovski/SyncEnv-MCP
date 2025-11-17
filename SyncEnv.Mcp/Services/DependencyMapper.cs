using SyncEnvMcp.Models;
using Microsoft.Extensions.Logging;
using Synergy.Model;

namespace SyncEnvMcp.Services;

public interface IDependencyMapper
{
    /// <summary>
    /// Get all dependencies for a game (teams, competition, season, etc.)
    /// </summary>
    Task<DependencyGraph> GetGameDependenciesAsync(string gameId, string sourceEnvironment);
    
    /// <summary>
    /// Get dependencies for a team (players, competitions, etc.)
    /// </summary>
    Task<DependencyGraph> GetTeamDependenciesAsync(string teamId, string sourceEnvironment);
    
    /// <summary>
    /// Get dependencies for a competition (seasons, teams, etc.)
    /// </summary>
    Task<DependencyGraph> GetCompetitionDependenciesAsync(string competitionId, string sourceEnvironment);
}

public class DependencyMapper : IDependencyMapper
{
    private readonly IDataService _dataService;
    private readonly ILogger<DependencyMapper> _logger;
    
    public DependencyMapper(IDataService dataService, ILogger<DependencyMapper> logger)
    {
        _dataService = dataService;
        _logger = logger;
    }
    
    public async Task<DependencyGraph> GetGameDependenciesAsync(string gameId, string sourceEnvironment)
    {
        var graph = new DependencyGraph();
        
        // Get the game first
        var game = await _dataService.FindOneAsync<Game>("games", g => g.id == gameId, sourceEnvironment);
        
        if (game == null)
        {
            _logger.LogWarning("Game {GameId} not found in {Environment}", gameId, sourceEnvironment);
            return graph;
        }
        
        graph.AddEntity("games", game.id, game);
        
        // Add teams
        if (game.HomeTeam?.id != null)
        {
            graph.AddDependency("teams", game.HomeTeam.id);
        }
        
        if (game.AwayTeam?.id != null)
        {
            graph.AddDependency("teams", game.AwayTeam.id);
        }
        
        // Add competition
        if (game.Competition?.id != null)
        {
            graph.AddDependency("competitions", game.Competition.id);
        }
        
        // Add season
        if (game.Season?.id != null)
        {
            graph.AddDependency("seasons", game.Season.id);
        }
        
        // Add league
        if (game.League?.id != null)
        {
            graph.AddDependency("leagues", game.League.id);
        }
        
        _logger.LogInformation("Built dependency graph for game {GameId}: {DependencyCount} dependencies", 
            gameId, graph.GetAllDependencies().Count());
        
        return graph;
    }
    
    public async Task<DependencyGraph> GetTeamDependenciesAsync(string teamId, string sourceEnvironment)
    {
        var graph = new DependencyGraph();
        
        // Get the team
        var team = await _dataService.FindOneAsync<Team>("teams", t => t.id == teamId, sourceEnvironment);
        
        if (team == null)
        {
            _logger.LogWarning("Team {TeamId} not found in {Environment}", teamId, sourceEnvironment);
            return graph;
        }
        
        graph.AddEntity("teams", team.id, team);
        
        // Add players
        foreach (var playerRef in team.Players)
        {
            if (playerRef?.id != null)
            {
                graph.AddDependency("players", playerRef.id);
            }
        }
        
        // Add competitions
        foreach (var competitionRef in team.Competitions)
        {
            if (competitionRef?.id != null)
            {
                graph.AddDependency("competitions", competitionRef.id);
            }
        }
        
        return graph;
    }
    
    public async Task<DependencyGraph> GetCompetitionDependenciesAsync(string competitionId, string sourceEnvironment)
    {
        var graph = new DependencyGraph();
        
        // Get the competition
        var competition = await _dataService.FindOneAsync<Competition>("competitions", c => c.id == competitionId, sourceEnvironment);
        
        if (competition == null)
        {
            _logger.LogWarning("Competition {CompetitionId} not found in {Environment}", competitionId, sourceEnvironment);
            return graph;
        }
        
        graph.AddEntity("competitions", competition.id, competition);
        
        // Add seasons
        foreach (var seasonRef in competition.Seasons)
        {
            if (seasonRef?.id != null)
            {
                graph.AddDependency("seasons", seasonRef.id);
            }
        }
        
        // Add participating teams
        foreach (var teamRef in competition.ParticipatingTeams)
        {
            if (teamRef?.id != null)
            {
                graph.AddDependency("teams", teamRef.id);
            }
        }
        
        return graph;
    }
}

/// <summary>
/// Represents the dependency graph for sync operations
/// </summary>
public class DependencyGraph
{
    private readonly Dictionary<string, Dictionary<string, object>> _entities = new();
    private readonly Dictionary<string, HashSet<string>> _dependencies = new();
    
    public void AddEntity(string collection, string id, object entity)
    {
        if (!_entities.ContainsKey(collection))
        {
            _entities[collection] = new Dictionary<string, object>();
        }
        
        _entities[collection][id] = entity;
    }
    
    public void AddDependency(string collection, string id)
    {
        if (!_dependencies.ContainsKey(collection))
        {
            _dependencies[collection] = new HashSet<string>();
        }
        
        _dependencies[collection].Add(id);
    }
    
    public Dictionary<string, HashSet<string>> GetAllDependencies() => _dependencies;
    
    public Dictionary<string, Dictionary<string, object>> GetAllEntities() => _entities;
    
    public IEnumerable<string> GetCollections() => _dependencies.Keys.Union(_entities.Keys);
    
    public int GetTotalEntityCount() => _entities.Values.Sum(dict => dict.Count);
    
    public int GetTotalDependencyCount() => _dependencies.Values.Sum(set => set.Count);
}