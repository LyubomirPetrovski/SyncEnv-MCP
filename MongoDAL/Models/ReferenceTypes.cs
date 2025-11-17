using Synergy.Model;

namespace SyncEnvMcp.Models;

public class TeamRef : DocumentRef
{
    public string Name { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
}

public class BasicMonikerRef : DocumentRef
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}
