namespace ThumbnailExplorer.Models;

public class RootDirectory
{
    public string Path { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public DateTime? LastScanTime { get; set; }
}
