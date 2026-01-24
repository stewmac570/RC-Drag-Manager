using System.Collections.Generic;

namespace RCDragManager.CodeStats.Models
{
    public class RepositoryInfo
    {
        /// <summary>
        /// Repository class name, e.g. RaceSessionRepository.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Namespace containing the repository class.
        /// </summary>
        public string? Namespace { get; set; }

        /// <summary>
        /// Fully-qualified name (Namespace + "." + Name).
        /// </summary>
        public string? FullName { get; set; }

        /// <summary>
        /// Relative file path from scan root.
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Base type / inheritance info, if we can see it (e.g. object, SomeBaseRepo).
        /// </summary>
        public string? BaseType { get; set; }

        /// <summary>
        /// SQL usages found inside this repository.
        /// </summary>
        public List<RepositorySqlUsage> SqlUsages { get; set; } = new List<RepositorySqlUsage>();
    }

    public class RepositorySqlUsage
    {
        /// <summary>
        /// Name of the method containing this SQL, if detected.
        /// </summary>
        public string? MethodName { get; set; }

        /// <summary>
        /// 1-based line number of the SQL snippet.
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Short single-line snippet of the SQL content.
        /// </summary>
        public string Snippet { get; set; } = string.Empty;

        /// <summary>
        /// Command type, e.g. SELECT, INSERT, UPDATE, DELETE.
        /// </summary>
        public string? CommandType { get; set; }

        /// <summary>
        /// Tables referenced in the query (FROM, JOIN, INTO).
        /// </summary>
        public List<string> Tables { get; set; } = new List<string>();

        /// <summary>
        /// Columns referenced in the SELECT or INSERT column list.
        /// </summary>
        public List<string> Columns { get; set; } = new List<string>();

        /// <summary>
        /// Parameters like @Id, @SessionId.
        /// </summary>
        public List<string> Parameters { get; set; } = new List<string>();
    }
}
