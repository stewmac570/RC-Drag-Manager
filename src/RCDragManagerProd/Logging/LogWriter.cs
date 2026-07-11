using System;
using System.Globalization;
using System.IO;

namespace RCDragManagerProd.Logging
{
    /// <summary>
    /// Serialized, append-only log file writer. Keeps one file handle open for the
    /// writer's lifetime (AutoFlush, share-read so the operator can tail the file)
    /// and takes a lock per line so concurrent threads can never tear or drop lines.
    /// The file is rolled to "&lt;name&gt;.1" when it exceeds 5 MB at open time.
    /// </summary>
    public sealed class LogWriter : IDisposable
    {
        private const long MaxLogBytes = 5 * 1024 * 1024;

        private readonly object _sync = new object();
        private readonly string _path;
        private StreamWriter _stream;
        private bool _failed;

        public LogWriter(string path)
        {
            _path = path;
        }

        public void WriteLine(string message)
        {
            lock (_sync)
            {
                if (_failed) return;
                try
                {
                    if (_stream == null) Open();
                    _stream.WriteLine(
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
                        + "  " + message);
                }
                catch
                {
                    // The path is unusable (another instance holds it, disk full, ...).
                    // Latch off instead of retrying the failing open on every line.
                    _failed = true;
                    try { _stream?.Dispose(); } catch { }
                    _stream = null;
                }
            }
        }

        private void Open()
        {
            var dir = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            RollIfOversized();
            _stream = new StreamWriter(
                new FileStream(_path, FileMode.Append, FileAccess.Write, FileShare.Read))
            {
                AutoFlush = true
            };
        }

        private void RollIfOversized()
        {
            try
            {
                var info = new FileInfo(_path);
                if (!info.Exists || info.Length <= MaxLogBytes) return;

                var backup = _path + ".1";
                if (File.Exists(backup)) File.Delete(backup);
                File.Move(_path, backup);
            }
            catch
            {
                // Rolling is best-effort; appending to an oversized log still works.
            }
        }

        public void Dispose()
        {
            lock (_sync)
            {
                try { _stream?.Dispose(); } catch { }
                _stream = null;
                _failed = false;
            }
        }
    }
}
