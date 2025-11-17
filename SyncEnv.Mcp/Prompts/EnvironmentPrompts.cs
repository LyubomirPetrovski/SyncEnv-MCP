using ModelContextProtocol.Server;
using System.ComponentModel;

namespace SyncEnvMcp.Prompts;

[McpServerPromptType]
public sealed class EnvironmentPrompts
{
    [McpServerPrompt, Description("Get an overview of all environments and their database statistics")]
    public static string EnvironmentOverview()
    {
        return """
        Please provide a comprehensive overview of all environments:
        
        1. First, list all available environments using the list_environments tool
        2. For each environment, test the connection using test_connection
        3. For connected environments, retrieve database statistics using get_database_stats
        4. Present the information in a clear, organized format showing:
           - Environment name and connection status
           - Database name and collection count
           - Total documents and storage sizes
           - Any connection errors or issues
        
        This will help me understand the current state of all my environments at a glance.
        """;
    }
}
