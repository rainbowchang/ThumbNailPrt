using ThumbnailExplorer.Models;

namespace ThumbnailExplorer.Services;

public class ScanResult
{
    public int TotalFiles { get; set; }
    public int TotalPropertyDirs { get; set; }
    public int OrphanedDirs { get; set; }
    public List<string> OrphanedPaths { get; set; } = new();
}

public class ScanService
{
    private readonly ConfigurationService _configService;
    private readonly FileSystemService _fileSystemService;
    private readonly PropertyDirectoryService _propertyDirService;

    public ScanService(ConfigurationService configService, FileSystemService fileSystemService, PropertyDirectoryService propertyDirService)
    {
        _configService = configService;
        _fileSystemService = fileSystemService;
        _propertyDirService = propertyDirService;
    }

    public ScanResult ScanAll()
    {
        var result = new ScanResult();

        // 收集所有根目录下的原始文件
        var rootDirs = _configService.GetRootDirectories();
        var extensions = _configService.GetFileExtensions();
        var ignoredDirs = _configService.GetIgnoredDirectories();

        var allOriginalFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rootDir in rootDirs)
        {
            if (!rootDir.IsEnabled || !Directory.Exists(rootDir.Path))
                continue;

            var files = _fileSystemService.GetFilesRecursive(rootDir.Path, extensions, ignoredDirs);
            foreach (var file in files)
            {
                allOriginalFiles.Add(file);
            }
        }

        result.TotalFiles = allOriginalFiles.Count;

        // 遍历所有属性目录
        foreach (var propDirPath in _propertyDirService.GetAllHashDirectories())
        {
            result.TotalPropertyDirs++;

            string hash = Path.GetFileName(propDirPath);
            string originalPath = _propertyDirService.GetOriginalPath(hash);

            if (string.IsNullOrEmpty(originalPath) || !File.Exists(originalPath))
            {
                result.OrphanedDirs++;
                result.OrphanedPaths.Add(propDirPath);
            }
        }

        return result;
    }

    public List<PropertyDirectory> FindOrphanedDirectories()
    {
        var orphaned = new List<PropertyDirectory>();

        foreach (var propDirPath in _propertyDirService.GetAllHashDirectories())
        {
            string hash = Path.GetFileName(propDirPath);
            string originalPath = _propertyDirService.GetOriginalPath(hash);

            if (string.IsNullOrEmpty(originalPath) || !File.Exists(originalPath))
            {
                var propDir = new PropertyDirectory
                {
                    Hash = hash,
                    DirectoryPath = propDirPath,
                    OriginalPath = originalPath
                };
                orphaned.Add(propDir);
            }
        }

        return orphaned;
    }

    public void CleanOrphanedDirectories()
    {
        var orphaned = FindOrphanedDirectories();

        foreach (var propDir in orphaned)
        {
            _propertyDirService.Delete(propDir);
        }
    }

    public int GetOrphanedCount()
    {
        return FindOrphanedDirectories().Count;
    }
}
