# Veeam_Task

A C# program that synchronizes two folders: source and replica. The program maintains a full, identical copy of the source folder at the replica folder location.

## Features

- Replica folder content is modified to exactly match the source folder
- Runs at specified intervals
- All file operations are logged to both console and file
- All configuration via command-line arguments
- Uses MD5 checksums to detect file changes
  
## Usage

```
VeeamTask <sourceFolderPath> <replicaFolderPath> <logFilePath> <intervalInSeconds>
```

## Arguments

- `sourceFolderPath` - Path to the source folder to sync
- `replicaFolderPath` - Path to the replica folder where files will be copied
- `logFilePath` - Path to the log file where operations will be logged
- `intervalInSeconds` - Interval in seconds for how often the folders should be synced

## Example

```bash
VeeamTask "C:\Source" "C:\Replica" "C:\Logs\sync.log" 60
```

This will synchronize `C:\Source` to `C:\Replica` every 60 seconds, logging all operations to `C:\Logs\sync.log`.
