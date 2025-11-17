using Microsoft.Extensions.Logging;
using SyncEnvMcp.Models;
using Synergy.Model;

namespace SyncEnvMcp.Services;

/// <summary>
/// Service to initialize in-memory data with sample data for Production and Local environments
/// </summary>
public interface IDataInitializationHelperService
{
    /// <summary>
    /// Initialize both Production and Local environments with sample data
    /// </summary>
    Task InitializeAsync();
    
    /// <summary>
    /// Initialize specific environment with sample data
    /// </summary>
    Task InitializeEnvironmentAsync(string environment);
    
    /// <summary>
    /// Check if environments are initialized
    /// </summary>
    Task<bool> IsInitializedAsync(string environment);
}

public class DataInitializationHelperService : IDataInitializationHelperService
{
    private readonly IDataService _dataService;
    private readonly ISampleDataGenerator _sampleDataGenerator;
    private readonly ILogger<DataInitializationHelperService> _logger;
    
    public DataInitializationHelperService(
        IDataService dataService, 
        ISampleDataGenerator sampleDataGenerator, 
        ILogger<DataInitializationHelperService> logger)
    {
        _dataService = dataService;
        _sampleDataGenerator = sampleDataGenerator;
        _logger = logger;
    }
    
    public async Task InitializeAsync()
    {
        _logger.LogInformation("Initializing in-memory data for all environments");
        
        await InitializeEnvironmentAsync("Production");
        await InitializeEnvironmentAsync("Local");
        
        _logger.LogInformation("Completed initialization of all environments");
    }
    
