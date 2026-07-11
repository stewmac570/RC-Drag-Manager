using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCDragManagerProd.Logging;

namespace RCDragManagerProd.Tests;

/// <summary>
/// Covers <see cref="LogWriter"/> (issue #383) — the shared, lock-guarded log
/// writer that replaced per-line File.AppendAllText. The old approach raced the
/// open-append-close between the UI thread, live-feed workers and the dial-in
/// poll timer, throwing (and swallowing) IOException, i.e. silently losing lines.
/// </summary>
[TestClass]
public class LogWriterTests
{
    private static string NewTempLogPath() =>
        Path.Combine(Path.GetTempPath(), $"rcdragmanager-logwriter-{Guid.NewGuid():N}.log");

    [TestMethod]
    public void WriteLine_ConcurrentWriters_LosesNoLines()
    {
        var path = NewTempLogPath();
        try
        {
            const int threads = 8;
            const int linesPerThread = 250;

            using (var writer = new LogWriter(path))
            {
                Parallel.For(0, threads, t =>
                {
                    for (int i = 0; i < linesPerThread; i++)
                        writer.WriteLine($"thread={t} line={i}");
                });
            }

            var lines = File.ReadAllLines(path);
            Assert.AreEqual(threads * linesPerThread, lines.Length,
                "Every concurrently written line must reach the file.");
            Assert.IsTrue(lines.All(l => l.Contains("thread=") && l.Contains("line=")),
                "No line may be torn or interleaved.");
        }
        finally
        {
            try { File.Delete(path); } catch { }
        }
    }

    [TestMethod]
    public void WriteLine_PrefixesInvariantTimestamp()
    {
        var path = NewTempLogPath();
        try
        {
            using (var writer = new LogWriter(path))
                writer.WriteLine("hello");

            var line = File.ReadAllLines(path).Single();
            StringAssert.Matches(line, new System.Text.RegularExpressions.Regex(
                @"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}  hello$"));
        }
        finally
        {
            try { File.Delete(path); } catch { }
        }
    }

    [TestMethod]
    public void Open_RollsOversizedFileToBackup()
    {
        var path = NewTempLogPath();
        var backup = path + ".1";
        try
        {
            // Pre-create a file just over the 5 MB threshold.
            using (var fs = new FileStream(path, FileMode.CreateNew))
                fs.SetLength(5 * 1024 * 1024 + 1);

            using (var writer = new LogWriter(path))
                writer.WriteLine("fresh line");

            Assert.IsTrue(File.Exists(backup), "Oversized log must be moved to <name>.1.");
            var lines = File.ReadAllLines(path);
            Assert.AreEqual(1, lines.Length, "The live log must restart fresh after rolling.");
        }
        finally
        {
            try { File.Delete(path); } catch { }
            try { File.Delete(backup); } catch { }
        }
    }

    [TestMethod]
    public void WriteLine_UnusableDirectory_LatchesOffWithoutThrowing()
    {
        var invalidPath = Path.Combine(Path.GetTempPath(),
            $"rcdragmanager-logwriter-missing-{Guid.NewGuid():N}", new string('x', 300), "app.log");

        using var writer = new LogWriter(invalidPath);
        writer.WriteLine("first");   // fails internally, latches off
        writer.WriteLine("second");  // no-op, must not throw
    }
}
