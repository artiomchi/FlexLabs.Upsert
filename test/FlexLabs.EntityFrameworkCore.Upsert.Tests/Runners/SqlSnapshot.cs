using System.Runtime.CompilerServices;
using System.Text;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.Runners;

/// <summary>
/// Compares generated SQL with a checked-in snapshot file (<c>Runners/Snapshots/&lt;Engine&gt;.&lt;Scenario&gt;.sql</c>).
/// Snapshots store the statement split into one clause per line, so a change to the generated SQL shows up as a
/// readable diff.
/// </summary>
/// <remarks>
/// Snapshots are embedded in the test assembly and read from there, so tests don't depend on where the sources are
/// (CI builds rewrite <c>[CallerFilePath]</c> to <c>/_/...</c> for reproducible builds). The source path is only used
/// to write files when updating.
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

    public static void Match(string sql, string name, [CallerFilePath] string callerPath = "")
    {
        var actual = Normalise(Format(sql));

        if (UpdateSnapshots)
        {
            var sourceDirectory = Path.GetDirectoryName(callerPath);
            if (!Directory.Exists(sourceDirectory))
                Assert.Fail($"Can't update snapshots: the test sources aren't at '{callerPath}'. Run UPDATE_SNAPSHOTS=1 from a normal (non-CI) build of a source checkout.");

            var snapshotDirectory = Path.Combine(sourceDirectory, "Snapshots");
            Directory.CreateDirectory(snapshotDirectory);
            File.WriteAllText(Path.Combine(snapshotDirectory, name + ".sql"), actual + "\n", new UTF8Encoding(false));
            return;
        }

        var snapshot = ReadEmbeddedSnapshot(name);
        if (snapshot is null)
        {
            Assert.Fail($"Missing SQL snapshot Runners/Snapshots/{name}.sql. To create it run `UPDATE_SNAPSHOTS=1 dotnet test` locally." +
                $"{Environment.NewLine}Actual SQL:{Environment.NewLine}{actual}");
        }

        actual.Should().Be(Normalise(snapshot), $"the SQL must match {name}.sql (run with UPDATE_SNAPSHOTS=1 to accept an intentional change)");
    }

    // The csproj embeds Runners/Snapshots/*.sql with the logical name "Snapshots/<file>.sql".
    private static string ReadEmbeddedSnapshot(string name)
    {
        using var stream = typeof(SqlSnapshot).Assembly.GetManifestResourceStream($"Snapshots/{name}.sql");
        if (stream is null)
            return null;

        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
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
