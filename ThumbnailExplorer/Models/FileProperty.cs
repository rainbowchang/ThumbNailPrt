namespace ThumbnailExplorer.Models;

public class FileProperty
{
    public string FileHash { get; set; } = string.Empty;
    public string CustomName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public string Category { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedTime { get; set; }
    public DateTime ModifiedTime { get; set; }
}
