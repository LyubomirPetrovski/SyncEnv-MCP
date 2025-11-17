using SyncEnvMcp.Services;
using SyncEnvMcp.Tools;
using System.Reflection;

var builder = Host.CreateApplicationBuilder(args);

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

// Add MCP Server
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly(Assembly.LoadFrom(Path.Combine(appDirectory, "SyncEnv.Mcp.dll")));

var app = builder.Build();

// Initialize in-memory data with sample data
var dataInitService = app.Services.GetRequiredService<IDataInitializationHelperService>();
await dataInitService.InitializeAsync();

// Initialize MCP tools with services
ToolInitializer.InitializeTools(app.Services);

await app.RunAsync();