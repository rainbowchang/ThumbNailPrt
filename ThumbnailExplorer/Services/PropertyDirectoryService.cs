using System.Text.Json;
using ThumbnailExplorer.Models;

namespace ThumbnailExplorer.Services;

public class PropertyDirectoryService
{
    private readonly ConfigurationService _configService;
    private readonly HashService _hashService;
    private readonly string _propertyRoot;

    private const string ThumbnailsFolder = "thumbnails";
    private const string PropertiesFile = "properties.json";
    private const string PathFile = "path.txt";

    public PropertyDirectoryService(ConfigurationService configService, HashService hashService)
    {
        _configService = configService;
        _hashService = hashService;
        _propertyRoot = _configService.GetPropertyDirectoryRoot();
        Directory.CreateDirectory(_propertyRoot);
    }

    public PropertyDirectory GetOrCreate(string originalPath, DateTime creationTime)
    {
        string hash = _hashService.ComputeHash(originalPath, creationTime);
        string dirPath = _hashService.GetPropertyDirectoryPath(hash);

        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
            Directory.CreateDirectory(Path.Combine(dirPath, ThumbnailsFolder));

            // 创建 path.txt
            File.WriteAllText(Path.Combine(dirPath, PathFile), originalPath);

            // 创建默认 properties.json
            var defaultProps = new FileProperty
            {
                FileHash = hash,
                CreatedTime = DateTime.Now,
                ModifiedTime = DateTime.Now
            };
            SavePropertiesToDir(dirPath, defaultProps);
        }

        return LoadPropertyDirectory(hash, dirPath, originalPath);
    }

    public FileProperty LoadProperties(PropertyDirectory propDir)
    {
        return LoadPropertiesFromDir(propDir.DirectoryPath);
    }

    public void SaveProperties(PropertyDirectory propDir, FileProperty properties)
    {
        properties.ModifiedTime = DateTime.Now;
        SavePropertiesToDir(propDir.DirectoryPath, properties);
        propDir.Properties = properties;
    }

    public List<ThumbnailInfo> GetThumbnails(PropertyDirectory propDir)
    {
        var thumbnails = new List<ThumbnailInfo>();
        string thumbDir = Path.Combine(propDir.DirectoryPath, ThumbnailsFolder);

        if (Directory.Exists(thumbDir))
        {
            var files = Directory.GetFiles(thumbDir, "thumbnail_*.png")
                .OrderBy(f => f)
                .ToList();

            foreach (var file in files)
            {
                var thumb = new ThumbnailInfo
                {
                    FilePath = file,
                    CreatedTime = File.GetCreationTime(file),
                    Index = thumbnails.Count
                };
                thumbnails.Add(thumb);
            }
        }

        return thumbnails;
    }

    public void AddThumbnail(PropertyDirectory propDir, byte[] imageData)
    {
        string thumbDir = Path.Combine(propDir.DirectoryPath, ThumbnailsFolder);
        int maxThumbs = _configService.GetMaxThumbnailsPerFile();
        var existing = GetThumbnails(propDir);

        if (existing.Count >= maxThumbs)
        {
            throw new InvalidOperationException($"已达到最大缩略图数量 ({maxThumbs})");
        }

        int nextIndex = existing.Count + 1;
        string fileName = $"thumbnail_{nextIndex:D3}.png";
        string filePath = Path.Combine(thumbDir, fileName);

        File.WriteAllBytes(filePath, imageData);
    }

    public void Delete(PropertyDirectory propDir)
    {
        if (Directory.Exists(propDir.DirectoryPath))
        {
            DeleteToRecycleBin(propDir.DirectoryPath);
        }
    }

    private void DeleteToRecycleBin(string path)
    {
        // Use P/Invoke SHFileOperation to move to recycle bin
        SHFILEOPSTRUCT fs = new SHFILEOPSTRUCT();
        fs.wFunc = FO_DELETE;
        fs.pFrom = path + "\0\0";
        fs.fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_SILENT;

        int result = SHFileOperation(ref fs);
    }

    [System.Runtime.InteropServices.DllImport("shell32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern int SHFileOperation(ref SHFILEOPSTRUCT FileOp);

    private const int FO_DELETE = 0x0003;
    private const int FOF_ALLOWUNDO = 0x0040;
    private const int FOF_NOCONFIRMATION = 0x0010;
    private const int FOF_SILENT = 0x0004;

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private struct SHFILEOPSTRUCT
    {
        public IntPtr hwnd;
        public int wFunc;
        public string pFrom;
        public string pTo;
        public ushort fFlags;
        public bool fAnyOperationsAborted;
        public IntPtr hNameMappings;
        public string lpszProgressTitle;
    }

    public string GetOriginalPath(string hash)
    {
        string dirPath = _hashService.GetPropertyDirectoryPath(hash);
        string pathFile = Path.Combine(dirPath, PathFile);

        if (File.Exists(pathFile))
        {
            return File.ReadAllText(pathFile).Trim();
        }

        return string.Empty;
    }

    public bool ValidatePropertyDirectory(string hash, string originalPath, DateTime creationTime)
    {
        // 计算期望的哈希
        string expectedHash = _hashService.ComputeHash(originalPath, creationTime);

        // 哈希必须匹配
        if (hash != expectedHash)
            return false;

        // 检查 path.txt 中的路径是否匹配
        string storedPath = GetOriginalPath(hash);
        if (storedPath != originalPath)
            return false;

        // 检查原始文件是否存在
        return File.Exists(originalPath);
    }

    public IEnumerable<string> GetAllHashDirectories()
    {
        if (!Directory.Exists(_propertyRoot))
            yield break;

        foreach (var dir in Directory.GetDirectories(_propertyRoot))
        {
            string hash = Path.GetFileName(dir);
            if (_hashService.IsHashValid(hash))
            {
                yield return dir;
            }
        }
    }

    private PropertyDirectory LoadPropertyDirectory(string hash, string dirPath, string originalPath)
    {
        return new PropertyDirectory
        {
            Hash = hash,
            RootPath = _propertyRoot,
            DirectoryPath = dirPath,
            OriginalPath = originalPath,
            Properties = LoadPropertiesFromDir(dirPath),
            Thumbnails = GetThumbnailsFromDir(dirPath)
        };
    }

    private FileProperty LoadPropertiesFromDir(string dirPath)
    {
        string propsFile = Path.Combine(dirPath, PropertiesFile);

        if (File.Exists(propsFile))
        {
            string json = File.ReadAllText(propsFile);
            return JsonSerializer.Deserialize<FileProperty>(json) ?? new FileProperty();
        }

        return new FileProperty();
    }

    private void SavePropertiesToDir(string dirPath, FileProperty properties)
    {
        string propsFile = Path.Combine(dirPath, PropertiesFile);
        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(properties, options);
        File.WriteAllText(propsFile, json);
    }

    private List<ThumbnailInfo> GetThumbnailsFromDir(string dirPath)
    {
        var thumbnails = new List<ThumbnailInfo>();
        string thumbDir = Path.Combine(dirPath, ThumbnailsFolder);

        if (Directory.Exists(thumbDir))
        {
            var files = Directory.GetFiles(thumbDir, "thumbnail_*.png")
                .OrderBy(f => f)
                .ToList();

            foreach (var file in files)
            {
                thumbnails.Add(new ThumbnailInfo
                {
                    FilePath = file,
                    CreatedTime = File.GetCreationTime(file),
                    Index = thumbnails.Count
                });
            }
        }

        return thumbnails;
    }
}
