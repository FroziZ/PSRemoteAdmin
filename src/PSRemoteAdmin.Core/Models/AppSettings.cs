namespace PSRemoteAdmin.Core.Models;

public class AppSettings
{
    public string LdapConnectionString { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public int WinRmPort { get; set; } = 5985;
    public int MaxConcurrency { get; set; } = 10;
    public string? RunAsUsername { get; set; }
    public string LogFilePath { get; set; } =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PSRemoteAdmin", "logs", "app-.log");
}
