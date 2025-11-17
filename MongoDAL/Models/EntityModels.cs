using Synergy.Model;

namespace SyncEnvMcp.Models;

// Full Game document (extends your GameRef)
public class Game : Document
{
    public TeamRef AwayTeam { get; set; } = new();
    public TeamRef HomeTeam { get; set; } = new();
    public BasicMonikerRef Competition { get; set; } = new();
    public BasicMonikerRef League { get; set; } = new();
    public BasicMonikerRef Season { get; set; } = new();

    public DateTime Date { get; set; }

    // Additional game details
    public string Venue { get; set; } = string.Empty;
    public int? HomeScore { get; set; }
    public int? AwayScore { get; set; }

    // Relationships to additional data
    public List<DocumentRef> Events { get; set; } = new();

    // Metadata
    public SyncMetadata SyncInfo { get; set; } = new();
}

// Full Team document
public class Team : Document
{
    public string Name { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
    public DateTime Founded { get; set; }
    public string Stadium { get; set; } = string.Empty;
    public List<string> Colors { get; set; } = new();
    
    // Relationships
    public List<DocumentRef> Players { get; set; } = new();
    public List<DocumentRef> Competitions { get; set; } = new();
    
    // Metadata for sync tracking
    public SyncMetadata SyncInfo { get; set; } = new();
}

// Full Competition document  
public class Competition : Document
{
    public string Name { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public CompetitionType Type { get; set; }
    
    // Relationships
    public List<DocumentRef> Seasons { get; set; } = new();
    public List<DocumentRef> ParticipatingTeams { get; set; } = new();
    
    // Metadata
    public SyncMetadata SyncInfo { get; set; } = new();
}

// Full Season document
public class Season : Document
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    
    // Relationships
    public List<DocumentRef> Teams { get; set; } = new();
    public List<DocumentRef> Games { get; set; } = new();
    
    // Metadata
    public SyncMetadata SyncInfo { get; set; } = new();
}

public class Player : Document
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public DateTime DateOfBirth { get; set; }
    public string Country { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public int? JerseyNumber { get; set; }
    
    // Current team
    public DocumentRef CurrentTeam { get; set; } = new();
    
    // Metadata
    public SyncMetadata SyncInfo { get; set; } = new();
}

// Enums for enhanced models
public enum CompetitionType
{
    League,
    Cup,
    Tournament,
    Friendly,
    International
}