    public async Task InitializeEnvironmentAsync(string environment)
    {
        _logger.LogDebug("Initializing {Environment} environment with sample data", environment);
        
        try
        {
            // Generate sample data
            var teams = _sampleDataGenerator.GenerateSampleTeams();
            var competitions = _sampleDataGenerator.GenerateSampleCompetitions();
            var seasons = _sampleDataGenerator.GenerateSampleSeasons();
            var games = _sampleDataGenerator.GenerateSampleGames(teams, competitions, seasons);
            var players = _sampleDataGenerator.GenerateSamplePlayers(teams);
            
            // Add environment-specific variations
            if (environment == "Production")
            {
                // Production has more comprehensive data
                teams.AddRange(GenerateAdditionalTeams());
                games.AddRange(GenerateAdditionalGames(teams, competitions, seasons));
                players.AddRange(GenerateAdditionalPlayers(teams));
            }
            else if (environment == "Local")
            {
                // Local starts empty or with minimal data
                // We'll add just a few items for testing
                teams = teams.Take(2).ToList();
                games = games.Take(1).ToList();
                players = players.Take(3).ToList();
            }
            
            // Insert data into the environment
            await _dataService.InsertManyAsync("teams", teams, environment);
            await _dataService.InsertManyAsync("competitions", competitions, environment);
            await _dataService.InsertManyAsync("seasons", seasons, environment);
            await _dataService.InsertManyAsync("games", games, environment);
            await _dataService.InsertManyAsync("players", players, environment);
            
            _logger.LogInformation("Successfully initialized {Environment} with {Teams} teams, {Competitions} competitions, {Seasons} seasons, {Games} games, {Players} players",
                environment, teams.Count, competitions.Count, seasons.Count, games.Count, players.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize {Environment} environment", environment);
            throw;
        }
    }
    
    public async Task<bool> IsInitializedAsync(string environment)
    {
        try
        {
            var teams = await _dataService.GetAllAsync<Team>("teams", environment);
            return teams.Any();
        }
        catch
        {
            return false;
        }
    }
    
    private List<Team> GenerateAdditionalTeams()
    {
        return new List<Team>
        {
            new Team
            {
                id = "60a1b2c3d4e5f6789abcdef4",
                Name = "Chelsea",
                ShortName = "Chelsea",
                FullName = "Chelsea Football Club",
                Country = "England",
                City = "London",
                Stadium = "Stamford Bridge",
                Founded = new DateTime(1905, 1, 1),
                Colors = new List<string> { "Blue", "White" }
            },
            new Team
            {
                id = "60a1b2c3d4e5f6789abcdef5",
                Name = "Arsenal",
                ShortName = "Arsenal", 
                FullName = "Arsenal Football Club",
                Country = "England",
                City = "London",
                Stadium = "Emirates Stadium",
                Founded = new DateTime(1886, 1, 1),
                Colors = new List<string> { "Red", "White" }
            },
            new Team
            {
                id = "60a1b2c3d4e5f6789abcdef6",
                Name = "Bayern Munich",
                ShortName = "Bayern",
                FullName = "FC Bayern München",
                Country = "Germany",
                City = "Munich", 
                Stadium = "Allianz Arena",
                Founded = new DateTime(1900, 1, 1),
                Colors = new List<string> { "Red", "White" }
            }
        };
    }
    
    private List<Game> GenerateAdditionalGames(List<Team> allTeams, List<Competition> competitions, List<Season> seasons)
    {
        var additionalGames = new List<Game>();
        
        if (allTeams.Count >= 6) // We need at least 6 teams for additional games
        {
            var premierLeague = competitions.FirstOrDefault(c => c.Code == "EPL");
            var currentSeason = seasons.FirstOrDefault(s => s.IsActive);
            
            if (premierLeague != null && currentSeason != null)
            {
                // Chelsea vs Arsenal
                additionalGames.Add(new Game
                {
                    id = "90a1b2c3d4e5f6789abcdef2",
                    Date = new DateTime(2024, 2, 1),
                    HomeTeam = CreateTeamRef(allTeams[4]), // Chelsea
                    AwayTeam = CreateTeamRef(allTeams[5]), // Arsenal
                    Competition = new BasicMonikerRef
                    {
                        id = premierLeague.id,
                        Name = premierLeague.Name,
                        Code = premierLeague.Code
                    },
                    Season = new BasicMonikerRef
                    {
                        id = currentSeason.id,
                        Name = currentSeason.Name,
                        Code = currentSeason.Code
                    },
                    Venue = allTeams[4].Stadium,
                    HomeScore = 1,
                    AwayScore = 2
                });
            }
            
            // Add Champions League game if we have teams from different countries
            var championsLeague = competitions.FirstOrDefault(c => c.Code == "UCL");
            if (championsLeague != null && allTeams.Count >= 7)
            {
                var uclSeason = seasons.FirstOrDefault(s => s.IsActive);
                if (uclSeason != null)
                {
                    additionalGames.Add(new Game
                    {
                        id = "90a1b2c3d4e5f6789abcdef3",
                        Date = new DateTime(2024, 3, 15),
                        HomeTeam = CreateTeamRef(allTeams[6]), // Bayern Munich
                        AwayTeam = CreateTeamRef(allTeams[0]), // Manchester United
                        Competition = new BasicMonikerRef
                        {
                            id = championsLeague.id,
                            Name = championsLeague.Name,
                            Code = championsLeague.Code
                        },
                        Season = new BasicMonikerRef
                        {
                            id = uclSeason.id,
                            Name = uclSeason.Name,
                            Code = uclSeason.Code
                        },
                        Venue = allTeams[6].Stadium,
                        HomeScore = 2,
                        AwayScore = 3
                    });
                }
            }
        }
        
        return additionalGames;
    }
    
    private List<Player> GenerateAdditionalPlayers(List<Team> allTeams)
    {
        var additionalPlayers = new List<Player>();
        
        if (allTeams.Count >= 5) // Chelsea players
        {
            additionalPlayers.AddRange(new List<Player>
            {
                new Player
                {
                    id = "50a1b2c3d4e5f6789abcdef3",
                    FirstName = "Raheem",
                    LastName = "Sterling",
                    DateOfBirth = new DateTime(1994, 12, 8),
                    Country = "England",
                    Position = "Winger",
                    JerseyNumber = 17,
                    CurrentTeam = new DocumentRef(allTeams[4].id)
                }
            });
        }
        
        if (allTeams.Count >= 6) // Arsenal players
        {
            additionalPlayers.AddRange(new List<Player>
            {
                new Player
                {
                    id = "50a1b2c3d4e5f6789abcdef4",
                    FirstName = "Bukayo",
                    LastName = "Saka",
                    DateOfBirth = new DateTime(2001, 9, 5),
                    Country = "England",
                    Position = "Winger",
                    JerseyNumber = 7,
                    CurrentTeam = new DocumentRef(allTeams[5].id)
                }
            });
        }
        
        return additionalPlayers;
    }
    
    private TeamRef CreateTeamRef(Team team)
    {
        return new TeamRef
        {
            id = team.id,
            Name = team.Name,
            ShortName = team.ShortName,
            Country = team.Country
        };
    }
}