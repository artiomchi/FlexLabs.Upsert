using System.Text;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.Runners;

/// <summary>
/// Compares generated SQL with a checked-in snapshot file (<c>Runners/Snapshots/&lt;Engine&gt;.&lt;Scenario&gt;.sql</c>).
/// Snapshots store the statement split into one clause per line, so a change to the generated SQL shows up as a
/// readable diff.
/// </summary>
/// <remarks>
/// Snapshots are plain files in the source tree. The project directory is found by walking up from the test output
/// folder to this project's .csproj, so it doesn't depend on compile-time paths (reproducible CI builds rewrite
/// <c>[CallerFilePath]</c> to <c>/_/...</c>); the tests must run from the build output of a source checkout.
/// After an intentional SQL change, run <c>UPDATE_SNAPSHOTS=1 dotnet test</c> locally and review <c>git diff</c>.
/// The update switch is ignored when <c>CI=true</c> (set by GitHub Actions), so CI never creates or rewrites
/// snapshots: a missing or different snapshot fails the test.
/// </remarks>
internal static class SqlSnapshot
{
    private static readonly bool UpdateSnapshots =
        Environment.GetEnvironmentVariable("UPDATE_SNAPSHOTS") == "1"
        && Environment.GetEnvironmentVariable("CI") != "true";

    // Clause keywords that start a new line when they appear outside any parentheses or quoted identifier.
    private static readonly string[] ClauseKeywords =
    [
        "USING", "ON", "VALUES", "WHERE",                        // MERGE sources and conditions, INSERT values
        "DO UPDATE SET", "DO NOTHING",                           // Postgres / Sqlite conflict action
        "WHEN MATCHED", "WHEN NOT MATCHED",                      // MERGE branches
    ];

    private static readonly Lazy<string> SnapshotDirectory = new(FindSnapshotDirectory);

    public static void Match(string sql, string name)
    {
        var actual = Normalise(Format(sql));
        var path = Path.Combine(SnapshotDirectory.Value, name + ".sql");

        if (UpdateSnapshots)
        {
            Directory.CreateDirectory(SnapshotDirectory.Value);
            File.WriteAllText(path, actual + "\n", new UTF8Encoding(false));
            return;
        }

        if (!File.Exists(path))
        {
            Assert.Fail($"Missing SQL snapshot {path}. To create it run `UPDATE_SNAPSHOTS=1 dotnet test` locally." +
                $"{Environment.NewLine}Actual SQL:{Environment.NewLine}{actual}");
        }

        actual.Should().Be(Normalise(File.ReadAllText(path)), $"the SQL must match {name}.sql (run with UPDATE_SNAPSHOTS=1 to accept an intentional change)");
    }

    private static string FindSnapshotDirectory()
    {
        var projectFile = typeof(SqlSnapshot).Assembly.GetName().Name + ".csproj";
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, projectFile)))
                return Path.Combine(directory.FullName, "Runners", "Snapshots");
        }

        throw new InvalidOperationException(
            $"Couldn't find {projectFile} in or above '{AppContext.BaseDirectory}'. SQL snapshots are read from the source tree, " +
            "so the tests must run from the build output of a source checkout.");
    }

    /// <summary>Splits a single-line statement into one clause per line. Joining the lines with spaces restores the original.</summary>
    internal static string Format(string sql)
    {
        var result = new StringBuilder(sql.Length + 16);
        var depth = 0;
        char? closingQuote = null;

        for (var i = 0; i < sql.Length; i++)
        {
            var c = sql[i];

            if (closingQuote is { } quote)
            {
                if (c == quote)
                    closingQuote = null;
            }
            else
            {
                switch (c)
                {
                    case '"' or '`' or '\'': closingQuote = c; break;
                    case '[': closingQuote = ']'; break;
                    case '(': depth++; break;
                    case ')': depth--; break;
                    case ' ' when depth == 0 && StartsWithClauseKeyword(sql, i + 1):
                        result.Append('\n');
                        continue;
                }
            }

            result.Append(c);
        }

        return result.ToString();
    }

    private static bool StartsWithClauseKeyword(string sql, int index)
    {
        foreach (var keyword in ClauseKeywords)
        {
            var end = index + keyword.Length;
            if (string.CompareOrdinal(sql, index, keyword, 0, keyword.Length) == 0
                && (end == sql.Length || !(char.IsLetterOrDigit(sql[end]) || sql[end] == '_')))
            {
                return true;
            }
        }

        return false;
    }

    private static string Normalise(string text)
        => text.ReplaceLineEndings("\n").TrimEnd();
}
