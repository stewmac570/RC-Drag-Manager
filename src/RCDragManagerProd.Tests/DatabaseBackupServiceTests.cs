using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Repositories;

namespace RCDragManagerProd.Tests;

/// <summary>
/// Guards the database backup feature (issue #405): a timestamped copy on every
/// app start with a rolling retention window, manual back up / restore with
/// validation, and the pre-restore safety snapshot. Every test runs against
/// throwaway temp-folder databases, never the real one.
/// </summary>
[TestClass]
[DoNotParallelize]
public class DatabaseBackupServiceTests
{
    private string _folder;
    private string DbPath => Path.Combine(_folder, "race_data.db");
    private string ConnString => $"Data Source={DbPath};Version=3;";

    [TestInitialize]
    public void Setup() =>
        _folder = Path.Combine(Path.GetTempPath(), $"rcdragmanager-backup-{Guid.NewGuid():N}");

    [TestCleanup]
    public void Teardown()
    {
        try { if (Directory.Exists(_folder)) Directory.Delete(_folder, recursive: true); }
        catch { /* best-effort */ }
    }

    private void CreateDatabase()
    {
        Directory.CreateDirectory(_folder);
        DatabaseInitializer.InitializeDatabase(ConnString);
    }

    // ── Startup backup + retention ────────────────────────────────────────────

    [TestMethod]
    public void BackupOnStartup_WritesTimestampedCopy_AndPrunesToRetention()
    {
        CreateDatabase();
        var service = new DatabaseBackupService(DbPath);

        for (int i = 0; i < 5; i++) service.BackupOnStartup(retentionCount: 3);

        var backups = Directory.GetFiles(service.BackupFolder, "race_data_*.db");
        Assert.AreEqual(3, backups.Length,
            "Only the retention count of backups may survive");

        foreach (var b in backups)
        {
            var name = Path.GetFileName(b);
            StringAssert.StartsWith(name, "race_data_", "Backups must use the race_data_ prefix");
            StringAssert.EndsWith(name, ".db", "Backups must keep the .db extension");
            Assert.IsTrue(Path.GetFileNameWithoutExtension(name).Length > "race_data_".Length,
                "The name must carry a timestamp");
        }
    }

    [TestMethod]
    public void BackupOnStartup_NoDatabase_ReturnsNull()
    {
        var service = new DatabaseBackupService(DbPath);

        Assert.IsNull(service.BackupOnStartup(),
            "A fresh install has no database to back up");
        Assert.IsFalse(Directory.Exists(service.BackupFolder),
            "No Backups folder should be created when there is nothing to copy");
    }

    // ── Manual back up / restore round-trip ───────────────────────────────────

    [TestMethod]
    public void BackupTo_RestoreFrom_RoundTripsDriversStatsAndSessions()
    {
        CreateDatabase();
        var repoA = new DriverRepository(ConnString);
        var driver = NewDriver("Round Trip Driver");
        repoA.AddDriver(driver);
        driver.TotalWins = 3;
        repoA.UpdateDriver(driver);
        new RaceSessionRepository(ConnString).SaveSession(new RaceSession
        {
            EventName = "Backup Round Trip",
            EventDate = new DateTime(2026, 9, 13, 9, 0, 0),
            RaceType = "Round Robin",
            ClassType = "Heads Up"
        });

        var backupFile = Path.Combine(_folder, "manual-backup.db");
        var written = new DatabaseBackupService(DbPath).BackupTo(backupFile);
        Assert.AreEqual(backupFile, written, "Back up now must report the file it wrote");
        Assert.IsTrue(File.Exists(backupFile), "Back up now must actually write the file");

        // Restore into a different, fresh database.
        var db2 = Path.Combine(_folder, "second.db");
        var conn2 = $"Data Source={db2};Version=3;";
        DatabaseInitializer.InitializeDatabase(conn2);
        var result = new DatabaseBackupService(db2).RestoreFrom(backupFile);

        Assert.IsTrue(result.Success, "A validated backup must restore: " + result.Error);
        var loadedDrivers = new DriverRepository(conn2).GetAllDrivers();
        Assert.AreEqual(1, loadedDrivers.Count, "Drivers must round-trip");
        Assert.AreEqual("Round Trip Driver", loadedDrivers[0].Name);
        Assert.AreEqual(3, loadedDrivers[0].TotalWins, "Stats must round-trip");
        Assert.AreEqual(1, loadedDrivers[0].Cars.Count, "Cars must round-trip");
        Assert.IsTrue(new RaceSessionRepository(conn2).GetAllSessions()
                .Any(s => s.EventName == "Backup Round Trip"),
            "Saved events must round-trip");
    }

