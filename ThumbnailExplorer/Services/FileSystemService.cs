using ThumbnailExplorer.Models;

namespace ThumbnailExplorer.Services;

public class FileSystemService
{
    public OriginalFile GetFileInfo(string path)
    {
        var fileInfo = new FileInfo(path);

        return new OriginalFile
        {
            FullPath = fileInfo.FullName,
            FileName = fileInfo.Name,
            Extension = fileInfo.Extension.ToLowerInvariant(),
            CreationTime = fileInfo.CreationTime,
            LastWriteTime = fileInfo.LastWriteTime,
            Length = fileInfo.Length
        };
    }

    public bool FileExists(string path, DateTime creationTime)
    {
        if (!File.Exists(path))
            return false;

        var fileInfo = new FileInfo(path);
        return fileInfo.CreationTime.Date == creationTime.Date &&
               fileInfo.CreationTime.Hour == creationTime.Hour &&
               fileInfo.CreationTime.Minute == creationTime.Minute &&
               fileInfo.CreationTime.Second == creationTime.Second;
    }

    public long GetFileSize(string path)
    {
        return new FileInfo(path).Length;
    }

    public DateTime GetLastWriteTime(string path)
    {
        return File.GetLastWriteTime(path);
    }

    public IEnumerable<string> GetDirectories(string path)
    {
        return Directory.GetDirectories(path);
    }

    public IEnumerable<string> GetFiles(string path, string searchPattern)
    {
        return Directory.GetFiles(path, searchPattern);
    }

    public IEnumerable<string> GetFilesRecursive(string path, IEnumerable<string> extensions, IEnumerable<string> ignoredDirs)
    {
        var result = new List<string>();
        var extSet = new HashSet<string>(extensions, StringComparer.OrdinalIgnoreCase);
        var ignoredSet = new HashSet<string>(ignoredDirs, StringComparer.OrdinalIgnoreCase);

        ScanDirectoryRecursive(path, extSet, ignoredSet, result);
        return result;
    }

    private void ScanDirectoryRecursive(string path, HashSet<string> extensions, HashSet<string> ignoredDirs, List<string> result)
    {
        try
        {
            foreach (var dir in Directory.GetDirectories(path))
            {
                string dirName = Path.GetFileName(dir);
                if (ignoredDirs.Contains(dirName))
                    continue;

                ScanDirectoryRecursive(dir, extensions, ignoredDirs, result);
            }

            foreach (var file in Directory.GetFiles(path))
            {
                string ext = Path.GetExtension(file);
                if (extensions.Contains(ext))
                {
                    result.Add(file);
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            // 跳过无权限访问的目录
        }
    }
}
