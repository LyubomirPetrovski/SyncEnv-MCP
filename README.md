# SyncEnv MCP - Model Context Protocol Demo for .NET

A comprehensive demonstration of implementing the [Model Context Protocol (MCP)](https://modelcontextprotocol.io/) in .NET 9. This sample application showcases how to build MCP servers with both HTTP and StdIO transports, featuring real-world data synchronization scenarios for game/sports data management.

## 🤔 What is MCP?

The Model Context Protocol (MCP) is an open protocol that standardizes how applications provide context to Large Language Models (LLMs). This enables AI assistants like Claude, ChatGPT, and others to interact with your application's data and tools in a standardized way.

## ✨ Features

This demo application demonstrates:

- **Dual Transport Support**: Both HTTP and StdIO (standard input/output) MCP server implementations
- **Custom MCP Tools**: Ready-to-use tools for environment management and data synchronization
- **Dependency Mapping**: Automatic resolution of entity relationships for complex data synchronization
- **In-Memory Data Storage**: Sample data generator with realistic sports/game data
- **Docker Support**: Containerized deployment ready
- **Comprehensive Testing**: Unit and integration tests included

## 🛠️ MCP Tools Implemented

### Environment Tools
- `ListEnvironments` - List all available environments (Production, Local)
- `TestConnection` - Test connection to a specific environment
- `GetDatabaseStats` - Get database statistics for an environment

### Game Sync Tools
- `FindGames` - Search for games by team name and date range
- `PreviewGameSync` - Preview what data would be synced (dry-run mode)
- `SyncGame` - Sync a specific game and all its dependencies between environments

## 📁 Solution Structure

```
SyncEnvMcp/
SyncEnvMcp/
├── SyncEnv.Mcp/                    # Core MCP library
│   ├── Services/                   # Data services and dependency mapping
│   ├── Tools/                      # MCP tool implementations
│   └── Prompts/                    # MCP prompts (future enhancement)
├── SyncEnv.Mcp.Http/               # HTTP transport MCP server
│   ├── Program.cs                  # HTTP server configuration
│   ├── Examples/                   # Example client implementations
│   └── Dockerfile                  # Container configuration
├── SyncEnv.Mcp.StdIo/             # StdIO transport MCP server
│   ├── Program.cs                  # StdIO server configuration
│   └── Dockerfile                  # Container configuration
├── SyncEnv.Mcp.Tests/             # Unit tests
├── SyncEnv.Mcp.Tests.Integration/ # Integration tests
└── MongoDAL/                       # Data models and abstractions
```

## 🚀 Technologies

- **.NET 9** - Latest .NET framework with C# 13
- **ASP.NET Core** - For HTTP transport server
- **ModelContextProtocol.NET** - Official MCP SDK for .NET
- **xUnit** - Testing framework
- **Docker** - Containerization support

## 📦 Installation & Setup

### Prerequisites

- .NET 9 SDK
- Visual Studio 2022 or VS Code
- Docker (optional, for containerized deployment)

### Running the HTTP Server

```bash
cd SyncEnv.Mcp.Http
dotnet run
```

The server will start on `http://0.0.0.0:5000` with the MCP endpoint at `/mcp`.

Test the health endpoint:
```bash
curl http://localhost:5000/health
```

### Running the StdIO Server

```bash
cd SyncEnv.Mcp.StdIo
dotnet run
```

This starts the MCP server using standard input/output for communication with MCP clients.

### Running with Docker

```bash
# Build the HTTP server image
docker build -f SyncEnv.Mcp.Http/Dockerfile -t syncenv-mcp-http .

# Run the container
docker run -p 5000:5000 syncenv-mcp-http
```

## 🧪 Running Tests

```bash
# Run unit tests
dotnet test SyncEnv.Mcp.Tests/SyncEnv.Mcp.Tests.Unit.csproj

# Run integration tests
dotnet test SyncEnv.Mcp.Tests.Integration/SyncEnv.Mcp.Tests.Integration.csproj

# Run all tests
dotnet test
```

## 💡 Usage Examples

### Example 1: List Available Environments

```json
{
  "method": "tools/call",
  "params": {
    "name": "ListEnvironments",
    "arguments": {}
  }
}
```

### Example 2: Find Games by Team

```json
{
  "method": "tools/call",
  "params": {
    "name": "FindGames",
    "arguments": {
      "teamName": "Manchester",
      "startDate": "2024-01-01",
      "environment": "Production"
    }
  }
}
```

### Example 3: Preview Game Sync

```json
{
  "method": "tools/call",
  "params": {
    "name": "PreviewGameSync",
    "arguments": {
      "gameId": "game_12345",
      "sourceEnvironment": "Production"
    }
  }
}
```

### Example 4: Sync Game with Dependencies

```json
{
  "method": "tools/call",
  "params": {
    "name": "SyncGame",
    "arguments": {
      "gameId": "game_12345",
      "sourceEnvironment": "Production",
      "targetEnvironment": "Local"
    }
  }
}
```

## 🔌 Connecting MCP Clients

### Claude Desktop

Add to your Claude Desktop configuration file:

**For HTTP Server:**
```json
{
  "mcpServers": {
    "syncenv": {
      "url": "http://localhost:5000/mcp"
    }
  }
}
```

**For StdIO Server:**
```json
{
  "mcpServers": {
    "syncenv": {
      "command": "dotnet",
      "args": ["run", "--project", "path/to/SyncEnv.Mcp.StdIo"]
    }
  }
}
```

## 🎓 Key Concepts Demonstrated

### 1. MCP Tool Registration
Tools are registered using attributes and dependency injection:

```csharp
[McpServerToolType]
public sealed class EnvironmentTools
{
    [McpServerTool, Description("List all available environments")]
    public static string ListEnvironments() { ... }
}
```

### 2. Dependency Mapping
Automatic resolution of entity relationships for complex data synchronization:

```csharp
public interface IDependencyMapper
{
    Task<DependencyGraph> GetGameDependenciesAsync(string gameId, string environment);
}
```

### 3. Dual Transport Support
The same tools work with both HTTP and StdIO transports:

```csharp
// HTTP Transport
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<EnvironmentTools>();

// StdIO Transport
builder.Services.AddMcpServer()
    .WithStdioTransport()
    .WithTools<EnvironmentTools>();
```

## 📚 Learn More

- [Model Context Protocol Specification](https://spec.modelcontextprotocol.io/)
- [MCP Documentation](https://modelcontextprotocol.io/)
- [.NET MCP SDK](https://github.com/modelcontextprotocol/dotnet-sdk)

## 🤝 Contributing

This is a demonstration project, but feel free to:
- Report issues or suggest improvements
- Fork and enhance with your own MCP tools
- Use as a template for your own MCP implementations

## 📄 License

This is a sample/demonstration project. Feel free to use it for learning and building your own MCP implementations.

## 🔮 Future Enhancements

- [ ] Add MCP Prompts support
- [ ] Implement MCP Resources
- [ ] Add authentication/authorization
- [ ] Support for MongoDB instead of in-memory storage
- [ ] Real-time synchronization with SignalR
- [ ] Add more complex dependency scenarios
- [ ] GraphQL integration example

## 📧 Contact

Created as a demonstration of MCP in .NET - feel free to learn from and adapt this code for your own projects!

---

**Note**: This is a sample application designed to demonstrate MCP concepts. The in-memory data storage is intentionally simplified for learning purposes. In production scenarios, you would integrate with actual databases and implement proper security measures.
