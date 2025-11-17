using ModelContextProtocol.Server;
using System.ComponentModel;
using SyncEnvMcp.Services;

namespace SyncEnvMcp.Tools;

[McpServerToolType]
public sealed class EnvironmentTools
{
    private static IDataService? _dataService;
    
    public static void Initialize(IDataService dataService)
    {
        _dataService = dataService;
    }
    
    [McpServerTool, Description("List all available environments (Production, Local).")]
    public static string ListEnvironments()
    {
        if (_dataService == null)
            return "Error: Service not initialized";
            
        var environments = _dataService.GetAvailableEnvironments();
        return $"Available environments: {string.Join(", ", environments)}";
    }
    
    [McpServerTool, Description("Test connection to a specific environment.")]
    public static async Task<string> TestConnection(
        [Description("Environment name (Production, Local)")] string environment = "Local")
    {
        if (_dataService == null)
            return "Error: Service not initialized";
            
        try
        {
            var isConnected = await _dataService.TestConnectionAsync(environment);
            return isConnected 
                ? $"✅ Successfully connected to {environment}" 
                : $"❌ Failed to connect to {environment}";
        }
        catch (Exception ex)
        {
            return $"❌ Connection error to {environment}: {ex.Message}";
        }
    }
    
    [McpServerTool, Description("Get database statistics for an environment.")]
    public static async Task<string> GetDatabaseStats(
        [Description("Environment name (Production, Local)")] string environment = "Local")
    {
        if (_dataService == null)
            return "Error: Service not initialized";
            
        try
        {
            var stats = await _dataService.GetDatabaseStatsAsync(environment);
            
            if (!string.IsNullOrEmpty(stats.Error))
            {
                return $"❌ Error getting stats for {environment}: {stats.Error}";
            }
            
            return $"""
                📊 Database Stats for {stats.Environment}:
                Database: {stats.DatabaseName}
                Collections: {stats.Collections}
                Documents: {stats.Objects:N0}
                Data Size: {stats.FormatDataSize()}
                Storage Size: {stats.FormatStorageSize()}
                """;
        }
        catch (Exception ex)
        {
            return $"❌ Error getting stats for {environment}: {ex.Message}";
        }
    }
}