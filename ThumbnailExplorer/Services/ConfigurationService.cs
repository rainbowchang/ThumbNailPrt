using System.Text.Json;
using ThumbnailExplorer.Data;
using ThumbnailExplorer.Models;

namespace ThumbnailExplorer.Services;

public class ConfigurationService
{
    private static readonly string ConfigFileName = "config.json";
    private static readonly string AppDataSubFolder = "NQThumbnail";
    private AppConfig _config = new();
    private readonly string _configFilePath;

    public ConfigurationService()
    {
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string configDir = Path.Combine(appDataPath, AppDataSubFolder);
        Directory.CreateDirectory(configDir);
        _configFilePath = Path.Combine(configDir, ConfigFileName);
    }

    public AppConfig LoadConfig()
    {
        if (File.Exists(_configFilePath))
        {
            string json = File.ReadAllText(_configFilePath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            _config = JsonSerializer.Deserialize<AppConfig>(json, options) ?? new AppConfig();
        }
        else
        {
            _config = CreateDefaultConfig();
            SaveConfig(_config);
        }
        return _config;
    }

    public void SaveConfig(AppConfig config)
    {
        _config = config;
        var options = new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
        string json = JsonSerializer.Serialize(_config, options);
        File.WriteAllText(_configFilePath, json);
    }

    public List<RootDirectory> GetRootDirectories()
    {
        return _config.RootDirectories;
    }

    public AppConfig GetConfig()
    {
        return _config;
    }

    public List<string> GetFileExtensions()
    {
        return _config.FileExtensions;
    }

    public string GetPropertyDirectoryRoot()
    {
        string root = _config.PropertyDirectoryRoot;
        if (root.StartsWith("%appdata%"))
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            root = root.Replace("%appdata%", appDataPath);
        }
        return root;
    }

    public int GetMaxThumbnailsPerFile()
    {
        return _config.MaxThumbnailsPerFile;
    }

    public ThumbnailSizeConfig GetThumbnailSize()
    {
        return _config.ThumbnailSize;
    }

    public List<string> GetIgnoredDirectories()
    {
        return _config.IgnoredDirectories;
    }

    private AppConfig CreateDefaultConfig()
    {
        return new AppConfig
        {
            Version = "1.0",
            RootDirectories = new List<RootDirectory>(),
            FileExtensions = new List<string> { ".prt", ".stp", ".igs", ".step", ".iges" },
            PropertyDirectoryRoot = "%appdata%\\NQThumbnail",
            MaxThumbnailsPerFile = 4,
            ThumbnailSize = new ThumbnailSizeConfig { Width = 256, Height = 256 },
            IgnoredDirectories = new List<string> { "node_modules", ".git", "backup" }
        };
    }
}
