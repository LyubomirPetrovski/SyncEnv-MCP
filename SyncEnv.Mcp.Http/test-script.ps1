# MCP HTTP Server Demo Script
# This script demonstrates the proper MCP HTTP communication flow

# Load required .NET assemblies for HttpClient
Add-Type -AssemblyName System.Net.Http

Write-Host "=== MCP HTTP Server Demo ===" -ForegroundColor Green

# Note: Make sure the server is running first with: dotnet run
$baseUrl = "http://localhost:5000"

# Helper function to parse SSE response
function Parse-SSEResponse {
    param([string]$response)
    
    # SSE responses have format: "event: message\ndata: {json}\n\n"
    $lines = $response -split "`n"
    foreach ($line in $lines) {
        if ($line.StartsWith("data: ")) {
            $jsonData = $line.Substring(6)
            try {
                return $jsonData | ConvertFrom-Json
            } catch {
                return $jsonData
            }
        }
    }
    return $response
}

Write-Host "`n1. Testing Health Endpoint..." -ForegroundColor Yellow
try {
    $healthResponse = Invoke-RestMethod -Uri "$baseUrl/health" -Method Get
    Write-Host "✅ Health Check: $healthResponse" -ForegroundColor Green
} catch {
    Write-Host "❌ Health check failed. Make sure server is running with 'dotnet run'" -ForegroundColor Red
    exit 1
}

