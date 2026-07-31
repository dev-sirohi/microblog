namespace Microblog.LoadTests.Shared;

// Tiny .env loader (no external package).
// Reads "KEY=VALUE" lines from a .env file and puts them into environment variables.
// It searches upward from the current folder, so it works no matter where you run from.
public static class DotEnv
{
    public static void Load()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null)
        {
            var path = Path.Combine(dir.FullName, ".env");
            if (File.Exists(path))
            {
                Apply(path);
                return;
            }
            dir = dir.Parent;
        }
        // No .env found — that's fine; values can also come from real environment variables.
    }

    private static void Apply(string path)
    {
        foreach (var raw in File.ReadAllLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;   // skip blanks & comments

            var eq = line.IndexOf('=');
            if (eq <= 0) continue;                                    // skip malformed lines

            var key = line[..eq].Trim();
            var val = line[(eq + 1)..].Trim();

            // Don't overwrite a value that's already set in the real environment.
            if (Environment.GetEnvironmentVariable(key) is null)
                Environment.SetEnvironmentVariable(key, val);
        }
    }
}
