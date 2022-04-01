using Daemon.Application.Common;

namespace Daemon.Application.Settings;

public class CoreConfiguration
{
    public string? Version { get; set; }
    public string? HubUrl { get; set; }
    public string? ServerUrl { get; set; }
    public Mode Mode { get; set; }
    public string? ConfigurationFilePath { get; set; }
}
