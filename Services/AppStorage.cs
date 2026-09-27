using System;
using System.IO;

namespace ActiveRest.Services;

public sealed record StorageProfile(string Directory, bool IsSeparate);

public static class AppStorage
{
    public static StorageProfile Profile { get; private set; } = DefaultProfile();

    public static void Configure(string[] args) => Profile = Resolve(args);

    public static StorageProfile Resolve(string[] args)
    {
        if (args.Length == 0) return DefaultProfile();
        if (args.Length != 2 || args[0] != "--data-directory")
            throw new ArgumentException("Use --data-directory followed by one absolute folder path, or launch without arguments.");
        return SeparateProfile(args[1]);
    }

    public static StorageProfile SeparateProfile(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Path.IsPathFullyQualified(directory))
            throw new ArgumentException("The separate data directory must be an absolute folder path.");
        directory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory));
        var normal = Path.TrimEndingDirectorySeparator(Path.GetFullPath(DefaultProfile().Directory));
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (Within(directory, normal, comparison) || Within(normal, directory, comparison))
            throw new ArgumentException("A separate profile cannot overlap the normal ActiveRest data folder.");
        EnsureNoLinks(directory);
        return new StorageProfile(directory, true);
    }

    private static bool Within(string path, string parent, StringComparison comparison) =>
        path.Equals(parent, comparison) || path.StartsWith(
            Path.EndsInDirectorySeparator(parent) ? parent : parent + Path.DirectorySeparatorChar, comparison);

    private static StorageProfile DefaultProfile() => new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ActiveRest"), false);

    public static void EnsureNoLinks(string path)
    {
        var current = Path.GetFullPath(path);
        while (current != null)
        {
            try
            {
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("Linked data files or folders are not supported; choose a separate real folder.");
            }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
            current = Path.GetDirectoryName(current);
        }
    }
}