    // ── Validation and safety ─────────────────────────────────────────────────

    [TestMethod]
    public void RestoreFrom_InvalidFile_FailsAndLeavesDatabaseUntouched()
    {
        CreateDatabase();
        var repo = new DriverRepository(ConnString);
        repo.AddDriver(NewDriver("Stay Put"));
        var before = repo.GetAllDrivers().Count;

        var garbage = Path.Combine(_folder, "garbage.db");
        File.WriteAllText(garbage, "this is not a sqlite database");

        var result = new DatabaseBackupService(DbPath).RestoreFrom(garbage);

        Assert.IsFalse(result.Success, "A non-database file must be refused");
        StringAssert.Contains(result.Error, "readable",
            "The refusal must be operator-friendly");
        Assert.AreEqual(before, repo.GetAllDrivers().Count,
            "A refused restore must leave the live database untouched");
    }

    [TestMethod]
    public void RestoreFrom_ValidBackup_SnapshotsTheReplacedDatabase()
    {
        CreateDatabase();
        var connString = ConnString;
        var repo = new DriverRepository(connString);
        repo.AddDriver(NewDriver("First"));
        var backupFile = Path.Combine(_folder, "manual-backup.db");
        new DatabaseBackupService(DbPath).BackupTo(backupFile);

        repo.AddDriver(NewDriver("Second"));   // the live DB moves on
        var result = new DatabaseBackupService(DbPath).RestoreFrom(backupFile);

        Assert.IsTrue(result.Success, "A validated backup must restore: " + result.Error);
        Assert.IsNotNull(result.SafetySnapshotPath, "Restore must name the pre-restore snapshot");
        Assert.IsTrue(File.Exists(result.SafetySnapshotPath), "The pre-restore snapshot must exist");
        StringAssert.Contains(Path.GetFileName(result.SafetySnapshotPath), "PRE-RESTORE",
            "The snapshot name must make clear it is the pre-restore safety copy");

        Assert.AreEqual(1, repo.GetAllDrivers().Count,
            "The live database must now hold the backup's content (one driver)");
        var snapshotRepo = new DriverRepository($"Data Source={result.SafetySnapshotPath};Version=3;");
        Assert.AreEqual(2, snapshotRepo.GetAllDrivers().Count,
            "The snapshot must hold the replaced database's content (two drivers)");
    }

    [TestMethod]
    public void RestoreFrom_MissingFile_IsRefusedWithoutTouchingDatabase()
    {
        CreateDatabase();
        var repo = new DriverRepository(ConnString);
        repo.AddDriver(NewDriver("Still Here"));

        var result = new DatabaseBackupService(DbPath)
            .RestoreFrom(Path.Combine(_folder, "does-not-exist.db"));

        Assert.IsFalse(result.Success, "A missing file must be refused");
        Assert.AreEqual(1, repo.GetAllDrivers().Count, "The live database must be untouched");
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static Driver NewDriver(string name) => new Driver
    {
        Name = name,
        QualTime = 5.5,
        Cars = new List<Car> { new Car { CarName = name + " Car", ClassType = "Heads Up" } }
    };
}
