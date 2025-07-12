namespace VeeamTask
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length != 4)
            {
                ShowHelp();
                return;
            }

            var sourceFolderPath = args[0];
            var replicaFolderPath = args[1];
            var logFileName = args[2];
            if (!int.TryParse(args[3], out var syncInterval) || syncInterval <= 0)
            {
                Console.WriteLine("Error: Sync interval must be a positive integer.");
                ShowHelp();
                return;
            }

            using var cts = new CancellationTokenSource();
            
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                Console.WriteLine("\nShutdown requested...");
            };

            try
            {
                var folderSync = new FolderSync(sourceFolderPath, replicaFolderPath, logFileName, syncInterval);
                await folderSync.SyncFoldersAsync(cts.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private static void ShowHelp()
        {
            Console.WriteLine(@"
Usage: VeeamTask <sourceFolderPath> <replicaFolderPath> <logFilePath> <intervalInSeconds>
Arguments:
  sourceFolderPath    Path to the source folder to sync.
  replicaFolderPath   Path to the replica folder where files will be copied.
  logFilePath         Path to the log file where operations will be logged.
  intervalInSeconds   Interval in seconds for how often the folders should be synced.
Example:
  VeeamTask ""C:\Source"" ""C:\Replica"" ""C:\Logs\sync.log"" 60
");
        }
    }
}