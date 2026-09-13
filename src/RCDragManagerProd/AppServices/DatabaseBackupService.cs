using System;
using System.IO;
using System.Linq;
using RCDragManagerProd.Logging;
using RCDragManagerProd.Repositories;

namespace RCDragManagerProd.AppServices
{
    /// <summary>
    /// Database backups (issue #405). Copies <c>race_data.db</c> to a timestamped
    /// file in the Backups folder on every app start (pruned to a rolling window),
    /// and backs the manual "Back up now" / "Restore from backup" actions in the
    /// Settings window. All file handling lives here; the view only opens the file
    /// dialogs and shows the results.
    /// </summary>
    public sealed class DatabaseBackupService
    {
        public const int DefaultRetentionCount = 10;

        /// <summary>Startup backups are timestamped, so pruning matches only them:
        /// manual backups the operator picked a folder for, and the PRE-RESTORE
        /// safety snapshots, use different names and are never pruned.</summary>
        private const string BackupPattern = "race_data_????-??-??_*.db";

        private readonly string _dbPath;

        public DatabaseBackupService(string dbPath)
        {
            _dbPath = dbPath ?? throw new ArgumentNullException(nameof(dbPath));
        }

        /// <summary>The Backups folder beside the database file.</summary>
        public string BackupFolder => Path.Combine(Path.GetDirectoryName(_dbPath), "Backups");

        /// <summary>
        /// Copies the database into the Backups folder as
        /// <c>race_data_YYYY-MM-DD_HHmm.db</c> and prunes older backups beyond
        /// <paramref name="retentionCount"/>. Returns the new backup path, or null
        /// when there is no database to copy.
        /// </summary>
        public string BackupOnStartup(int retentionCount = DefaultRetentionCount)
        {
            if (!File.Exists(_dbPath)) return null;
            Directory.CreateDirectory(BackupFolder);

            var stamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
            var target = Path.Combine(BackupFolder, $"race_data_{stamp}.db");

            // Two starts inside the same second must not overwrite each other.
            int suffix = 2;
            while (File.Exists(target))
                target = Path.Combine(BackupFolder, $"race_data_{stamp}_{suffix++}.db");

            File.Copy(_dbPath, target, overwrite: false);
            Logger.Log($"[BACKUP] Startup backup written: {target}");

            Prune(retentionCount);
            return target;
        }

        /// <summary>
        /// Copies the database to an arbitrary location (USB stick friendly).
        /// Returns the path written, or null when there is no database to copy.
        /// </summary>
        public string BackupTo(string targetPath)
        {
            if (targetPath == null) throw new ArgumentNullException(nameof(targetPath));
            if (!File.Exists(_dbPath)) return null;

            File.Copy(_dbPath, targetPath, overwrite: true);
            Logger.Log($"[BACKUP] Manual backup written: {targetPath}");
            return targetPath;
        }

        /// <summary>
        /// Replaces the live database with a validated backup. The current database
        /// is snapshotted into the Backups folder first so a bad restore can be
        /// undone, and the source is checked read-only before anything is touched.
        /// A failed validation changes nothing.
        /// </summary>
        public RestoreResult RestoreFrom(string sourcePath)
        {
            if (sourcePath == null) throw new ArgumentNullException(nameof(sourcePath));
            if (!File.Exists(sourcePath))
                return RestoreResult.Fail("No file was found at the chosen location.");

            var problem = DatabaseInitializer.ValidateDatabaseFile(sourcePath);
            if (problem != null) return RestoreResult.Fail(problem);

            string snapshot = null;
            if (File.Exists(_dbPath))
            {
                Directory.CreateDirectory(BackupFolder);
                snapshot = Path.Combine(BackupFolder,
                    $"race_data_PRE-RESTORE_{DateTime.Now:yyyyMMdd_HHmmss}.db");
                File.Copy(_dbPath, snapshot, overwrite: false);
            }

            File.Copy(sourcePath, _dbPath, overwrite: true);
            Logger.Log($"[BACKUP] Restore applied from {sourcePath}; " +
                       $"pre-restore snapshot: {snapshot ?? "(none — no database existed before)"}");
            return RestoreResult.Ok(snapshot);
        }

        /// <summary>Deletes the oldest startup backups beyond the retention count.</summary>
        public void Prune(int retentionCount = DefaultRetentionCount)
        {
            if (!Directory.Exists(BackupFolder)) return;

            var backups = Directory.GetFiles(BackupFolder, BackupPattern)
                .OrderByDescending(f => f, StringComparer.OrdinalIgnoreCase)
                .ToList();

            for (int i = retentionCount; i < backups.Count; i++)
            {
                try
                {
                    File.Delete(backups[i]);
                    Logger.Log($"[BACKUP] Pruned old backup: {backups[i]}");
                }
                catch (Exception ex)
                {
                    Logger.Log($"[BACKUP] Prune failed for {backups[i]}: {ex.Message}");
                }
            }
        }
    }

    /// <summary>Result of <see cref="DatabaseBackupService.RestoreFrom"/>.</summary>
    public sealed class RestoreResult
    {
        private RestoreResult(bool success, string error, string safetySnapshotPath)
        {
            Success = success;
            Error = error;
            SafetySnapshotPath = safetySnapshotPath;
        }

        /// <summary>True when the database was replaced.</summary>
        public bool Success { get; }

        /// <summary>Operator-friendly reason the restore was refused; null on success.</summary>
        public string Error { get; }

        /// <summary>Path of the pre-restore snapshot of the replaced database;
        /// null when there was no database to snapshot.</summary>
        public string SafetySnapshotPath { get; }

        public static RestoreResult Ok(string safetySnapshotPath) =>
            new RestoreResult(true, null, safetySnapshotPath);

        public static RestoreResult Fail(string error) => new RestoreResult(false, error, null);
    }
}
