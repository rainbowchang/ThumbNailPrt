namespace ThumbnailExplorer.Models;

public class OriginalFile
{
    public string FullPath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public DateTime CreationTime { get; set; }
    public DateTime LastWriteTime { get; set; }
    public long Length { get; set; }
    public string Hash { get; set; } = string.Empty;
}
