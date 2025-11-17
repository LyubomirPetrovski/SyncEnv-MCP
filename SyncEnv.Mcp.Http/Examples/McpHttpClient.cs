using System.Net.Http.Json;
using System.Text.Json;

namespace SyncEnv.Mcp.Http.Examples;

/// <summary>
/// Example client showing how to call the MCP HTTP server with JSON-RPC requests
/// </summary>
public class McpHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public McpHttpClient(string baseUrl = "http://localhost:5000")
    {
        _httpClient = new HttpClient();
        _baseUrl = baseUrl;
    }

    /// <summary>
    /// Call the ListEnvironments tool
    /// </summary>
    public async Task<string> ListEnvironmentsAsync()
    {
        var request = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "tools/call",
            @params = new
            {
                name = "ListEnvironments",
                arguments = new { }
            }
        };

        var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/mcp", request);
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        var jsonResponse = JsonDocument.Parse(content);
        
        return jsonResponse.RootElement.GetProperty("result").GetProperty("content").GetString() ?? "No result";
    }

    /// <summary>
    /// Test connection to an environment
    /// </summary>
    public async Task<string> TestConnectionAsync(string environment = "Local")
    {
        var request = new
        {
            jsonrpc = "2.0",
            id = 2,
            method = "tools/call",
            @params = new
            {
                name = "TestConnection",
                arguments = new { environment }
            }
        };

        var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/mcp", request);
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        var jsonResponse = JsonDocument.Parse(content);
        
        return jsonResponse.RootElement.GetProperty("result").GetProperty("content").GetString() ?? "No result";
    }

    /// <summary>
    /// Get database statistics for an environment
    /// </summary>
    public async Task<string> GetDatabaseStatsAsync(string environment = "Production")
    {
        var request = new
        {
            jsonrpc = "2.0",
            id = 3,
            method = "tools/call",
            @params = new
            {
                name = "GetDatabaseStats",
                arguments = new { environment }
            }
        };

        var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/mcp", request);
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        var jsonResponse = JsonDocument.Parse(content);
        
        return jsonResponse.RootElement.GetProperty("result").GetProperty("content").GetString() ?? "No result";
    }

    /// <summary>
    /// Example usage of the MCP HTTP client
    /// </summary>
    public static async Task RunExampleAsync()
    {
        var client = new McpHttpClient();

        try
        {
            Console.WriteLine("=== MCP HTTP Server Demo ===\n");

            // List environments
            Console.WriteLine("1. Listing environments:");
            var environments = await client.ListEnvironmentsAsync();
            Console.WriteLine(environments);
            Console.WriteLine();

            // Test connection
            Console.WriteLine("2. Testing connection to Local environment:");
            var connectionResult = await client.TestConnectionAsync("Local");
            Console.WriteLine(connectionResult);
            Console.WriteLine();

            // Get database stats
            Console.WriteLine("3. Getting database statistics for Production:");
            var stats = await client.GetDatabaseStatsAsync("Production");
            Console.WriteLine(stats);
            Console.WriteLine();

            Console.WriteLine("=== Demo Complete ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}