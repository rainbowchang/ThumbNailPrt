using System.Diagnostics;

namespace ThumbnailExplorer.Services;

public class OpenFileService
{
    public void OpenWithDefaultProgram(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("文件不存在", filePath);
        }

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = filePath,
            UseShellExecute = true
        };

        Process.Start(startInfo);
    }

    public void OpenInExplorer(string path)
    {
        if (File.Exists(path))
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{path}\"",
                UseShellExecute = true
            };

            Process.Start(startInfo);
        }
        else if (Directory.Exists(path))
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{path}\"",
                UseShellExecute = true
            };

            Process.Start(startInfo);
        }
    }
}
