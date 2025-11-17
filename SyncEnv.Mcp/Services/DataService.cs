using Microsoft.Extensions.Logging;
using SyncEnvMcp.Models;
using System.Linq.Expressions;

namespace SyncEnvMcp.Services;

/// <summary>
/// Simple data service interface that replaces IMongoService for in-memory collections
/// </summary>
public interface IDataService
{
    /// <summary>
    /// Get all items from a collection in specific environment
    /// </summary>
    Task<List<T>> GetAllAsync<T>(string collectionName, string environment = "Local") where T : class;
    
    /// <summary>
    /// Find items by predicate
    /// </summary>
    Task<List<T>> FindAsync<T>(string collectionName, Expression<Func<T, bool>> predicate, string environment = "Local") where T : class;
    
    /// <summary>
    /// Find single item by predicate  
    /// </summary>
    Task<T?> FindOneAsync<T>(string collectionName, Expression<Func<T, bool>> predicate, string environment = "Local") where T : class;
    
    /// <summary>
    /// Find items with text search (for team names, etc.)
    /// </summary>
    Task<List<T>> FindByTextAsync<T>(string collectionName, string searchText, string environment = "Local") where T : class;
    
    /// <summary>
    /// Insert multiple items
    /// </summary>
    Task InsertManyAsync<T>(string collectionName, IEnumerable<T> items, string environment = "Local") where T : class;
    
    /// <summary>
    /// Insert single item
    /// </summary>
    Task InsertOneAsync<T>(string collectionName, T item, string environment = "Local") where T : class;
    
    /// <summary>
    /// Replace or insert item (upsert)
    /// </summary>
    Task ReplaceOneAsync<T>(string collectionName, Expression<Func<T, bool>> predicate, T replacement, string environment = "Local") where T : class;
    
    /// <summary>
    /// Get available environments
    /// </summary>
    IEnumerable<string> GetAvailableEnvironments();
    
    /// <summary>
    /// Test connection (always returns true for in-memory)
    /// </summary>
    Task<bool> TestConnectionAsync(string environment);
    
    /// <summary>
    /// Get database statistics
    /// </summary>
    Task<DatabaseStats> GetDatabaseStatsAsync(string environment);
    
    /// <summary>
    /// Clear all data from environment
    /// </summary>
    Task ClearEnvironmentAsync(string environment);
}

public class InMemoryDataService : IDataService
{
    private readonly ILogger<InMemoryDataService> _logger;
    
    // Environment -> Collection -> List of items
    private readonly Dictionary<string, Dictionary<string, object>> _data = new();
    
    // Available environments
    private readonly string[] _environments = { "Production", "Local" };
    
    public InMemoryDataService(ILogger<InMemoryDataService> logger)
    {
        _logger = logger;
        
        // Initialize environments
        foreach (var env in _environments)
        {
            _data[env] = new Dictionary<string, object>();
        }
    }
    
    public async Task<List<T>> GetAllAsync<T>(string collectionName, string environment = "Local") where T : class
    {
        await Task.CompletedTask; // Simulate async
        
        var environmentData = GetEnvironmentData(environment);
        
        if (!environmentData.ContainsKey(collectionName))
        {
            return new List<T>();
        }
        
        var collection = (List<T>)environmentData[collectionName];
        return new List<T>(collection); // Return copy
    }
    
    public async Task<List<T>> FindAsync<T>(string collectionName, Expression<Func<T, bool>> predicate, string environment = "Local") where T : class
    {
        var collection = await GetAllAsync<T>(collectionName, environment);
        var compiledPredicate = predicate.Compile();
        return collection.Where(compiledPredicate).ToList();
    }
    
    public async Task<T?> FindOneAsync<T>(string collectionName, Expression<Func<T, bool>> predicate, string environment = "Local") where T : class
    {
        var collection = await GetAllAsync<T>(collectionName, environment);
        var compiledPredicate = predicate.Compile();
        return collection.FirstOrDefault(compiledPredicate);
    }
    
    public async Task<List<T>> FindByTextAsync<T>(string collectionName, string searchText, string environment = "Local") where T : class
    {
        var collection = await GetAllAsync<T>(collectionName, environment);
        
        // Simple text search implementation
        return collection.Where(item =>
        {
            if (item is Game game)
            {
                return game.HomeTeam?.Name?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true ||
                       game.AwayTeam?.Name?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true;
            }
            else if (item is Team team)
            {
                return team.Name?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true ||
                       team.ShortName?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true;
            }
            else if (item is Player player)
            {
                return player.FirstName?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true ||
                       player.LastName?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true;
            }
            
            return false;
        }).ToList();
    }
    
