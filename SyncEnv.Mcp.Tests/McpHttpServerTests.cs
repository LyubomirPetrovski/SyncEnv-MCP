using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace SyncEnv.Mcp.Http.Tests;

public class McpHttpServerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public McpHttpServerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task HealthEndpoint_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/health");
        
        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("MCP HTTP Server is running", content);
    }

    [Fact]
    public async Task McpJsonRpcCall_ListEnvironments_ShouldReturnSuccess()
    {
        // Arrange
        var jsonRpcRequest = new
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

        // Act
        var response = await _client.PostAsJsonAsync("/mcp", jsonRpcRequest);
        
        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var jsonResponse = JsonDocument.Parse(content);
        
        Assert.Equal("2.0", jsonResponse.RootElement.GetProperty("jsonrpc").GetString());
        Assert.Equal(1, jsonResponse.RootElement.GetProperty("id").GetInt32());
        Assert.True(jsonResponse.RootElement.TryGetProperty("result", out _));
    }

    [Fact]
    public async Task McpJsonRpcCall_TestConnection_ShouldReturnSuccess()
    {
        // Arrange
        var jsonRpcRequest = new
        {
            jsonrpc = "2.0",
            id = 2,
            method = "tools/call",
            @params = new
            {
                name = "TestConnection",
                arguments = new
                {
                    environment = "Local"
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/mcp", jsonRpcRequest);
        
        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var jsonResponse = JsonDocument.Parse(content);
        
        Assert.Equal("2.0", jsonResponse.RootElement.GetProperty("jsonrpc").GetString());
        Assert.Equal(2, jsonResponse.RootElement.GetProperty("id").GetInt32());
        Assert.True(jsonResponse.RootElement.TryGetProperty("result", out var result));
        
        var resultContent = result.GetProperty("content").GetString();
        Assert.Contains("Local", resultContent);
    }

    [Fact]
    public async Task McpJsonRpcCall_GetDatabaseStats_ShouldReturnSuccess()
    {
        // Arrange
        var jsonRpcRequest = new
        {
            jsonrpc = "2.0",
            id = 3,
            method = "tools/call",
            @params = new
            {
                name = "GetDatabaseStats",
                arguments = new
                {
                    environment = "Production"
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/mcp", jsonRpcRequest);
        
        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var jsonResponse = JsonDocument.Parse(content);
        
        Assert.Equal("2.0", jsonResponse.RootElement.GetProperty("jsonrpc").GetString());
        Assert.Equal(3, jsonResponse.RootElement.GetProperty("id").GetInt32());
        Assert.True(jsonResponse.RootElement.TryGetProperty("result", out var result));
        
        var resultContent = result.GetProperty("content").GetString();
        Assert.Contains("Database Stats", resultContent);
    }
}