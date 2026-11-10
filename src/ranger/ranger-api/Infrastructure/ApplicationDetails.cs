using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace RangerAi.Infrastructure;

public static class ApplicationDetails
{
    public static string Name => Assembly.GetEntryAssembly()?.GetName().Name;

    public static string Version => Assembly.GetEntryAssembly()?.GetName().Version?.ToString().ToUpper() ?? "UNKNOWN";

    public static string VersionFile
    {
        get
        {
            var fileName = Assembly.GetEntryAssembly()?.Location;
            return fileName != null ? FileVersionInfo.GetVersionInfo(fileName).FileVersion : "";
        }
    }

    /// <summary>
    ///     Returns installed exe path, for commands like c:\exercise\ghosts\ghosts.exe to work properly
    /// </summary>
    public static string InstalledPath => Clean(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location));

    public static string GetPath(string loc)
    {
        return Path.GetFullPath(Path.Combine(InstalledPath, loc));
    }

    public static bool IsLinux()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    }

    // ReSharper disable once InconsistentNaming
    public static bool IsOSX()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    }

    public static string GetOllamaHost()
    {
        var value = Environment.GetEnvironmentVariable("OLLAMA_HOST") ?? "http://host.docker.internal:11434";
        return value;
    }

    /// <summary>
    /// Currently n8n
    /// </summary>
    public static string GetWorkflowHost()
    {
        var value = Environment.GetEnvironmentVariable("N8N_API_URL") ?? "http://host.docker.internal:5678";
        return value;
    }

    public static string GetWorkflowApiKey()
    {
        var value = Environment.GetEnvironmentVariable("N8N_API_KEY") ?? "";
        return value;
    }

    public static string GetGhostsHost()
    {
        var value = Environment.GetEnvironmentVariable("GHOSTS_HOST") ?? "http://host.docker.internal:5000";
        return value;
    }

    private static string Clean(string x)
    {
        //linux path is file:/users
        //windows path is file:/z:
        //ugh
        var fileFormat = "file:\\";
        if (IsLinux() || IsOSX()) fileFormat = "file:";

        if (x.Contains(fileFormat)) x = x.Substring(x.IndexOf(fileFormat, StringComparison.InvariantCultureIgnoreCase) + fileFormat.Length);

        x = x.Replace(Convert.ToChar(@"\"), Path.DirectorySeparatorChar);
        x = x.Replace(Convert.ToChar(@"/"), Path.DirectorySeparatorChar);

        return x;
    }
}