    public async Task InsertManyAsync<T>(string collectionName, IEnumerable<T> items, string environment = "Local") where T : class
    {
        await Task.CompletedTask; // Simulate async
        
        var environmentData = GetEnvironmentData(environment);
        var collection = GetOrCreateCollection<T>(environmentData, collectionName);
        
        lock (collection)
        {
            collection.AddRange(items);
        }
        
        _logger.LogDebug("Inserted {Count} items into {Collection} in {Environment}", 
            items.Count(), collectionName, environment);
    }
    
    public async Task InsertOneAsync<T>(string collectionName, T item, string environment = "Local") where T : class
    {
        await InsertManyAsync(collectionName, new[] { item }, environment);
    }
    
    public async Task ReplaceOneAsync<T>(string collectionName, Expression<Func<T, bool>> predicate, T replacement, string environment = "Local") where T : class
    {
        await Task.CompletedTask; // Simulate async
        
        var environmentData = GetEnvironmentData(environment);
        var collection = GetOrCreateCollection<T>(environmentData, collectionName);
        var compiledPredicate = predicate.Compile();
        
        lock (collection)
        {
            var index = collection.FindIndex(item => compiledPredicate(item));
            if (index >= 0)
            {
                collection[index] = replacement;
                _logger.LogDebug("Replaced item in {Collection} in {Environment}", collectionName, environment);
            }
            else
            {
                // Upsert - insert if not found
                collection.Add(replacement);
                _logger.LogDebug("Inserted new item in {Collection} in {Environment}", collectionName, environment);
            }
        }
    }
    
    public IEnumerable<string> GetAvailableEnvironments()
    {
        return _environments;
    }
    
    public async Task<bool> TestConnectionAsync(string environment)
    {
        await Task.CompletedTask;
        var exists = _data.ContainsKey(environment);
        _logger.LogInformation("Connection test for {Environment}: {Result}", environment, exists ? "Success" : "Failed");
        return exists;
    }
    
    public async Task<DatabaseStats> GetDatabaseStatsAsync(string environment)
    {
        await Task.CompletedTask;
        
        if (!_data.ContainsKey(environment))
        {
            return new DatabaseStats { Environment = environment, Error = "Environment not found" };
        }
        
        var environmentData = _data[environment];
        var totalObjects = 0L;
        var collections = environmentData.Count;
        
        foreach (var collection in environmentData.Values)
        {
            if (collection is ICollection<object> list)
            {
                totalObjects += list.Count;
            }
        }
        
        // Simulate data size (rough estimate: 1KB per object)
        var estimatedDataSize = totalObjects * 1024;
        
        return new DatabaseStats
        {
            Environment = environment,
            DatabaseName = $"InMemory_{environment}",
            Collections = collections,
            Objects = totalObjects,
            DataSize = estimatedDataSize,
            StorageSize = estimatedDataSize // Same as data size for in-memory
        };
    }
    
    public async Task ClearEnvironmentAsync(string environment)
    {
        await Task.CompletedTask;
        
        if (_data.ContainsKey(environment))
        {
            _data[environment].Clear();
            _logger.LogInformation("Cleared all data from {Environment}", environment);
        }
    }
    
    private Dictionary<string, object> GetEnvironmentData(string environment)
    {
        if (!_data.ContainsKey(environment))
        {
            throw new ArgumentException($"Environment '{environment}' not found. Available environments: {string.Join(", ", _environments)}");
        }
        
        return _data[environment];
    }
    
    private List<T> GetOrCreateCollection<T>(Dictionary<string, object> environmentData, string collectionName) where T : class
    {
        if (!environmentData.ContainsKey(collectionName))
        {
            environmentData[collectionName] = new List<T>();
        }
        
        return (List<T>)environmentData[collectionName];
    }
}

public class DatabaseStats
{
    public string Environment { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public int Collections { get; set; }
    public long Objects { get; set; }
    public long DataSize { get; set; }
    public long StorageSize { get; set; }
    public string? Error { get; set; }
    
    public string FormatDataSize() => FormatBytes(DataSize);
    public string FormatStorageSize() => FormatBytes(StorageSize);
    
    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}