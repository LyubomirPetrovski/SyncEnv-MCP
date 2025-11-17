namespace SyncEnvMcp.Models;

// Sync tracking metadata
public class SyncMetadata
{
    public DateTime LastSynced { get; set; }
    public string SourceEnvironment { get; set; } = string.Empty;
    public string SyncedBy { get; set; } = "MCP-Server";
    public int SyncVersion { get; set; } = 1;
    public List<string> DependenciesSynced { get; set; } = new();
}