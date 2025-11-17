using SyncEnvMcp.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SyncEnvMcp.Tools;

public class ToolInitializer
{
    public static void InitializeTools(IServiceProvider serviceProvider)
    {
        var dataService = serviceProvider.GetRequiredService<IDataService>();
        var dependencyMapper = serviceProvider.GetRequiredService<IDependencyMapper>();
        var sampleDataGenerator = serviceProvider.GetRequiredService<ISampleDataGenerator>();
        var logger = serviceProvider.GetRequiredService<ILogger<ToolInitializer>>();
        
        // Initialize all MCP tools with required services
        EnvironmentTools.Initialize(dataService);
        GameSyncTools.Initialize(dataService, dependencyMapper, logger);
    }
}