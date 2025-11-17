using SyncEnvMcp.Models;
using Synergy.Model;

namespace SyncEnvMcp.Services;

public interface ISampleDataGenerator
{
    /// <summary>
    /// Generate sample teams
    /// </summary>
    List<Team> GenerateSampleTeams();
    
    /// <summary>
    /// Generate sample competitions
    /// </summary>
    List<Competition> GenerateSampleCompetitions();
    
    /// <summary>
    /// Generate sample seasons
    /// </summary>
    List<Season> GenerateSampleSeasons();
    
    /// <summary>
    /// Generate sample games
    /// </summary>
    List<Game> GenerateSampleGames(List<Team> teams, List<Competition> competitions, List<Season> seasons);
    
    /// <summary>
    /// Generate sample players
    /// </summary>
    List<Player> GenerateSamplePlayers(List<Team> teams);
}

public class SampleDataGenerator : ISampleDataGenerator
{
    public List<Team> GenerateSampleTeams()
    {
        return new List<Team>
        {
            new Team
            {
                id = "60a1b2c3d4e5f6789abcdef0",
                Name = "Manchester United",
                ShortName = "Man Utd",
                FullName = "Manchester United Football Club",
                Country = "England",
                City = "Manchester",
                Stadium = "Old Trafford",
                Founded = new DateTime(1878, 1, 1),
                Colors = new List<string> { "Red", "White" },
                LogoUrl = "https://example.com/logos/manutd.png"
            },
            new Team
            {
                id = "60a1b2c3d4e5f6789abcdef1",
                Name = "Liverpool",
                ShortName = "Liverpool",
                FullName = "Liverpool Football Club",
                Country = "England",
                City = "Liverpool",
                Stadium = "Anfield",
                Founded = new DateTime(1892, 1, 1),
                Colors = new List<string> { "Red", "White" },
                LogoUrl = "https://example.com/logos/liverpool.png"
            },
            new Team
            {
                id = "60a1b2c3d4e5f6789abcdef2",
                Name = "Barcelona",
                ShortName = "Barca",
                FullName = "Futbol Club Barcelona",
                Country = "Spain",
                City = "Barcelona",
                Stadium = "Camp Nou",
                Founded = new DateTime(1899, 1, 1),
                Colors = new List<string> { "Blue", "Red" },
                LogoUrl = "https://example.com/logos/barcelona.png"
            },
            new Team
            {
                id = "60a1b2c3d4e5f6789abcdef3",
                Name = "Real Madrid",
                ShortName = "Real",
                FullName = "Real Madrid Club de Futbol",
                Country = "Spain",
                City = "Madrid",
                Stadium = "Santiago Bernabeu",
                Founded = new DateTime(1902, 1, 1),
                Colors = new List<string> { "White" },
                LogoUrl = "https://example.com/logos/realmadrid.png"
            }
        };
    }
    
    public List<Competition> GenerateSampleCompetitions()
    {
        return new List<Competition>
        {
            new Competition
            {
                id = "70a1b2c3d4e5f6789abcdef0",
                Name = "Premier League",
                ShortName = "PL",
                Code = "EPL",
                Country = "England",
                Type = CompetitionType.League
            },
            new Competition
            {
                id = "70a1b2c3d4e5f6789abcdef1",
                Name = "La Liga",
                ShortName = "La Liga",
                Code = "ESP1",
                Country = "Spain",
                Type = CompetitionType.League
            },
            new Competition
            {
                id = "70a1b2c3d4e5f6789abcdef2",
                Name = "Champions League",
                ShortName = "UCL",
                Code = "UCL",
                Country = "Europe",
                Type = CompetitionType.Cup
            }
        };
    }
    