Write-Host "`n2. Testing List Tools via MCP..." -ForegroundColor Yellow
try {
    $listToolsRequest = @{
        jsonrpc = "2.0"
        id = 0
        method = "tools/list"
        params = @{
        }
    } | ConvertTo-Json -Depth 3
    
    $headers = @{
        "Accept" = "application/json, text/event-stream"
        "Content-Type" = "application/json"
    }
    
    $rawResponse = Invoke-RestMethod -Uri "$baseUrl/mcp" -Method Post -Body $listToolsRequest -Headers $headers
    $toolsResponse = Parse-SSEResponse -response $rawResponse
    
    Write-Host "✅ Available Tools:" -ForegroundColor Green
    if ($toolsResponse.result -and $toolsResponse.result.tools) {
        $toolsResponse.result.tools | ForEach-Object { 
            Write-Host "  • $($_.name): $($_.description)" -ForegroundColor Cyan
        }
    } else {
        Write-Host "  $toolsResponse" -ForegroundColor Cyan
    }
} catch {
    Write-Host "❌ Failed to get tools: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n3. Testing MCP Tool Calls..." -ForegroundColor Yellow

# Test list_environments (using snake_case as MCP converts PascalCase to snake_case)
Write-Host "`n🔍 Testing list_environments..." -ForegroundColor Yellow
try {
    $listEnvRequest = @{
        jsonrpc = "2.0"
        id = 1
        method = "tools/call"
        params = @{
            name = "list_environments"
            arguments = @{
            }
        }
    } | ConvertTo-Json -Depth 3
    
    $headers = @{
        "Accept" = "application/json, text/event-stream"
        "Content-Type" = "application/json"
    }
    
    $rawResponse = Invoke-RestMethod -Uri "$baseUrl/mcp" -Method Post -Body $listEnvRequest -Headers $headers
    $listEnvResponse = Parse-SSEResponse -response $rawResponse
    
    Write-Host "✅ list_environments Result:" -ForegroundColor Green
    if ($listEnvResponse.result -and $listEnvResponse.result.content) {
        $content = ($listEnvResponse.result.content | ForEach-Object { $_.text }) -join "`n"
        Write-Host "   $content" -ForegroundColor Cyan
    } else {
        Write-Host "   $listEnvResponse" -ForegroundColor Cyan
    }
} catch {
    Write-Host "❌ list_environments failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test test_connection
Write-Host "`n🔍 Testing test_connection..." -ForegroundColor Yellow
try {
    $testConnRequest = @{
        jsonrpc = "2.0"
        id = 2
        method = "tools/call"
        params = @{
            name = "test_connection"
            arguments = @{
                environment = "Local"
            }
        }
    } | ConvertTo-Json -Depth 3
    
    $headers = @{
        "Accept" = "application/json, text/event-stream"
        "Content-Type" = "application/json"
    }
    
    $rawResponse = Invoke-RestMethod -Uri "$baseUrl/mcp" -Method Post -Body $testConnRequest -Headers $headers
    $testConnResponse = Parse-SSEResponse -response $rawResponse
    
    Write-Host "✅ test_connection Result:" -ForegroundColor Green
    if ($testConnResponse.result -and $testConnResponse.result.content) {
        $content = ($testConnResponse.result.content | ForEach-Object { $_.text }) -join "`n"
        Write-Host "   $content" -ForegroundColor Cyan
    } else {
        Write-Host "   $testConnResponse" -ForegroundColor Cyan
    }
} catch {
    Write-Host "❌ test_connection failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test get_database_stats
Write-Host "`n🔍 Testing get_database_stats..." -ForegroundColor Yellow
try {
    $getStatsRequest = @{
        jsonrpc = "2.0"
        id = 3
        method = "tools/call"
        params = @{
            name = "get_database_stats"
            arguments = @{
                environment = "Production"
            }
        }
    } | ConvertTo-Json -Depth 3
    
    $headers = @{
        "Accept" = "application/json, text/event-stream"
        "Content-Type" = "application/json"
    }
    
    $rawResponse = Invoke-RestMethod -Uri "$baseUrl/mcp" -Method Post -Body $getStatsRequest -Headers $headers
    $getStatsResponse = Parse-SSEResponse -response $rawResponse
    
    Write-Host "✅ get_database_stats Result:" -ForegroundColor Green
    if ($getStatsResponse.result -and $getStatsResponse.result.content) {
        $content = ($getStatsResponse.result.content | ForEach-Object { $_.text }) -join "`n"
        Write-Host "   $content" -ForegroundColor Cyan
    } else {
        Write-Host "   $getStatsResponse" -ForegroundColor Cyan
    }
} catch {
    Write-Host "❌ get_database_stats failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test find_games
Write-Host "`n🔍 Testing find_games..." -ForegroundColor Yellow
try {
    $findGamesRequest = @{
        jsonrpc = "2.0"
        id = 4
        method = "tools/call"
        params = @{
            name = "find_games"
            arguments = @{
                teamName = "Arsenal"
                environment = "Production"
            }
        }
    } | ConvertTo-Json -Depth 3
    
    $headers = @{
        "Accept" = "application/json, text/event-stream"
        "Content-Type" = "application/json"
    }
    
    $rawResponse = Invoke-RestMethod -Uri "$baseUrl/mcp" -Method Post -Body $findGamesRequest -Headers $headers
    $findGamesResponse = Parse-SSEResponse -response $rawResponse
    
    Write-Host "✅ find_games Result:" -ForegroundColor Green
    if ($findGamesResponse.result -and $findGamesResponse.result.content) {
        $content = ($findGamesResponse.result.content | ForEach-Object { $_.text }) -join "`n"
        Write-Host "   $content" -ForegroundColor Cyan
    } else {
        Write-Host "   $findGamesResponse" -ForegroundColor Cyan
    }
} catch {
    Write-Host "❌ find_games failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=== Demo Complete ===" -ForegroundColor Green
Write-Host "📋 Summary:" -ForegroundColor Yellow
Write-Host "   • Health endpoint: Direct HTTP GET to /health" -ForegroundColor Gray
Write-Host "   • MCP requests: JSON-RPC POST to /mcp endpoint" -ForegroundColor Gray
Write-Host "   • All MCP methods use the same /mcp endpoint" -ForegroundColor Gray
Write-Host "   • Accept header must include: application/json, text/event-stream" -ForegroundColor Gray
Write-Host "   • Tool names are in snake_case (list_environments, not ListEnvironments)" -ForegroundColor Gray
Write-Host "`n🚀 To run the server: dotnet run" -ForegroundColor Yellow
Write-Host "🧪 To run tests: dotnet test" -ForegroundColor Yellow