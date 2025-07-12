using System.Security.Cryptography;

namespace VeeamTask;

public class FolderSync
{
    private readonly DirectoryInfo _sourceDirectory;
    private readonly DirectoryInfo _replicaDirectory;
    private readonly string _logFilePath;
    private readonly int _syncInterval;

    public FolderSync(string sourceFolderPath, string replicaFolderPath, string logFilePath, int syncInterval)
    {
        _sourceDirectory = new DirectoryInfo(sourceFolderPath);
        if (!_sourceDirectory.Exists)
        {
            throw new DirectoryNotFoundException($"Source directory '{sourceFolderPath}' does not exist.");
        }

        _replicaDirectory = new DirectoryInfo(replicaFolderPath);
        if (!_replicaDirectory.Exists)
        {
            Directory.CreateDirectory(_replicaDirectory.FullName);
        }

        _logFilePath = logFilePath;
        _syncInterval = syncInterval;
    }

    public async Task SyncFoldersAsync(CancellationToken cancellationToken = default)
    {
        Log("Starting folder synchronization", false);
        Log("Press Ctrl+C to stop synchronization.", false);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    ProcessDirectories();
                    ProcessFiles();
                }
                catch (Exception ex)
                {
                    Log($"Error during synchronization: {ex.Message}", false);
                }

                try
                {
                    await Task.Delay(_syncInterval * 1000, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            Log("Synchronization was cancelled.", false);
        }

        Log("Synchronization stopped.", false);
    }

    private void ProcessFiles()
    {
        foreach (var sourceFile in _sourceDirectory.GetFiles("*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(_sourceDirectory.FullName, sourceFile.FullName);
            var replicaFilePath = Path.Combine(_replicaDirectory.FullName, relativePath);
            var replicaFile = new FileInfo(replicaFilePath);
            if (!replicaFile.Exists)
            {
                Directory.CreateDirectory(replicaFile.DirectoryName!);
                sourceFile.CopyTo(replicaFile.FullName);
                Log($"Copied {sourceFile.FullName} to {replicaFile.FullName}");
            }
            else if (!CompareFiles(sourceFile, replicaFile))
            {
                sourceFile.CopyTo(replicaFile.FullName, true);
                Log($"Updated {replicaFile.FullName}");
            }
        }

        RemoveFiles();
    }

    private void RemoveFiles()
    {
        foreach (var replicaFile in _replicaDirectory.GetFiles("*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(_replicaDirectory.FullName, replicaFile.FullName);
            var sourceFilePath = Path.Combine(_sourceDirectory.FullName, relativePath);

            if (!File.Exists(sourceFilePath))
            {
                try
                {
                    File.Delete(replicaFile.FullName);
                    Log($"Deleted {replicaFile.FullName}");
                }
                catch (IOException ex)
                {
                    Log($"Failed to delete file {replicaFile.FullName}: {ex.Message}");
                }
            }
        }
    }

    private void ProcessDirectories()
    {
        foreach (var sourceSubDir in _sourceDirectory.GetDirectories("*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(_sourceDirectory.FullName, sourceSubDir.FullName);
            var replicaSubDirPath = Path.Combine(_replicaDirectory.FullName, relativePath);
            var replicaSubDir = new DirectoryInfo(replicaSubDirPath);

            if (!replicaSubDir.Exists)
            {
                Directory.CreateDirectory(replicaSubDir.FullName);
                Log($"Created directory {replicaSubDir.FullName}");
            }
        }

        RemoveDirectories();
    }

    private void RemoveDirectories()
    {
        var replicaDirs = _replicaDirectory
            .GetDirectories("*", SearchOption.AllDirectories)
            .OrderByDescending(d => d.FullName.Length);

        foreach (var replicaSubDir in replicaDirs)
        {
            var relativePath = Path.GetRelativePath(_replicaDirectory.FullName, replicaSubDir.FullName);
            var sourceSubDirPath = Path.Combine(_sourceDirectory.FullName, relativePath);

            if (!Directory.Exists(sourceSubDirPath))
            {
                try
                {
                    Directory.Delete(replicaSubDir.FullName, true);
                    Log($"Deleted directory {replicaSubDir.FullName}");
                }
                catch (IOException ex)
                {
                    Log($"Failed to delete directory {replicaSubDir.FullName}: {ex.Message}");
                }
            }
        }
    }

    private static bool CompareFiles(FileInfo file1, FileInfo file2)
    {
        if (file1.Length != file2.Length)
            return false;
        using var md5 = MD5.Create();
        using var fs1 = file1.OpenRead();
        using var fs2 = file2.OpenRead();
        var file1Hash = md5.ComputeHash(fs1);
        var file2Hash = md5.ComputeHash(fs2);
        return file1Hash.SequenceEqual(file2Hash);
    }

    private void Log(string message, bool toFile = true)
    {
        var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
        Console.WriteLine(logMessage);
        if (toFile)
        {
            try
            {
                var logDir = Path.GetDirectoryName(_logFilePath);
                if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                File.AppendAllText(_logFilePath, logMessage + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Log($"Failed to write to log file: {ex.Message}", false);
            }
        }
    }
}