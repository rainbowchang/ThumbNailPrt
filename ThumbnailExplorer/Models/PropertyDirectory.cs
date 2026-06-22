namespace ThumbnailExplorer.Models;

public class PropertyDirectory
{
    public string Hash { get; set; } = string.Empty;
    public string RootPath { get; set; } = string.Empty;
    public string DirectoryPath { get; set; } = string.Empty;
    public List<ThumbnailInfo> Thumbnails { get; set; } = new();
    public FileProperty Properties { get; set; } = new();
    public string OriginalPath { get; set; } = string.Empty;
}
