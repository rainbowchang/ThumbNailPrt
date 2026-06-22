namespace ThumbnailExplorer.Models;

public class ThumbnailInfo
{
    public int Index { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public DateTime CreatedTime { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}
