using System.Reflection;
using SyncEnvMcp.Prompts;
using SyncEnvMcp.Services;
using SyncEnvMcp.Tools;

var builder = WebApplication.CreateBuilder(args);

var appDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Directory.GetCurrentDirectory();
builder.Configuration.AddJsonFile(Path.Combine(appDirectory, "appsettings.json"), optional: false, reloadOnChange: true);

// Configure logging
builder.Logging.AddConsole(consoleLogOptions =>
{
    // Configure all logs to go to stderr
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

// Register in-memory services (replacing MongoDB)
builder.Services.AddSingleton<IDataService, InMemoryDataService>();
builder.Services.AddSingleton<IDependencyMapper, DependencyMapper>();
builder.Services.AddSingleton<ISampleDataGenerator, SampleDataGenerator>();
builder.Services.AddSingleton<IDataInitializationHelperService, DataInitializationHelperService>();

// Add MCP Server (with transport for HTTP)
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithTools<EnvironmentTools>()
    .WithTools<GameSyncTools>()
    .WithPrompts<EnvironmentPrompts>();
// .WithToolsFromAssembly(Assembly.LoadFrom(Path.Combine(appDirectory, "SyncEnv.Mcp.dll")));

var app = builder.Build();

// Initialize in-memory data with sample data
var dataInitService = app.Services.GetRequiredService<IDataInitializationHelperService>();
await dataInitService.InitializeAsync();

// Initialize MCP tools with services
ToolInitializer.InitializeTools(app.Services);

// Map MCP endpoints - this should create the /mcp endpoint automatically
app.MapMcp("/mcp");

// Add health check endpoint
app.MapGet("/health", () => "MCP HTTP Server is running");

await app.RunAsync("http://0.0.0.0:5000");

// Make Program class public for testing
public partial class Program { }