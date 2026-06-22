namespace ThumbnailExplorer.Data;

public class ThumbnailSizeConfig
{
    public int Width { get; set; } = 256;
    public int Height { get; set; } = 256;
}

public class AppConfig
{
    public string Version { get; set; } = "1.0";
    public List<Models.RootDirectory> RootDirectories { get; set; } = new();
    public List<string> FileExtensions { get; set; } = new() { ".prt", ".stp", ".igs", ".step", ".iges" };
    public string PropertyDirectoryRoot { get; set; } = "%appdata%\\NQThumbnail";
    public int MaxThumbnailsPerFile { get; set; } = 4;
    public ThumbnailSizeConfig ThumbnailSize { get; set; } = new();
    public List<string> IgnoredDirectories { get; set; } = new() { "node_modules", ".git", "backup" };
}
