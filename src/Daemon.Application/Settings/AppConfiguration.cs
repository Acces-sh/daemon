namespace Daemon.Application.Settings;

public class AppConfiguration
{
    public CoreConfiguration Core { get; set; }
    public string ApiToken { get; set; } = "";
    public string AuthorizedKeysFilePath { get; set; } = "/root/.ssh/authorized_keys";
}