    public List<Season> GenerateSampleSeasons()
    {
        var seasons = new List<Season>();
        
        // Generate independent seasons without competition references
        seasons.Add(new Season
        {
            id = "80a1b2c3d4e5f6789abcdef0",
            Name = "2023-24",
            Code = "2023-24",
            StartDate = new DateTime(2023, 8, 1),
            EndDate = new DateTime(2024, 5, 31),
            IsActive = true
        });
        
        seasons.Add(new Season
        {
            id = "80a1b2c3d4e5f6789abcdef1",
            Name = "2024-25",
            Code = "2024-25",
            StartDate = new DateTime(2024, 8, 1),
            EndDate = new DateTime(2025, 5, 31),
            IsActive = false
        });
        
        seasons.Add(new Season
        {
            id = "80a1b2c3d4e5f6789abcdef2",
            Name = "2022-23",
            Code = "2022-23",
            StartDate = new DateTime(2022, 8, 1),
            EndDate = new DateTime(2023, 5, 31),
            IsActive = false
        });
        
        return seasons;
    }
    
    public List<Game> GenerateSampleGames(List<Team> teams, List<Competition> competitions, List<Season> seasons)
    {
        var games = new List<Game>();
        var premierLeague = competitions.FirstOrDefault(c => c.Code == "EPL");
        var currentSeason = seasons.FirstOrDefault(s => s.IsActive);
        
        if (premierLeague == null || currentSeason == null) return games;
        
        // Create some sample games
        games.Add(new Game
        {
            id = "90a1b2c3d4e5f6789abcdef0",
            Date = new DateTime(2024, 1, 15),
            HomeTeam = new TeamRef
            {
                id = teams[0].id,
                Name = teams[0].Name,
                ShortName = teams[0].ShortName,
                Country = teams[0].Country
            },
            AwayTeam = new TeamRef
            {
                id = teams[1].id,
                Name = teams[1].Name,
                ShortName = teams[1].ShortName,
                Country = teams[1].Country
            },
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
            Venue = teams[0].Stadium,
            HomeScore = 2,
            AwayScore = 1,
        });
        
        games.Add(new Game
        {
            id = "90a1b2c3d4e5f6789abcdef1",
            Date = new DateTime(2024, 1, 22),
            HomeTeam = new TeamRef
            {
                id = teams[2].id,
                Name = teams[2].Name,
                ShortName = teams[2].ShortName,
                Country = teams[2].Country
            },
            AwayTeam = new TeamRef
            {
                id = teams[3].id,
                Name = teams[3].Name,
                ShortName = teams[3].ShortName,
                Country = teams[3].Country
            },
            Competition = new BasicMonikerRef
            {
                id = competitions[1].id, // La Liga
                Name = competitions[1].Name,
                Code = competitions[1].Code
            },
            Season = new BasicMonikerRef
            {
                id = currentSeason.id,
                Name = currentSeason.Name,
                Code = currentSeason.Code
            },
            Venue = teams[2].Stadium,
        });
        
        return games;
    }
    
    public List<Player> GenerateSamplePlayers(List<Team> teams)
    {
        var players = new List<Player>();
        
        // Manchester United players
        if (teams.Count > 0)
        {
            players.AddRange(new List<Player>
            {
                new Player
                {
                    id = "50a1b2c3d4e5f6789abcdef0",
                    FirstName = "Marcus",
                    LastName = "Rashford",
                    DateOfBirth = new DateTime(1997, 10, 31),
                    Country = "England",
                    Position = "Forward",
                    JerseyNumber = 10,
                    CurrentTeam = new DocumentRef(teams[0].id)
                },
                new Player
                {
                    id = "50a1b2c3d4e5f6789abcdef1",
                    FirstName = "Bruno",
                    LastName = "Fernandes",
                    DateOfBirth = new DateTime(1994, 9, 8),
                    Country = "Portugal",
                    Position = "Midfielder",
                    JerseyNumber = 18,
                    CurrentTeam = new DocumentRef(teams[0].id)
                }
            });
        }
        
        // Liverpool players
        if (teams.Count > 1)
        {
            players.AddRange(new List<Player>
            {
                new Player
                {
                    id = "50a1b2c3d4e5f6789abcdef2",
                    FirstName = "Mohamed",
                    LastName = "Salah",
                    DateOfBirth = new DateTime(1992, 6, 15),
                    Country = "Egypt",
                    Position = "Forward",
                    JerseyNumber = 11,
                    CurrentTeam = new DocumentRef(teams[1].id)
                }
            });
        }
        
        return players;
    }
}