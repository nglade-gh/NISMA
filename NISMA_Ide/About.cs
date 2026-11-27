using System.Diagnostics;

namespace NISMA_Ide;

/// <summary>
/// Identity of the running build, served to the About dialog: full name, author, version
/// (build date and time, commit) and repository.
///
/// The commit is read from the git repository that contains the executable, once, at first
/// use. When git is unavailable or the folder is not a repository — a plain copy of the
/// binaries, for instance — the commit is reported as unknown rather than invented.
/// </summary>
public sealed record AboutInfo(
    string Name,
    string Author,
    string Version,
    string Commit,
    string Repository);

public static class About
{
    public const string FullName = "NetBioDyn Instructions for Simulation and Model Authoring";
    public const string AuthorName = "Nicolas GLADE";
    public const string RepositoryUrl = "https://github.com/nglade-gh/NISMA.git";

    private static readonly Lazy<AboutInfo> Info = new(Read);

    public static AboutInfo Get() => Info.Value;

    private static AboutInfo Read()
    {
        var assembly = typeof(About).Assembly.Location;
        var built = assembly.Length > 0 && File.Exists(assembly)
            ? File.GetLastWriteTime(assembly)
            : DateTime.Now;

        var commit = Commit();
        return new AboutInfo(
            FullName,
            AuthorName,
            built.ToString("dd/MM/yyyy HH:mm"),
            commit ?? "inconnu",
            RepositoryUrl);
    }

    /// <summary>Short hash of the commit the executable was built from, or null.</summary>
    private static string? Commit()
    {
        try
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null &&
                   !Directory.Exists(Path.Combine(directory.FullName, ".git")))
                directory = directory.Parent;

            if (directory is null) return null;

            var start = new ProcessStartInfo("git", $"-C \"{directory.FullName}\" rev-parse --short HEAD")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(start);
            if (process is null) return null;

            var output = process.StandardOutput.ReadToEnd().Trim();
            if (!process.WaitForExit(2000))
            {
                try { process.Kill(true); } catch { /* already gone */ }
                return null;
            }

            return process.ExitCode == 0 && output.Length > 0 ? output : null;
        }
        catch
        {
            return null;
        }
    }
}
